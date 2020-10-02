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

                // NOTE: currently, only mapped phars are resolved!
                // TODO: resolve not-mapped phars (ResolvePath ... find .phar in compiled scripts ...)

                // find the phar
                for (int slash = 0; slash <= path.Length; slash++)
                {
                    if (slash == path.Length || PathUtils.IsDirectorySeparator(path[slash]))
                    {
                        var pharpath = path.Slice(0, slash);

                        phar = PharExtensions.AliasToPharFile(ctx, pharpath.ToString());

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
