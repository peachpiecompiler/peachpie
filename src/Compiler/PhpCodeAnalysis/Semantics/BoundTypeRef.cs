using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Bound <see cref="TypeRef"/>.
    /// </summary>
    [DebuggerDisplay("{DebugView,nq}")]
    public partial class BoundTypeRef
    {
        public TypeRef TypeRef => _typeRef;
        readonly TypeRef _typeRef;

        string DebugView
        {
            get
            {
                if (_typeRef is DirectTypeRef)
                    return ((DirectTypeRef)_typeRef).ClassName.ToString();

                if (TypeExpression != null)
                    return TypeExpression.ToString();

                return base.ToString();
            }
        }

        /// <summary>
        /// Resolved <see cref="this.TypeRef"/> if possible.
        /// </summary>
        internal TypeSymbol ResolvedType { get; set; }

        /// <summary>
        /// Expression getting type name.
        /// </summary>
        internal BoundExpression TypeExpression { get; set; }

        public BoundTypeRef(TypeRef tref)
        {
            _typeRef = tref;
        }
    }
}
