using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Devsense.PHP.Errors;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis;

namespace Peachpie.CodeAnalysis.Syntax
{
    sealed class PhpSourceUnit : SourceUnit
    {
        public SourceText SourceText { get; set; }

        public PhpSourceUnit(string filePath, SourceText source, Encoding encoding = null)
            : base(filePath, encoding ?? Encoding.UTF8, CreateLineBreaks(source))
        {
            this.SourceText = source;
        }

        public override void Close()
        {
            // TODO: dispose SourceText ?
        }

        public override string GetSourceCode(Span span)
        {
            return SourceText.ToString(span.ToTextSpan());
        }

        public void Parse(NodesFactory factory, IErrorSink<Span> errors,
            IErrorRecovery recovery = null,
            LanguageFeatures features = LanguageFeatures.Basic,
            Lexer.LexicalStates state = Lexer.LexicalStates.INITIAL)
        {
            var parser = new Parser();

            using (var source = new StringReader(SourceText.ToString()))
            {
                using (var provider = new AdditionalSyntaxProvider(
                    new PhpTokenProvider(
                        new Lexer(source, Encoding.UTF8, errors, features, 0, state),
                        this),
                    factory,
                    parser.CreateTypeRef))
                {
                    ast = parser.Parse(provider, factory, features, errors, recovery);
                }
            }
        }

        public override void Parse(INodesFactory<LangElement, Span> factory, IErrorSink<Span> errors, IErrorRecovery recovery = null)
        {
            Parse((NodesFactory)factory, errors, recovery, LanguageFeatures.Basic, Lexer.LexicalStates.INITIAL);
        }

        static ILineBreaks CreateLineBreaks(SourceText source)
        {
            return Devsense.PHP.Text.LineBreaks.Create(
                source.ToString(),
                source.Lines.Select(line => line.EndIncludingLineBreak).ToList());
        }
    }
}
