using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System.Text;
using Devsense.PHP.Syntax;

namespace Pchp.CodeAnalysis.DocumentationComments
{
    internal class DocumentationCommentCompiler
    {
        internal static void WriteDocumentationCommentXml(PhpCompilation compilation, string assemblyName, Stream xmlDocStream, DiagnosticBag xmlDiagnostics, CancellationToken cancellationToken)
        {
            if (xmlDocStream != null)
            {
                new DocumentationCommentCompiler(xmlDocStream, xmlDiagnostics, cancellationToken)
                    .WriteCompilation(compilation, assemblyName)
                    .Dispose();
            }
        }

        readonly StreamWriter _writer;
        readonly DiagnosticBag _xmlDiagnostics;
        readonly CancellationToken _cancellationToken;

        private DocumentationCommentCompiler(Stream xmlDocStream, DiagnosticBag xmlDiagnostics, CancellationToken cancellationToken)
        {
            _writer = new StreamWriter(xmlDocStream, Encoding.UTF8);
            _xmlDiagnostics = xmlDiagnostics;
            _cancellationToken = cancellationToken;
        }

        static string XmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            int len = text.Length;
            var encodedText = new StringBuilder(len + 8);

            for (int i = 0; i < len; ++i)
            {
                char ch = text[i];
                switch (ch)
                {
                    case '<':
                        encodedText.Append("&lt;");
                        break;
                    case '>':
                        encodedText.Append("&gt;");
                        break;
                    case '&':
                        encodedText.Append("&amp;");
                        break;
                    default:
                        encodedText.Append(ch);
                        break;
                }
            }

            return encodedText.ToString();
        }

        DocumentationCommentCompiler WriteCompilation(PhpCompilation compilation, string assemblyName)
        {
            _writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            _writer.WriteLine("<doc>");
            _writer.WriteLine("<assembly><name>{0}</name></assembly>", assemblyName);
            _writer.WriteLine("<members>");

            // TODO: implement Symbol.GetDocumentationCommentId
            // TODO: implement Symbol.GetDocumentationCommentXml

            var tables = compilation.SourceSymbolCollection;
            tables.GetFiles().ForEach(WriteFile);
            tables.GetTypes().ForEach(WriteType);
            tables.AllRoutines.ForEach(WriteRoutine);

            _writer.WriteLine("</members>");
            _writer.WriteLine("</doc>");

            // TODO: analyse PHPDoc and report into xmlDiagnostics

            //
            return this;
        }

        void Dispose()
        {
            _writer.Flush();
            _writer.Dispose();
        }

        void WriteSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary)) return;

            summary = summary.Trim();

            if (summary.Length == 0) return;

            _writer.WriteLine("<summary>");
            _writer.Write(XmlEncode(summary));
            _writer.WriteLine();
            _writer.WriteLine("</summary>");
        }

        void WriteParam(string pname, string pdesc, string type = null)
        {
            if (string.IsNullOrWhiteSpace(pdesc) && string.IsNullOrEmpty(type))
            {
                return;
            }

            //

            _writer.Write("<param name=\"");
            _writer.Write(XmlEncode(pname));
            _writer.Write('\"');

            if (!string.IsNullOrEmpty(type))
            {
                // type="int|string|bool"
                _writer.Write(" type=\"");
                _writer.Write(XmlEncode(type));
                _writer.Write('\"');
            }

            _writer.Write('>');
            _writer.Write(XmlEncode(pdesc));
            _writer.WriteLine("</param>");
        }

        void WriteRoutine(SourceRoutineSymbol routine)
        {
            var ps = routine.Parameters;

            //
            _writer.WriteLine($"<member name=\"{CommentIdResolver.GetId(routine)}\">");

            // PHPDoc
            var phpdoc = routine.PHPDocBlock;
            if (phpdoc != null)
            {
                WriteSummary(phpdoc.Summary);

                // user parameters
                foreach (var p in phpdoc.Params)
                {
                    // TODO: note the parameter type into Doc comment

                    if (p.VariableName != null)
                    {
                        WriteParam(p.VariableName.TrimStart('$'), p.Description, p.TypeNames);
                    }
                }
                var rtag = phpdoc.Returns;
                if (rtag != null && !string.IsNullOrWhiteSpace(rtag.Description))
                {
                    _writer.WriteLine("<returns>{0}</returns>", XmlEncode(rtag.Description));
                }
            }

            // TODO: <exception> ... if any

            //// implicit parameters
            //foreach (var p in ps)
            //{
            //    if (p.IsImplicitlyDeclared)
            //    {
            //        if (SpecialParameterSymbol.IsContextParameter(p))
            //        {
            //            // WriteParam(p.MetadataName, PhpResources.XmlDoc_ContextParamDescription);
            //        }
            //    }
            //    else
            //    {
            //        break;  // implicit parameters are always at begining
            //    }
            //}

            _writer.WriteLine("</member>");

        }

        void WriteType(SourceTypeSymbol type)
        {
            _writer.WriteLine($"<member name=\"{CommentIdResolver.GetId(type)}\">");
            var phpdoc = type.Syntax?.PHPDoc;
            if (phpdoc != null)
            {
                WriteSummary(phpdoc.Summary);
            }
            _writer.WriteLine("</member>");

            foreach (var field in type.GetMembers().OfType<SourceFieldSymbol>())
            {
                if ((phpdoc = field.PhpDocBlock) != null)
                {
                    var summary = phpdoc.Summary;
                    var value = string.Empty;
                    if (string.IsNullOrEmpty(summary))
                    {
                        // try @var or @staticvar:
                        var vartag = field.FindPhpDocVarTag();
                        if (vartag != null)
                        {
                            summary = vartag.Description;

                            if (!string.IsNullOrEmpty(vartag.TypeNames))
                            {
                                value = string.Format("<value>{0}</value>", XmlEncode(vartag.TypeNames));
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        _writer.WriteLine($"<member name=\"{CommentIdResolver.GetId(field)}\">");
                        WriteSummary(summary);
                        _writer.WriteLine(value);
                        _writer.WriteLine("</member>");
                    }
                }
            }
        }

        void WriteFile(SourceFileSymbol file)
        {

        }
    }
}
