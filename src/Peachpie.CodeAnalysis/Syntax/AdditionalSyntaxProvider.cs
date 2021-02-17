using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
using TSpan = Devsense.PHP.Text.Span;
using TValue = Devsense.PHP.Syntax.SemanticValueType;

namespace Peachpie.CodeAnalysis.Syntax
{
    /// <summary>
    /// Tokens provider that matches additional syntax patterns,
    /// stores them in <see cref="NodesFactory"/> and
    /// does not pass used tokens to the <see cref="GetNextToken"/>.
    /// </summary>
    sealed class AdditionalSyntaxProvider : ITokenProvider<TValue, TSpan>, IDisposable
    {
        readonly PhpTokenProvider _provider;
        readonly NodesFactory _nodes;

        /// <summary>
        /// Factory for <see cref="TypeRef"/>, translates given qualified name using current naming context.
        /// Arguments: (<see cref="QualifiedNameRef"/> typeName, <see cref="bool"/> allowPrimitiveTypeNames).
        /// </summary>
        readonly Func<QualifiedNameRef, bool, TypeRef> _typeRefFactory;

        public AdditionalSyntaxProvider(PhpTokenProvider provider, NodesFactory nodes, Func<QualifiedNameRef, bool, TypeRef> typeRefFactory)
        {
            _provider = provider ?? throw ExceptionUtilities.ArgumentNull();
            _nodes = nodes ?? throw ExceptionUtilities.ArgumentNull();
            _typeRefFactory = typeRefFactory ?? throw ExceptionUtilities.ArgumentNull();
        }

        #region ITokenProvider

        public TValue TokenValue
        {
            get => _provider.TokenValue;
            set => _provider.TokenValue = value;
        }

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
            /* 
             * NOTE:
             * 
             * Here we try to match our extended syntax (quietly) and consume additional tokens.
             * 
             */

            // 
            // GENERICS:
            // 

            // T_NEW QualifiedName "<GenericTypes>"             // new classname<A,B,C> // see _followsNewKeyword
            // QualifiedName "<GenericTypes>" T_DOUBLE_COLON    // classname<A,B,C>::
            // QualifiedName "<GenericTypes>" T_LPAREN          // fooname<A,B,C>(
            if (t == Tokens.T_LT)
            {
                if (_lastToken == Tokens.T_STRING)
                {
                    // try match "<GenericTypes>" : List<TypeRef>
                    int p = 0;
                    if (MatchGenericTypes(ref p, out var types))
                    {
                        var next = NextToken(ref p);
                        if (_followsNewKeyword || (next.Token == Tokens.T_DOUBLE_COLON || next.Token == Tokens.T_LPAREN))
                        {
                            // we got it!
                            _nodes.AddAnnotation(_provider.TokenPosition.Start, types);
                            _provider.Remove(0, p - 1);

                            return true;
                        }
                    }
                }
            }

            ////
            //// CUSTOM ATTRIBUTES:
            ////

            //// [QualifiedName]
            //// [QualifiedName(Value, PropertyName = Value)]
            //// - must be followed: final|class|interface|trait|public|protected|private|static|function|abstract
            //if (t == Tokens.T_LBRACKET)
            //{
            //    // must be prefixed: ;, {, }, <?
            //    if (_lastToken == 0 ||
            //        _lastToken == Tokens.T_SEMI || _lastToken == Tokens.T_OPEN_TAG ||
            //        _lastToken == Tokens.T_RBRACE || _lastToken == Tokens.T_LBRACE)
            //    {
            //        int p = 0;
            //        if (MatchCustomAttribute(ref p, out var attribute) && IsAtDeclarationKeyword(p))
            //        {
            //            if (_consumedCustomAttrs == null) _consumedCustomAttrs = new List<SourceCustomAttribute>(1);
            //            _consumedCustomAttrs.Add(attribute);

            //            _provider.Remove(0, p);
            //            return true;
            //        }
            //    }
            //}

            // T_FN in a qualified name
            // allows to compile older pre-fn syntax

