using System;
using System.Collections.Generic;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Text;
using Pchp.CodeAnalysis;
using Peachpie.CodeAnalysis.Utilities;
using TSpan = Devsense.PHP.Text.Span;
using TValue = Devsense.PHP.Syntax.SemanticValueType;

namespace Peachpie.CodeAnalysis.Syntax
{
    /// <summary>
    /// Tokens provider that matches additional syntax patters,
    /// stores them in <see cref="NodesFactory"/> and
    /// does not pass used tokens to the <see cref="GetNextToken"/>.
    /// </summary>
    sealed class AdditionalSyntaxProvider : ITokenProvider<TValue, TSpan>, IDisposable
    {
        readonly PhpTokenProvider _provider;
        readonly INodesFactory<LangElement, TSpan> _nodes;

        public AdditionalSyntaxProvider(PhpTokenProvider provider, INodesFactory<LangElement, TSpan> nodes)
        {
            _provider = provider ?? throw ExceptionUtilities.ArgumentNull();
            _nodes = nodes ?? throw ExceptionUtilities.ArgumentNull();
        }

        #region ITokenProvider

        public TValue TokenValue => _provider.TokenValue;

        public TSpan TokenPosition => _provider.TokenPosition;

        public string TokenText => _provider.TokenText;

        public PHPDocBlock DocBlock { get => _provider.DocBlock; set => _provider.DocBlock = value; }

        public int GetNextToken()
        {
            var t = _provider.GetNextToken();

            if (TryMatchSyntaxExtension((Tokens)t))
            {
                // something has changed,
                // get the new "NextToken"
                t = (int)_provider.Token;
            }

            return t;
        }

        public void ReportError(string[] expectedTokens) => ((ITokenProvider<TValue, TSpan>)_provider).ReportError(expectedTokens);

        public void Dispose()
        {
            ((IDisposable)_provider).Dispose();
        }

        #endregion

        bool TryMatchSyntaxExtension(Tokens t)
        {
            // 
            // GENERICS:
            // 

            // T_NEW QualifiedName "<GenericTypes>"             // new classname<A,B,C>
            // QualifiedName "<GenericTypes>" T_DOUBLE_COLON    // classname<A,B,C>::
            // QualifiedName "<GenericTypes>" T_LPAREN          // fooname<A,B,C>(

            //
            // CUSTOM ATTRIBUTES:
            //

            // [QualifiedName] final? class|interface|trait
            // [QualifiedName(Value, PropertyName = Value)] final? class|interface|trait

            //
            // TYPED PROPERTIES:
            //

            // T_VAR|T_STATIC|T_PUBLIC|T_PRIVATE|T_PROTECTED (VariableName)+ "T_COLON QualifiedName" T_SEMI  // var $pname : A;

            return false;
        }
    }
}
