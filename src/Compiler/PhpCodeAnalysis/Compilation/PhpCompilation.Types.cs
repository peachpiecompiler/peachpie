using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System.Diagnostics;

namespace Pchp.CodeAnalysis
{
    partial class PhpCompilation
    {
        #region CoreTypes, CoreMethods, Merging

        /// <summary>
        /// Well known types associated with this compilation.
        /// </summary>
        public CoreTypes CoreTypes => _coreTypes;
        readonly CoreTypes _coreTypes;

        /// <summary>
        /// Well known methods associated with this compilation.
        /// </summary>
        public CoreMethods CoreMethods => _coreMethods;
        readonly CoreMethods _coreMethods;

        #endregion

        #region PHP Type Hierarchy

        /// <summary>
        /// Merges two CLR types into one, according to PCHP type hierarchy.
        /// </summary>
        /// <param name="first">First type.</param>
        /// <param name="second">Second type.</param>
        /// <returns>One type convering both <paramref name="first"/> and <paramref name="second"/> types.</returns>
        public NamedTypeSymbol Merge(NamedTypeSymbol first, NamedTypeSymbol second)
        {
            Contract.ThrowIfNull(first);
            Contract.ThrowIfNull(second);

            Debug.Assert(first != CoreTypes.PhpAlias && second != CoreTypes.PhpAlias);

            // merge is not needed,
            if (first == second || first == CoreTypes.PhpValue)
                return first;

            if (second == CoreTypes.PhpValue)
                return second;

            // a number (int | double)
            if (IsNumber(first) && IsNumber(second))
                return CoreTypes.PhpNumber;

            //// a string builder
            //if (first.IsString && second.IsString)
            //    return CoreTypes.PhpStringBuilder;

            //

            // most common PHP value type
            return CoreTypes.PhpValue;
        }

        /// <summary>
        /// Determines whether given type is treated as a PHP number (<c>int</c> or <c>double</c>).
        /// </summary>
        public bool IsNumber(TypeSymbol type)
        {
            Contract.ThrowIfNull(type);

            return
                type.SpecialType == SpecialType.System_Double ||
                type.SpecialType == SpecialType.System_Int32 ||
                type.SpecialType == SpecialType.System_Int64 ||
                type == CoreTypes.PhpNumber;
        }

        #endregion
        
        internal NamedTypeSymbol GetWellKnownType(WellKnownType id)
        {
            var name = id.GetMetadataName();
            if (name != null && this.CorLibrary != null)
            {
                return this.CorLibrary.GetTypeByMetadataName(name);
            }

            return null;
        }

        protected override INamedTypeSymbol CommonGetSpecialType(SpecialType specialType)
        {
            return this.CorLibrary.GetSpecialType(specialType);
        }

        IEnumerable<IAssemblySymbol> ProbingAssemblies
        {
            get
            {
                foreach (var pair in CommonGetBoundReferenceManager().GetReferencedAssemblies())
                    yield return pair.Value;

                yield return this.SourceAssembly;
            }
        }

        protected override INamedTypeSymbol CommonGetTypeByMetadataName(string metadataName)
        {
            return ProbingAssemblies
                    .Select(a => a.GetTypeByMetadataName(metadataName))
                    .Where(a => a != null)
                    .FirstOrDefault();
        }

        /// <summary>
        /// Resolves <see cref="TypeSymbol"/> best fitting given type mask.
        /// </summary>
        internal NamedTypeSymbol GetTypeFromTypeRef(TypeRefContext typeCtx, TypeRefMask typeMask)
        {
            if (!typeMask.IsAnyType)
            {
                if (typeMask.IsRef)
                {
                    return CoreTypes.PhpAlias;
                }

                if (typeMask.IsVoid)
                {
                    return CoreTypes.Void;
                }

                var types = typeCtx.GetTypes(typeMask);
                Debug.Assert(types.Count != 0);

                // determine best fitting CLR type based on defined PHP types hierarchy
                var result = GetTypeFromTypeRef(types[0]);

                for (int i = 1; i < types.Count; i++)
                {
                    var tdesc = GetTypeFromTypeRef(types[i]);
                    result = Merge(result, GetTypeFromTypeRef(types[i]));
                }

                //
                return result;
            }

            // most common type
            return CoreTypes.PhpValue;
        }

        internal NamedTypeSymbol GetTypeFromTypeRef(ITypeRef t)
        {
            if (t is PrimitiveTypeRef)
            {
                return GetTypeFromTypeRef((PrimitiveTypeRef)t);
            }
            else if (t is ClassTypeRef)
            {
                return CoreTypes.Object;
            }
            else if (t is ArrayTypeRef)
            {

            }
            else if (t is LambdaTypeRef)
            {

            }

            throw new ArgumentException();
        }

        NamedTypeSymbol GetTypeFromTypeRef(PrimitiveTypeRef t)
        {
            switch (t.TypeCode)
            {
                case PhpDataType.Double: return CoreTypes.Double;
                case PhpDataType.Long: return CoreTypes.Long;
                case PhpDataType.Boolean: return CoreTypes.Boolean;
                case PhpDataType.String: return CoreTypes.String;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Resolves <see cref="INamedTypeSymbol"/> best fitting given type mask.
        /// </summary>
        internal NamedTypeSymbol GetTypeFromTypeRef(SourceRoutineSymbol routine, TypeRefMask typeMask)
        {
            if (routine.ControlFlowGraph.HasFlowState)
            {
                var ctx = routine.ControlFlowGraph.FlowContext;
                return this.GetTypeFromTypeRef(ctx.TypeRefContext, typeMask);
            }

            throw new InvalidOperationException();
        }
    }
}
