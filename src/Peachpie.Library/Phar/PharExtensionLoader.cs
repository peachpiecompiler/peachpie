using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;
using static Pchp.Library.Phar.PharExtensions;
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library.Phar
{
    internal sealed class PharExtension
    {
        sealed class PharWrapper : StreamWrapper
        {
            public static string scheme => "phar";

            #region StreamWrapper

            public override string Label => "phar";

            public override string Scheme => scheme;

            public override bool IsUrl => true;

            static bool TryResolvePhar(Context ctx, ReadOnlySpan<char> path, out CachedPhar phar, out ReadOnlySpan<char> entry)
            {
                return TryResolvePhar(pharpath =>
                {
                    var result = PharExtensions.AliasToPharFile(ctx, pharpath);

                    if (result == null && pharpath.EndsWith(PharExtensions.PharExtension, CurrentPlatform.PathStringComparison))
                    {
                        // resolve not-mapped phars (resolve path to root ... find .phar in compiled scripts ...)
                        var stub = Context.TryResolveScript(ctx.RootPath, pharpath);
                        result = PharExtensions.TryGetCachedPhar(stub);
                    }

                    return result;

                }, path, out phar, out entry);
            }

            static bool TryResolvePhar(Func<string, CachedPhar> resolver, ReadOnlySpan<char> path, out CachedPhar phar, out ReadOnlySpan<char> entry)
            {
                phar = default;
                entry = default;

                if (FileSystemUtils.TryGetScheme(path, out var schemespan))
                {
                    if (!schemespan.SequenceEqual("phar".AsSpan()))
                    {
                        return false;
                    }

                    // slice off the scheme://
                    path = path.Slice(schemespan.Length + 3);
                }

                if (path.IsEmpty)
                {
                    return false;
                }

                // find the phar
                for (int slash = 0; slash <= path.Length; slash++)
                {
                    if (slash == path.Length || PathUtils.IsDirectorySeparator(path[slash]))
                    {
                        var pharpath = path.Slice(0, slash);

                        phar = resolver(pharpath.ToString());

                        if (phar != null)
                        {
                            entry = slash < path.Length ? path.Slice(slash + 1) : ReadOnlySpan<char>.Empty;
                            return true;
                        }
                    }
                }

                //
                return false;
            }

            public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
            {
                if (TryResolvePhar(ctx, path.AsSpan(), out var phar, out var entry))
                {
                    // Template: phar://alias/entryName
                    var resource = PharExtensions.GetResourceStream(phar, entry);

                    if (resource != null)
                    {
                        return new NativeStream(ctx, resource, this, StreamAccessOptions.UseText | StreamAccessOptions.Read, path, context);
                    }
                    else
                    {
                        // TODO: entry not found
                    }
                }
                else
                {
                    // TODO: phar alias nor phar file not found
                }

                return null;
            }

            public override void ResolvePath(Context ctx, ref string path)
            {
                // resolve the phar alias or phar relative path
                // phar://filename.phar/entryname -> phar://resolve_filename.phar/entryname
                if (TryResolvePhar(ctx, path.AsSpan(), out var phar, out var entry))
                {
                    path = $"{scheme}://{phar.PharFile}/{entry.ToString()}";
                }
            }

            public override StatStruct Stat(string root, string path, StreamStatOptions options, StreamContext context, bool streamStat)
            {
                // path is already normalized using ResolvePath method
                // phar://{pharFile}/{entry}

                if (TryResolvePhar(pharpath => PharExtensions.TryGetPhar(pharpath), path.AsSpan(), out var phar, out var entry))
                {
                    if (entry.IsEmpty)
                    {
                        // phar stub itself
                        return new StatStruct(st_mode: FileModeFlags.File | FileModeFlags.Read);
                    }

                    // pharfile.phar/entryname
                    if (phar.Scripts.IndexOf($"{phar.PharFile}{CurrentPlatform.DirectorySeparator}{entry.ToString()}") >= 0)
                    {
                        return new StatStruct(st_mode: FileModeFlags.File | FileModeFlags.Read);
                    }

                    if (GetResourceContent(phar, entry) != null)
                    {
                        return new StatStruct(st_mode: FileModeFlags.File | FileModeFlags.Read);
                    }

                    // TODO: directory
                }

                return StatStruct.Invalid;
            }

            public override List<string> Listing(string root, string path, StreamListingOptions options, StreamContext context)
            {
                // path is already normalized using ResolvePath method
                // phar://{pharFile}/{entry}

                if (TryResolvePhar(pharpath => PharExtensions.TryGetPhar(pharpath), path.AsSpan(), out var phar, out var entry))
                {
                    // TODO: list entries in given phar

                }

                return base.Listing(root, path, options, context);
            }

            #endregion

            public override bool ResolveInclude(Context ctx, string cd, string path, out Context.ScriptInfo script)
            {
                // Template: include "phar://{path}"

                if (TryResolvePhar(ctx, path.AsSpan(), out var phar, out var entry))
                {
                    Debug.Assert(phar.PharFile.EndsWith(PharExtensions.PharExtension, CurrentPlatform.PathStringComparison));
                    var pharPath = PharExtensions.PharEntryRelativePath(phar.PharFile, entry);
                    script = Context.TryGetDeclaredScript(pharPath);
                    return script.IsValid;
                }

                // invalid
                script = default;
                return false;
            }
        }

        public const string ExtensionName = "phar";

        public PharExtension()
        {
            //PhpFilter.AddSystemFilter(new PharFilterFactory());
            StreamWrapper.RegisterSystemWrapper(new PharWrapper());
            //RegisterLegacyOptions();
        }

        ///// <summary>
        ///// Gets, sets, or restores a value of a legacy configuration option.
        ///// </summary>
        //private static PhpValue GetSet(Context ctx, IPhpConfigurationService config, string option, PhpValue value, IniAction action)
        //{
        //    Debug.Fail("Option '" + option + "' is not currently supported.");
        //    return PhpValue.Null;
        //}

        ///// <summary>
        ///// Registers legacy ini-options.
        ///// </summary>
        //static void RegisterLegacyOptions()
        //{
        //    const string s = ExtensionName;
        //    GetSetDelegate d = new GetSetDelegate(GetSet);

        //    Register("zlib.output_compression", IniFlags.Supported | IniFlags.Global, d, s);
        //    Register("zlib.output_compression_level", IniFlags.Supported | IniFlags.Global, d, s);
        //    Register("zlib.output_handler", IniFlags.Supported | IniFlags.Global, d, s);
        //}
    }
}
