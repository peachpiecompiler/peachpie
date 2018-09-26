using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library.Phar
{
    internal sealed class PharExtension
    {
        sealed class PharWrapper : StreamWrapper, Context.IIncludeResolver
        {
            public static string scheme => "phar";

            #region StreamWrapper

            public override string Label => "phar";

            public override string Scheme => scheme;

            public override bool IsUrl => true;

            public override PhpStream Open(Context ctx, ref string path, string mode, StreamOpenOptions options, StreamContext context)
            {
                if (FileSystemUtils.TryGetScheme(path, out var schemespan))
                {
                    var schemeends = schemespan.Length + 3;
                    var sep = path.IndexOfAny(PathUtils.DirectorySeparatorChars, schemeends);
                    if (sep >= 0)
                    {
                        Stream resource = null;

                        var alias = path.Substring(schemeends, sep - schemeends);
                        var pharFile = PharExtensions.AliasToPharFile(ctx, alias);
                        if (pharFile != null)
                        {
                            // Template: phar://alias/entryName
                            resource = PharExtensions.GetResourceStream(pharFile, path.Substring(sep + 1));
                        }
                        else
                        {
                            // Template: phar://path_phar_file/entryName
                            var pharExt = path.IndexOfOrdinal(PharExtensions.PharExtension, schemeends, path.Length - schemeends);
                            if (pharExt >= 0 && pharExt + PharExtensions.PharExtension.Length + 1 < path.Length)
                            {
                                // path_phar_file:
                                var pharPath = path.Substring(schemeends, pharExt + PharExtensions.PharExtension.Length - schemeends);

                                // entryName:
                                var entryName = path.Substring(pharExt + PharExtensions.PharExtension.Length + 1);

                                // ensure phar is loaded
                                // TODO: locate pharPath and get containing System.Reflection.Assembly
                                throw new NotImplementedException();
                            }
                        }

                        if (resource != null)
                        {
                            return new NativeStream(ctx, resource, this, StreamAccessOptions.UseText | StreamAccessOptions.Read, path, context);
                        }
                    }
                }

                return null;
            }

            #endregion

            public Context.ScriptInfo ResolveScript(Context ctx, string cd, string path)
            {
                // Template: include "phar://{path}"

                var sep = path.IndexOfAny(PathUtils.DirectorySeparatorChars);
                if (sep >= 0)
                {
                    var pharFile = PharExtensions.AliasToPharFile(ctx, path.Remove(sep));
                    if (pharFile != null)
                    {
                        Debug.Assert(pharFile.EndsWith(PharExtensions.PharExtension, CurrentPlatform.PathStringComparison));
                        var pharPath = PharExtensions.PharEntryRelativePath(pharFile, path.Substring(sep + 1));
                        return Context.TryGetDeclaredScript(pharPath);
                    }
                }

                // invalid
                return default;
            }
        }

        public const string ExtensionName = "phar";

        public PharExtension()
        {
            var wrapper = new PharWrapper();

            //PhpFilter.AddSystemFilter(new PharFilterFactory());
            StreamWrapper.SystemStreamWrappers.Add(PharWrapper.scheme, wrapper);
            Context.IncludeProvider.Instance.RegisterSchemeIncluder("phar", wrapper);
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
