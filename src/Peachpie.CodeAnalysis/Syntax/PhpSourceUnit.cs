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
        readonly LanguageFeatures _features;
        readonly Lexer.LexicalStates _state;

        public SourceText SourceText { get; set; }

        public PhpSourceUnit(string filePath, SourceText source, Encoding encoding = null, LanguageFeatures features = LanguageFeatures.Basic, Lexer.LexicalStates initialState = Lexer.LexicalStates.INITIAL)
            : base(filePath, encoding ?? Encoding.UTF8, CreateLineBreaks(source))
        {
            _features = features;
            _state = initialState;

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

        public override void Parse(INodesFactory<LangElement, Span> factory, IErrorSink<Span> errors, IErrorRecovery recovery = null)
        {
            using (var source = new StringReader(SourceText.ToString()))
            {
                using (var provider = new AdditionalSyntaxProvider(
                    new PhpTokenProvider(
                        new Lexer(source, Encoding.UTF8, errors, _features, 0, _state),
                        this),
                    factory))
                {
                    ast = new Parser().Parse(provider, factory, _features, errors, recovery);
                }
            }
        }

        static ILineBreaks CreateLineBreaks(SourceText source)
        {
            return Devsense.PHP.Text.LineBreaks.Create(
                source.ToString(),
                source.Lines.Select(line => line.EndIncludingLineBreak).ToList());
        }
    }
}