            // \Fn
            // class Fn
            if (t == Tokens.T_FN && (_lastToken == Tokens.T_NS_SEPARATOR || _lastToken == Tokens.T_CLASS))
            {
                // TODO: warning, use of reserved identifier "fn"
                var text = _provider.TokenText;

                _provider.UpdateToken(new CompleteToken(Tokens.T_STRING, new TValue { String = text }, _provider.TokenPosition, text));
                return true;
            }

            //

            if (t != Tokens.T_WHITESPACE && t != Tokens.T_COMMENT && t != Tokens.T_DOC_COMMENT)
            {
                // remember last (non-whitespace) token
                _lastToken = t;

                // remember we are in "T_NEW QualifiedName" context:
                if (_followsNewKeyword)
                {
                    if (t != Tokens.T_STRING &&
                        t != Tokens.T_NS_SEPARATOR)
                    {
                        _followsNewKeyword = false;
                    }
                }
                else if (t == Tokens.T_NEW)
                {
                    _followsNewKeyword = true;
                }

                //// push collected attributes to "nodes"
                //if (_consumedCustomAttrs != null)
                //{
                //    foreach (var attr in _consumedCustomAttrs)
                //    {
                //        _nodes.AddAnnotation(_provider.TokenPosition.Start, attr);
                //    }
                //    _consumedCustomAttrs = null;
                //}
            }

            return false;
        }

        bool _followsNewKeyword = false;
        Tokens _lastToken = default; // last processed token so we don't lookup if not necessary
        //List<SourceCustomAttribute> _consumedCustomAttrs;

        //bool IsAtDeclarationKeyword(int idx)
        //{
        //    var next = NextToken(ref idx);

        //    switch (next.Token)
        //    {
        //        case Tokens.T_FINAL:
        //        case Tokens.T_CLASS:
        //        case Tokens.T_INTERFACE:
        //        case Tokens.T_TRAIT:
        //        case Tokens.T_PUBLIC:
        //        case Tokens.T_PROTECTED:
        //        case Tokens.T_PRIVATE:
        //        case Tokens.T_STATIC:
        //        case Tokens.T_FUNCTION:
        //        case Tokens.T_ABSTRACT:
        //        case Tokens.T_VAR:
        //        case Tokens.T_LBRACKET:
        //            return true;

        //        default:
        //            return false;
        //    }
        //}

        #region Match*

        ///// <summary>
        ///// Matches a custom attribute syntax.
        ///// </summary>
        //bool MatchCustomAttribute(ref int idx, out SourceCustomAttribute attribute)
        //{
        //    attribute = default;
        //    var p = idx;

        //    // [
        //    if (MatchToken(ref p, Tokens.T_LBRACKET))
        //    {
        //        // "QualifiedName"
        //        if (MatchQualifiedName(ref p, out var qname))
        //        {
        //            List<KeyValuePair<Name, LangElement>> arguments = null;

        //            // ( ... )
        //            if (MatchToken(ref p, Tokens.T_LPAREN))
        //            {
        //                bool hasProperty = false;

        //                // property = value
        //                // value
        //                while (MatchAttributeValue(ref p, out var property, out var value))
        //                {
        //                    // quickly check the syntax makes sense:
        //                    if (property.Value != null)
        //                    {
        //                        hasProperty = true;
        //                    }
        //                    else
        //                    {
        //                        if (hasProperty) return false; // ERR: constructor argument after property assignment
        //                    }

        //                    // property = value
        //                    if (arguments == null) arguments = new List<KeyValuePair<Name, LangElement>>();

        //                    arguments.Add(new KeyValuePair<Name, LangElement>(property, value));

        //                    // ,
        //                    if (NextToken(ref p).Token == Tokens.T_COMMA)
        //                        continue;

        //                    p--;
        //                    break;
        //                }

        //                if (NextToken(ref p).Token != Tokens.T_RPAREN)
        //                {
        //                    return false; // ERR: ')' expected
        //                }
        //            }

