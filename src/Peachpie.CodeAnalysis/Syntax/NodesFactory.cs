using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Text;

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
        public GlobalCode Root => _root;
        GlobalCode _root;

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

        public override LangElement GlobalCode(Span span, IEnumerable<LangElement> statements, NamingContext context)
        {
            return _root = (GlobalCode)base.GlobalCode(span, statements, context);
        }

        public override LangElement Function(Span span, bool conditional, bool aliasReturn, PhpMemberAttributes attributes, TypeRef returnType, Name name, Span nameSpan, IEnumerable<FormalTypeParam> typeParamsOpt, IEnumerable<FormalParam> formalParams, Span formalParamsSpan, LangElement body)
        {
            return AddAndReturn(ref _functions,
                (FunctionDecl)base.Function(span, conditional, aliasReturn, attributes, returnType, name, nameSpan, typeParamsOpt, formalParams, formalParamsSpan, body));
        }

        public override LangElement Type(Span span, Span headingSpan, bool conditional, PhpMemberAttributes attributes, Name name, Span nameSpan, IEnumerable<FormalTypeParam> typeParamsOpt, INamedTypeRef baseClassOpt, IEnumerable<INamedTypeRef> implements, IEnumerable<LangElement> members, Span bodySpan)
        {
            return AddAndReturn(ref _types,
                (TypeDecl)base.Type(span, headingSpan, conditional, attributes, name, nameSpan, typeParamsOpt, baseClassOpt, implements, members, bodySpan));
        }

        public override TypeRef AnonymousTypeReference(Span span, Span headingSpan, bool conditional, PhpMemberAttributes attributes, IEnumerable<FormalTypeParam> typeParamsOpt, INamedTypeRef baseClassOpt, IEnumerable<INamedTypeRef> implements, IEnumerable<LangElement> members, Span bodySpan)
        {
            var tref = (AnonymousTypeRef)base.AnonymousTypeReference(span, headingSpan, conditional, attributes, typeParamsOpt, baseClassOpt, implements, members, bodySpan);

            AddAndReturn(ref _types, tref.TypeDeclaration);

            return tref;
        }

        public override LangElement Lambda(Span span, Span headingSpan, bool isStatic, bool aliasReturn, TypeRef returnType, IEnumerable<FormalParam> formalParams, Span formalParamsSpan, IEnumerable<FormalParam> lexicalVars, LangElement body)
        {
            return AddAndReturn(ref _lambdas,
                (LambdaFunctionExpr)base.Lambda(span, headingSpan, isStatic, aliasReturn, returnType, formalParams, formalParamsSpan, lexicalVars, body));
        }

        public override LangElement Yield(Span span, LangElement keyOpt, LangElement valueOpt)
        {
            return AddAndReturn(ref _yieldNodes, base.Yield(span, keyOpt, valueOpt));
        }

        public override LangElement YieldFrom(Span span, LangElement fromExpr)
        {
            return AddAndReturn(ref _yieldNodes, base.YieldFrom(span, fromExpr));
        }

        public override LangElement ParenthesisExpression(Span span, LangElement expression)
        {
            // ignore parenthesis
            return expression;
        }

        public NodesFactory(SourceUnit sourceUnit) : base(sourceUnit)
        {
        }
    }
}
