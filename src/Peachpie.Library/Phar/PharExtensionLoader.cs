using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library.Phar
{
    internal sealed class PharExtension
    {
        sealed class Wrapper : Context.IIncludeResolver
        {
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
            var wrapper = new Wrapper();

            //PhpFilter.AddSystemFilter(new PharFilterFactory());
            //StreamWrapper.SystemStreamWrappers.Add(PharStreamWrapper.scheme, wrapper);
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
