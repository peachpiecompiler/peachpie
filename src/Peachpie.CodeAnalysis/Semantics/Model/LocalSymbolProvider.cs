using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Specialized <see cref="ISymbolProvider"/> for in a local context.
    /// Allows for resolution of generic parameters and better resolution of symbols in current scope.
    /// </summary>
    internal sealed class LocalSymbolProvider : ISymbolProvider
    {
        readonly ISymbolProvider/*!*/_model;
        readonly FlowContext/*!*/_ctx;

        /// <summary>
        /// Current type scope.
        /// Can be a <c>null</c> reference.
        /// </summary>
        NamedTypeSymbol ContainingType
        {
            get
            {
                return _ctx.TypeRefContext.SelfType;
            }
        }

        /// <summary>
        /// Reference to current file symbol.
        /// Cannot be <c>null</c>.
        /// </summary>
        SourceFileSymbol ContainingFile => _ctx.ContainingFile;

        /// <summary>
        /// Type symbol representing current <c>self</c>. Can be <c>null</c> or a generic parameter.
        /// </summary>
        TypeSymbol SelfType
        {
            get
            {
                var t = (TypeSymbol)ContainingType;
                if (t != null)
                {
                    if (t is SourceTraitTypeSymbol trait)
                    {
                        t = trait.TSelfParameter;
                    }
                }
                else
                {
                    // Lambda
                    // Global code
                    // ...
                }

                return t;
            }
        }

        public LocalSymbolProvider(ISymbolProvider/*!*/model, FlowContext/*!*/ctx)
        {
            Debug.Assert(model != null);
            Debug.Assert(ctx != null);

            _model = model;
            _ctx = ctx;
        }

        public SourceFileSymbol ResolveFile(string path) => _model.ResolveFile(path);

        public INamedTypeSymbol ResolveType(QualifiedName name)
        {
            // TODO: generic arguments:

            // TODO: self, parent:

            NamedTypeSymbol type;

            // type symbols:
            if (ContainingType is IPhpTypeSymbol phpt && phpt.FullName == name)
            {
                // name == scope
                type = ContainingType;
            }
            else
            {
                type = (NamedTypeSymbol)_model.ResolveType(name);
            }

            //
            if (type is AmbiguousErrorTypeSymbol ambiguous)
            {
                // choose the one declared in this file unconditionally
                // TODO: resolution scope, includes, ...
                var best = ambiguous.CandidateSymbols.FirstOrDefault(x => x is SourceTypeSymbol srct && !srct.Syntax.IsConditional && srct.ContainingFile == ContainingFile);
                if (best != null)
                {
                    type = (NamedTypeSymbol)best;
                }
            }

            // construct trait (implicit type argument TSelf)
            if (type.IsTraitType())
            {
                type = type.Construct(SelfType ?? ContainingFile.DeclaringCompilation.CoreTypes.Object);
            }

            //
            return type;
        }

        public IPhpValue ResolveConstant(string name) => _model.ResolveConstant(name);

        public IPhpRoutineSymbol ResolveFunction(QualifiedName name) => _model.ResolveFunction(name);
    }
}