        //            // ]
        //            if (MatchToken(ref p, Tokens.T_RBRACKET, out var rbrtoken))
        //            {
        //                // ensure the qname ends with "Attribute":
        //                const string suffix = "Attribute";
        //                if (!qname.QualifiedName.Name.Value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    qname = new QualifiedNameRef(
        //                        qname.Span,
        //                        new Name(qname.QualifiedName.Name.Value + suffix),
        //                        qname.QualifiedName.Namespaces,
        //                        qname.QualifiedName.IsFullyQualifiedName);
        //                }

        //                //
        //                attribute = new SourceCustomAttribute(_typeRefFactory(qname, false), arguments);
        //                idx = p;
        //                return true;
        //            }
        //        }
        //    }

        //    //
        //    return false;
        //}

        //bool MatchAttributeValue(ref int idx, out Name property, out LangElement value)
        //{
        //    var oldidx = idx;

        //    // T_STRING = "Value"
        //    if (MatchToken(ref idx, Tokens.T_STRING, out var identifier) &&
        //        MatchToken(ref idx, Tokens.T_EQ))
        //    {
        //        property = new Name(identifier.TokenValue.String);
        //    }
        //    else
        //    {
        //        property = default;
        //        idx = oldidx;
        //    }

        //    // Value:

        //    if (MatchValue(ref idx, out value))
        //    {
        //        return true;
        //    }

        //    // no match
        //    idx = oldidx;
        //    return false;
        //}

        //bool MatchValue(ref int idx, out LangElement value)
        //{
        //    var oldidx = idx;

        //    // T_DNUMBER, T_LNUMBER, T_CONSTANT_ENCAPSED_STRING
        //    if (MatchLiteral(ref idx, out var lit))
        //    {
        //        value = lit;
        //        return true;
        //    }

        //    // typeof("QualifiedName")
        //    if (MatchTypeOfQualifiedName(ref idx, out var typename))
        //    {
        //        // Template: TypeRef : System.Type
        //        value = _typeRefFactory(typename, true);
        //        return true;
        //    }

        //    // "QualifiedName", "QualifiedName"::T_STRING
        //    if (MatchQualifiedName(ref idx, out var qname))
        //    {
        //        var tref = _typeRefFactory(qname, false);    // translate qname using parser's current naming context

        //        if (MatchToken(ref idx, Tokens.T_DOUBLE_COLON))
        //        {
        //            // ::T_STRING
        //            if (MatchToken(ref idx, Tokens.T_STRING, out var nametoken))
        //            {
        //                value = _nodes.ClassConstUse(default,
        //                    tref,
        //                    new Name(nametoken.TokenValue.String),
        //                    nametoken.TokenPosition);
        //                return true;
        //            }
        //            // ERR
        //        }
        //        else
        //        {
        //            // GlobalConst
        //            value = _nodes.ConstUse(
        //                default,
        //                new TranslatedQualifiedName(tref.QualifiedName.Value, default));
        //            return true;
        //        }
        //    }

        //    // no match
        //    idx = oldidx;
        //    value = default;
        //    return false;
        //}

        //bool MatchLiteral(ref int idx, out Literal value)
        //{
        //    var oldidx = idx;

        //    var token = NextToken(ref idx);
        //    switch (token.Token)
        //    {
        //        case Tokens.T_LNUMBER:
        //            value = _nodes.Literal(token.TokenPosition, token.TokenValue.Long);
        //            return true;
        //        case Tokens.T_DNUMBER:
        //            value = _nodes.Literal(token.TokenPosition, token.TokenValue.Double);
        //            return true;
        //        case Tokens.T_CONSTANT_ENCAPSED_STRING:
        //            value = (Literal)_nodes.Literal(token.TokenPosition, token.TokenValue.String, originalValue: null);
        //            return true;
        //    }

        //    // rollback
        //    idx = oldidx;
        //    value = default;
        //    return false;
        //}

        //bool MatchTypeOfQualifiedName(ref int idx, out QualifiedNameRef qname)
        //{
        //    // Template:
        //    // T_STRING("typeof") '(' "QualifiedName" ')'

        //    var p = idx;

