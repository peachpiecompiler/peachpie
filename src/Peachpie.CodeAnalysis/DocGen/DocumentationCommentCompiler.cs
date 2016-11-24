using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System.Text;

namespace Pchp.CodeAnalysis.DocGen
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
            _writer = new StreamWriter(xmlDocStream);
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
            _writer.Write("<?xml version=\"1.0\"?>");
            _writer.Write("<doc>");
            _writer.Write("<assembly><name>{0}</name></assembly>", assemblyName);
            _writer.Write("<members>");

            var tables = compilation.SourceSymbolTables;
            tables.GetFiles().ForEach(WriteFile);
            tables.GetTypes().OfType<SourceTypeSymbol>().ForEach(WriteType);
            tables.AllRoutines.ForEach(WriteRoutine);

            _writer.Write("</members>");
            _writer.Write("</doc>");

            // TODO: analyse PHPDoc and report into xmlDiagnostics

            //
            return this;
        }

        void Dispose()
        {
            _writer.Flush();
            _writer.Dispose();
        }

        void WriteRoutine(SourceRoutineSymbol routine)
        {
            // TODO: summary, @param, @return
        }

        void WriteType(SourceTypeSymbol type)
        {
            _writer.Write($"<member name=\"{CommentIdResolver.GetId(type)}\">");
            var phpdoc = type.Syntax?.PHPDoc;
            if (phpdoc != null)
            {                
                _writer.Write("<summary>");
                _writer.Write(XmlEncode(phpdoc.Summary));
                _writer.Write("</summary>");
            }
            _writer.Write("</member>");
        }

        void WriteFile(SourceFileSymbol file)
        {

        }
    }
}
