using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Devsense.PHP.Syntax;
using Pchp.Core;
using Pchp.Library;
using Pchp.Library.Streams;
using static Pchp.Library.StandardPhpOptions;

namespace Peachpie.Library.Scripting
{
    [PhpExtension(ExtensionName, Registrator = typeof(Config))]
    public static class Highlight
    {
        public const string ExtensionName = "standard";

        sealed class Config : IPhpConfiguration
        {
            public Config()
            {
                RegisterLegacyOptions();
                Context.RegisterConfiguration(this);
            }

            public string ExtensionName => Highlight.ExtensionName;

            public IPhpConfiguration Copy() => (Config)this.MemberwiseClone();

            public string comment = "#FF8000";
            public string @default = "#0000BB";
            public string keyword = "#007700";
            public string @string = "#DD0000";
            public string html = "#000000";

            /// <summary>
            /// Gets or sets a value of a legacy configuration option.
            /// </summary>
            private static PhpValue GetSet(Context ctx, IPhpConfigurationService config, string option, PhpValue value, IniAction action)
            {
                var local = config.Get<Config>();
                if (local == null)
                {
                    return PhpValue.Null;
                }

                return option switch
                {
                    "highlight.comment" => StandardPhpOptions.GetSet(ref local.comment, null, value, action),
                    "highlight.default" => StandardPhpOptions.GetSet(ref local.@default, null, value, action),
                    "highlight.keyword" => StandardPhpOptions.GetSet(ref local.keyword, null, value, action),
                    "highlight.string" => StandardPhpOptions.GetSet(ref local.@string, null, value, action),
                    "highlight.html" => StandardPhpOptions.GetSet(ref local.html, null, value, action),

                    _ => PhpValue.Null,
                };
            }

            /// <summary>
            /// Registers legacy ini-options.
            /// </summary>
            internal static void RegisterLegacyOptions()
            {
                var d = new GetSetDelegate(GetSet);

                Register("highlight.comment", IniFlags.Supported | IniFlags.Local, d, Highlight.ExtensionName);
                Register("highlight.default", IniFlags.Supported | IniFlags.Local, d, Highlight.ExtensionName);
                Register("highlight.keyword", IniFlags.Supported | IniFlags.Local, d, Highlight.ExtensionName);
                Register("highlight.string", IniFlags.Supported | IniFlags.Local, d, Highlight.ExtensionName);
                Register("highlight.html", IniFlags.Supported | IniFlags.Local, d, Highlight.ExtensionName);
            }
        }

        static Config GetConfig(Context ctx) => ctx.Configuration.Get<Config>();

        /// <summary>
        /// Outputs or returns html code for a highlighted version of the given PHP code.
        /// </summary>
        public static PhpValue highlight_string(Context ctx, string source, bool @return = false)
        {
            var output = StringBuilderUtilities.Pool.Get();
            var config = GetConfig(ctx);

            using (var xmlwriter = XmlWriter.Create(output, new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                Indent = true,
                NewLineHandling = NewLineHandling.Entitize,
                OmitXmlDeclaration = true,
            }))
            {
                xmlwriter.WriteStartElement("code");

                xmlwriter.WriteStartElement("span");
                xmlwriter.WriteAttributeString("style", $"color: {config.html}");

                using (var tokenizer = new Lexer(new StringReader(source ?? string.Empty), Encoding.UTF8))
                {
                    Tokens t;
                    while ((t = tokenizer.GetNextToken()) != Tokens.EOF)
                    {
                        var category = TokensExtension.GetTokenCategory(t, tokenizer);

                        string color = category switch
                        {
                            TokenCategory.Html => null,
                            TokenCategory.Comment => config.comment,
                            TokenCategory.LineComment => config.comment,
                            TokenCategory.Keyword => config.keyword,
                            TokenCategory.String => config.@string,
                            TokenCategory.StringCode => config.@string,
                            _ => config.@default,
                        };

                        if (string.IsNullOrEmpty(color))
                        {
                            xmlwriter.WriteString(tokenizer.TokenText);
                        }
                        else
                        {
                            xmlwriter.WriteStartElement("span");
                            xmlwriter.WriteAttributeString("style", $"color: {color}");

                            xmlwriter.WriteString(tokenizer.TokenText);

                            xmlwriter.WriteEndElement();
                        }
                    }
                }

                xmlwriter.WriteEndElement();
                xmlwriter.WriteEndElement();
            }
            //
            var result = StringBuilderUtilities.GetStringAndReturn(output);

            if (@return)
            {
                return result;
            }
            else
            {
                ctx.Echo(result);
                return true;
            }
        }

        /// <summary>
        /// Syntax highlighting of a file.
        /// </summary>
        public static PhpValue highlight_file(Context ctx, string filename, bool @return = false)
        {
            var stream = PhpStream.Open(ctx, filename, StreamOpenMode.ReadText);
            if (stream != null)
            {
                var source = stream.ReadStringContents(-1);
                return highlight_string(ctx, source, @return);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Alias of <see cref="highlight_file"/>().
        /// </summary>
        public static PhpValue show_source(Context ctx, string filename, bool @return = false) => highlight_file(ctx, filename, @return);
    }
}