        //    if (MatchToken(ref p, Tokens.T_STRING, out var t) && t.TokenValue.String == "typeof" && // typeof
        //        MatchToken(ref p, Tokens.T_LPAREN) &&   // (
        //        MatchQualifiedName(ref p, out qname) && // "QualifiedName"
        //        MatchToken(ref p, Tokens.T_RPAREN))     // )
        //    {
        //        idx = p;
        //        return true;
        //    }

        //    //
        //    qname = default;
        //    return false;
        //}

        /// <summary>
        /// Matches token at position and advances the position to next token.
        /// If token does not match, function return <c>false</c> and position is not advanced.
        /// </summary>
        bool MatchToken(ref int idx, Tokens t) => MatchToken(ref idx, t, out var _, false);

        /// <summary>
        /// Matches token at position and advances the position to next token.
        /// If token does not match, function return <c>false</c> and position is not advanced.
        /// </summary>
        bool MatchToken(ref int idx, Tokens t, out CompleteToken token, bool ensureTokenText = false)
        {
            var oldidx = idx;

            token = NextToken(ref idx);

            if (token.Token == t)
            {
                if (ensureTokenText)
                {
                    token = _provider.WithTokenText(token);
                }

                return true;
            }

            // rollback
            idx = oldidx;
            return false;
        }

        CompleteToken NextToken(ref int idx)
        {
            CompleteToken token;

            do
            {
                token = _provider.Lookup(idx);
                idx++;
            } while (token.IsWhitespace()); // skip ws

            return token;
        }

        bool MatchQualifiedName(ref int idx, out QualifiedNameRef qname)
        {
            // just an ugly loop that matches "QualifiedName" syntax as far as it can

            // Pattern:
            // \?T_STRING(\T_STRING)*

            qname = default;
            Name name = default;
            List<Name> names = null;
            bool fullyQualified = false;

            int idx2 = idx;
            var span = Span.Invalid;

            for (; ; )
            {
                // \?T_STRING?
                var hassep = MatchToken(ref idx2, Tokens.T_NS_SEPARATOR);
                var hasname = MatchToken(ref idx2, Tokens.T_STRING, out var ntok);

                if (hasname)
                {
                    if (name.Value != null)
                    {
                        if (!hassep) return false;  // ERR: names must be separated by separator
                        if (names == null) names = new List<Name>();
                        names.Add(name);
                    }
                    else
                    {
                        fullyQualified = hassep;
                    }


                    //
                    Debug.Assert(ntok.TokenValue.String != null);
                    name = new Name(ntok.TokenValue.String);
                    span = span.IsValid ? Span.Combine(span, ntok.TokenPosition) : ntok.TokenPosition;
                }
                else
                {
                    if (hassep || name.Value == null) // && !hasname
                    {
                        // ERR: name must follow after the separator
                        // ERR: or name was not matched
                        return false;
                    }

                    //
                    qname = new QualifiedNameRef(
                        span,
                        name,
                        names != null ? names.ToArray() : Array.Empty<Name>(),
                        fullyQualified);
                    idx = idx2;

                    return true;
                }
            }
        }

        /// <summary>
        /// Matches "&lt;T1,T2&gt;".
        /// </summary>
        bool MatchGenericTypes(ref int idx, out List<TypeRef> types)
        {
            types = null;

            if (MatchToken(ref idx, Tokens.T_LT))
            {

                for (; ; )
                {
                    if (MatchQualifiedName(ref idx, out var qname))
                    {
                        // T
                        var tref = _typeRefFactory(qname, true);

                        if (MatchGenericTypes(ref idx, out var nested))
                        {
                            // nested "<GenericTypes>"
                            tref = new GenericTypeRef(tref.Span, tref, nested);
                        }

                        types ??= new List<TypeRef>(1);
                        types.Add(tref);

                        if (MatchToken(ref idx, Tokens.T_COMMA))
                        {
                            // match next T
                            continue;
                        }
                        else if (MatchToken(ref idx, Tokens.T_GT))
                        {
                            // match
                            return true;
                        }
                    }

                    // unexpected token -> exit
                    break;
                }
            }

            //
            return false;
        }

        #endregion
    }
}
