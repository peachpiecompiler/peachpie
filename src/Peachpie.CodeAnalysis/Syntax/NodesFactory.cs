using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Peachpie.CodeAnalysis.Syntax
{
    /// <summary>
    /// Provides nodes instantiation for underlaying parser
    /// and collects instantiated nodes.
    /// </summary>
    sealed class NodesFactory : BasicNodesFactory
    {
        /// <summary>
        /// Gets constructed lambda nodes.
        /// </summary>
        public List<LambdaFunctionExpr> Lambdas => _lambdas;
        List<LambdaFunctionExpr> _lambdas;

        /// <summary>
        /// Gets constructed type declaration nodes.
        /// </summary>
        public List<TypeDecl> Types => _types;
        List<TypeDecl> _types;

        /// <summary>
        /// Gets constructed function declaration nodes.
        /// </summary>
        public List<FunctionDecl> Functions => _functions;
        List<FunctionDecl> _functions;

        /// <summary>
        /// Gets constructed global code (ast root).
        /// </summary>
        public GlobalCode Root { get; private set; }

        /// <summary>
        /// Gets constructed yield extpressions.
        /// </summary>
        public List<LangElement> YieldNodes => _yieldNodes;
        List<LangElement> _yieldNodes;

        /// <summary>
        /// Adds node to the list and returns the node.
        /// </summary>
        static T AddAndReturn<T>(ref List<T> list, T node)
        {
            if (list == null)
            {
                list = new List<T>();
            }

            list.Add(node);
            return node;
        }

        public void AddAnnotation(int position, object obj)
        {
            AddAndReturn(ref _annotations, (position, obj));
        }
        List<(int, object)> _annotations; // list of parsed custom attributes, will be taken and used for the next declaration

        /// <summary>
        /// Gets an additional annotation if any.
        /// </summary>
        /// <typeparam name="T">Annotation type.</typeparam>
        /// <param name="position">Position of the annotation.</param>
        /// <param name="obj">Resulting object.</param>
        /// <returns>Wtehher the annotation was found.</returns>
        bool TryGetAnotation<T>(int position, out T obj)
        {
            // check Span contains position => add to Properties
            if (_annotations != null)
            {
                for (int i = _annotations.Count - 1; i >= 0; i--)
                {
                    if (_annotations[i].Item1 == position && _annotations[i].Item2 is T value)
                    {
                        _annotations.RemoveAt(i);
                        obj = value;
                        return true;
                    }
                }
            }

            //
            obj = default;
            return false;
        }

        /// <summary>
        /// If applicable, annotates the element with previously parsed <see cref="SourceCustomAttribute"/>.
        /// </summary>
        T WithCustomAttributes<T>(T element) where T : LangElement
        {
            while (TryGetAnotation<AttributeData>(element.Span.Start, out var attr))
            {
                element.AddCustomAttribute(attr);
            }

            //
            return element;
        }

        TypeRef WithGenericTypes(TypeRef tref)
        {
            return TryGetAnotation<List<TypeRef>>(tref.Span.End, out var generics)
                ? new GenericTypeRef(tref.Span, tref, generics)
                : tref;
        }

        CallSignature WithGenericTypes(CallSignature signature, Span nameSpan)
        {
            if (TryGetAnotation<List<TypeRef>>(nameSpan.End, out var generics))
            {
                signature.GenericParams = generics.ToArray();
            }

            return signature;
        }

        public override LangElement GlobalCode(Span span, IEnumerable<LangElement> statements, NamingContext context)
        {
            Debug.Assert(_annotations == null || _annotations.Count == 0, $"file {this.SourceUnit.FilePath} contains CLR annotations we did not consume! Probably a bogus in AdditionalSyntaxProvider."); // all parsed custom annotations have to be consumed

            return Root = (GlobalCode)base.GlobalCode(span, statements, context);
        }

        public override LangElement Function(Span span, bool conditional, bool aliasReturn, PhpMemberAttributes attributes, TypeRef returnType, Name name, Span nameSpan, IEnumerable<FormalTypeParam> typeParamsOpt, IEnumerable<FormalParam> formalParams, Span formalParamsSpan, LangElement body)
        {
            return AddAndReturn(ref _functions,
                WithCustomAttributes((FunctionDecl)base.Function(span, conditional, aliasReturn, attributes, returnType, name, nameSpan, typeParamsOpt, formalParams, formalParamsSpan, body)));
        }

        public override LangElement Type(Span span, Span headingSpan, bool conditional, PhpMemberAttributes attributes, Name name, Span nameSpan, IEnumerable<FormalTypeParam> typeParamsOpt, INamedTypeRef baseClassOpt, IEnumerable<INamedTypeRef> implements, IEnumerable<LangElement> members, Span bodySpan)
        {
            var tref = (TypeDecl)base.Type(span, headingSpan, conditional, attributes, name, nameSpan, typeParamsOpt, baseClassOpt, implements, members, bodySpan);

            return AddAndReturn(ref _types, WithCustomAttributes(tref));
        }

        public override LangElement DeclList(Span span, PhpMemberAttributes attributes, IList<LangElement> decls, TypeRef type)
        {
            return WithCustomAttributes(base.DeclList(span, attributes, decls, type));
        }

        public override LangElement Method(Span span, bool aliasReturn, PhpMemberAttributes attributes, TypeRef returnType, Span returnTypeSpan, string name, Span nameSpan, IEnumerable<FormalTypeParam> typeParamsOpt, IEnumerable<FormalParam> formalParams, Span formalParamsSpan, IEnumerable<ActualParam> baseCtorParams, LangElement body)
        {
            return WithCustomAttributes(
                base.Method(span, aliasReturn, attributes, returnType, returnTypeSpan, name, nameSpan, typeParamsOpt, formalParams, formalParamsSpan, baseCtorParams, body));
        }

        public override TypeRef AnonymousTypeReference(Span span, Span headingSpan, bool conditional, PhpMemberAttributes attributes, IEnumerable<FormalTypeParam> typeParamsOpt, INamedTypeRef baseClassOpt, IEnumerable<INamedTypeRef> implements, IEnumerable<LangElement> members, Span bodySpan)
        {
            var tref = (AnonymousTypeRef)base.AnonymousTypeReference(span, headingSpan, conditional, attributes, typeParamsOpt, baseClassOpt, implements, members, bodySpan);

            AddAndReturn(ref _types, tref.TypeDeclaration);

            return tref;
        }

        public override LangElement Lambda(Span span, Span headingSpan, bool aliasReturn, TypeRef returnType, IEnumerable<FormalParam> formalParams, Span formalParamsSpan, IEnumerable<FormalParam> lexicalVars, LangElement body)
        {
            return AddAndReturn(ref _lambdas,
                (LambdaFunctionExpr)base.Lambda(span, headingSpan, aliasReturn, returnType, formalParams, formalParamsSpan, lexicalVars, body));
        }

        public override LangElement Yield(Span span, LangElement keyOpt, LangElement valueOpt)
        {
            return AddAndReturn(ref _yieldNodes, base.Yield(span, keyOpt, valueOpt));
        }

        public override LangElement YieldFrom(Span span, LangElement fromExpr)
        {
            return AddAndReturn(ref _yieldNodes, base.YieldFrom(span, fromExpr));
        }

        public Literal Literal(Span span, long lvalue) => new LongIntLiteral(span, lvalue); // overload to avoid boxing

        public Literal Literal(Span span, double dvalue) => new DoubleLiteral(span, dvalue); // overload to avoid boxing

        public override LangElement Literal(Span span, object value, string originalValue)
        {
            return base.Literal(span, value, originalValue: null);  // discard the original value string, not needed, free some memory
        }

        public override LangElement EncapsedExpression(Span span, LangElement expression, Tokens openDelimiter) => expression;

        public override LangElement StringEncapsedExpression(Span span, LangElement expression, Tokens openDelimiter) => expression;

        public override LangElement HeredocExpression(Span span, LangElement expression, Tokens quoteStyle, string label) => expression;

        public override LangElement Call(Span span, Name name, Span nameSpan, CallSignature signature, TypeRef typeRef)
        {
            return base.Call(span, name, nameSpan, WithGenericTypes(signature, nameSpan), typeRef);
        }

        public override LangElement Call(Span span, TranslatedQualifiedName name, CallSignature signature, LangElement memberOfOpt)
        {
            return base.Call(span, name, WithGenericTypes(signature, name.Span), memberOfOpt);
        }

        public override TypeRef TypeReference(Span span, QualifiedName className)
        {
            return WithGenericTypes(base.TypeReference(span, className));
        }

        public NodesFactory(SourceUnit sourceUnit)
            : base(sourceUnit)
        {
        }
    }
}
