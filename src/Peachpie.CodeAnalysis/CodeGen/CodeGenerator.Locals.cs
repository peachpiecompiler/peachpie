using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.CodeGen
{
    partial class CodeGenerator
    {
        #region GetTemporaryLocal

        /// <summary>
        /// Returns a <see cref="LocalDefinition"/> of a temporary local variable of a specified <see cref="TypeSymbol"/>.
        /// </summary>
        /// <param name="type">The requested <see cref="TypeSymbol"/> of the local.</param>
        /// <param name="immediateReturn"><c>True</c> to immediately return the local builder to the pool of locals
        /// available for reuse (no need to call <see cref="ReturnTemporaryLocal"/>).</param>
        /// <returns>The <see cref="LocalDefinition"/>.</returns>
        /// <remarks>
        /// If a <see cref="LocalDefinition"/> of the given <see cref="TypeSymbol"/> has already been declared and returned
        /// to the pool, this local is reused. Otherwise, a new local is declared. Use this method to obtain a
        /// short-lived temporary local. If <paramref name="immediateReturn"/> is <c>false</c>, return the local
        /// to the pool of locals available for reuse by calling <see cref="ReturnTemporaryLocal"/>.
        /// </remarks>
        public LocalDefinition/*!*/ GetTemporaryLocal(TypeSymbol/*!*/ type, bool immediateReturn)
        {
            var definition = _il.LocalSlotManager.AllocateSlot((Microsoft.Cci.ITypeReference)type, LocalSlotConstraints.None);

            if (immediateReturn)
                _il.LocalSlotManager.FreeSlot(definition);

            return definition;
        }

        /// <summary>
        /// Returns a <see cref="LocalDefinition"/> of a temporary local variable of a specified <see cref="TypeSymbol"/>.
        /// </summary>
        /// <param name="type">The requested <see cref="TypeSymbol"/> of the local.</param>
        /// <returns>The <see cref="LocalDefinition"/>.</returns>
        /// <remarks>
        /// If a <see cref="LocalDefinition"/> of the given <see cref="TypeSymbol"/> has already been declared and returned
        /// to the pool, this local is reused. Otherwise, a new local is declared. Use this method to obtain a
        /// short-lived temporary local.
        /// Return the local to the pool of locals available for reuse by calling <see cref="ReturnTemporaryLocal"/>.
        /// </remarks>
        public LocalDefinition/*!*/ GetTemporaryLocal(TypeSymbol/*!*/ type)
        {
            return GetTemporaryLocal(type, false);
        }

        /// <summary>
        /// Returns a <see cref="LocalDefinition"/> previously obtained from <see cref="GetTemporaryLocal(TypeSymbol,bool)"/> to the
        /// pool of locals available for reuse.
        /// </summary>
        /// <param name="definition">The <see cref="LocalDefinition"/> to return to the pool.</param>
        public void ReturnTemporaryLocal(LocalDefinition/*!*/ definition)
        {
            _il.LocalSlotManager.FreeSlot(definition);
        }

        #endregion

        /// <summary>
        /// If possible, gets <see cref="IPlace"/> representing given expression (in case of a field or variable).
        /// </summary>
        /// <param name="expr"></param>
        /// <returns>Place or <c>null</c>.</returns>
        internal IPlace PlaceOrNull(BoundExpression expr)
        {
            if (expr is BoundReferenceExpression)
            {
                return ((BoundReferenceExpression)expr).Place(_il);
            }

            return null;
        }
    }
}
