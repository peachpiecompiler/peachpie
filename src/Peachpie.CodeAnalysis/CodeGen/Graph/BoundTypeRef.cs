using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Semantics
{
    partial interface IBoundTypeRef
    {
        /// <summary>
        /// Emits load of <c>PhpTypeInfo</c>.
        /// </summary>
        ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false);
    }

    partial class BoundTypeRef : IBoundTypeRef
    {
        /// <summary>
        /// Emits name of bound type.
        /// </summary>
        internal virtual void EmitClassName(CodeGenerator cg)
        {
            if (_typeRef is PrimitiveTypeRef)
            {
                // cannot emit the class name because type is not a class
                throw new InvalidOperationException();  // not a class
            }
            else if (_typeRef is TranslatedTypeRef || _typeRef is ClassTypeRef)
            {
                // type name is known in parse time
                var classname = ((INamedTypeRef)_typeRef).ClassName;
                cg.Builder.EmitStringConstant(classname.ToString());
            }
            else if (this.ResolvedType.IsValidType())
            {
                // type was resolved by analysis
                cg.Builder.EmitStringConstant(((IPhpTypeSymbol)this.ResolvedType).FullName.ToString());
            }
            else if (_typeRef is ReservedTypeRef)
            {
                // Template: {LOAD PhpTypeInfo}.Name
                EmitLoadTypeInfo(cg, true);
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetName_PhpTypeInfo.Getter)
                    .Expect(SpecialType.System_String);
            }
            else if (TypeExpression != null)
            {
                // indirect type, evaluate
                cg.EmitConvert(TypeExpression, cg.CoreTypes.String);
            }
            else
            {
                // unhandled
                throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(_typeRef);
            }
        }

        /// <summary>
        /// Emits load of <c>PhpTypeInfo</c>.
        /// </summary>
        /// <param name="cg">Code generator instance.</param>
        /// <param name="throwOnError">Emits PHP error in case type is not declared.</param>
        /// <remarks>Emits <c>NULL</c> in case type is not declared.</remarks>
        internal virtual TypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            Debug.Assert(cg != null);

            if (this.ResolvedType.IsValidType())
            {
                return EmitLoadPhpTypeInfo(cg, this.ResolvedType);
            }
            else if (_typeRef is ReservedTypeRef) // late static bound
            {
                switch (((ReservedTypeRef)_typeRef).Type)
                {
                    case ReservedTypeRef.ReservedType.@static:
                        return EmitLoadStaticPhpTypeInfo(cg);

                    case ReservedTypeRef.ReservedType.self:
                        return EmitLoadSelf(cg, throwOnError: true);

                    case ReservedTypeRef.ReservedType.parent:
                        return EmitLoadParent(cg);

                    default:
                        throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
                }
            }
            else
            {
                if (_objectTypeInfoSemantic && this.TypeExpression != null) // type of object instance handled // only makes sense if type is indirect
                {
                    // TODO: throwOnError
                    cg.EmitLoadContext();
                    cg.EmitConvertToPhpValue(this.TypeExpression);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.TypeNameOrObjectToType_Context_PhpValue);
                }
                else
                {
                    // CALL <ctx>.GetDeclaredType(<typename>, autoload:true)
                    cg.EmitLoadContext();
                    this.EmitClassName(cg);
                    cg.Builder.EmitBoolConstant(true);
                    return cg.EmitCall(ILOpCode.Call, throwOnError
                        ? cg.CoreMethods.Context.GetDeclaredTypeOrThrow_string_bool
                        : cg.CoreMethods.Context.GetDeclaredType_string_bool);
                }
            }
        }

        ITypeSymbol IBoundTypeRef.EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError)
            => EmitLoadTypeInfo(cg, throwOnError);

        /// <summary>
        /// Emits <c>PhpTypeInfo</c> of late static bound type.
        /// </summary>
        internal static TypeSymbol EmitLoadStaticPhpTypeInfo(CodeGenerator cg)
        {
            if (cg.Routine != null)
            {
                if (cg.Routine is SourceLambdaSymbol lambda)
                {
                    // Handle lambda since $this can be null (unbound)
                    // Template: CLOSURE.Static();
                    lambda.ClosureParameter.EmitLoad(cg.Builder);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Static_Closure)
                        .Expect(cg.CoreTypes.PhpTypeInfo);
                }

                var thisVariablePlace = cg.Routine.GetPhpThisVariablePlace(cg.Module);
                if (thisVariablePlace != null)
                {
                    // Template: GetPhpTypeInfo(this)
                    thisVariablePlace.EmitLoad(cg.Builder);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.GetPhpTypeInfo_Object);
                }

                var lateStaticParameter = cg.Routine.ImplicitParameters.FirstOrDefault(SpecialParameterSymbol.IsLateStaticParameter);
                if (lateStaticParameter != null)
                {
                    // Template: LOAD @static   // ~ @static parameter passed by caller
                    return lateStaticParameter.EmitLoad(cg.Builder);
                }

                var caller = cg.CallerType;
                if (caller is SourceTypeSymbol srct && srct.IsSealed)
                {
                    // `static` == `self` <=> self is sealed
                    // Template: GetPhpTypeInfo<CallerType>()
                    return EmitLoadPhpTypeInfo(cg, caller);
                }
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Loads <c>PhpTypeInfo</c> of <c>self</c>.
        /// </summary>
        /// <param name="cg"></param>
        /// <param name="throwOnError">Whether to expect only valid scope.</param>
        /// <returns>Type symbol of PhpTypeInfo.</returns>
        internal static TypeSymbol EmitLoadSelf(CodeGenerator cg, bool throwOnError = false)
        {
            var caller = cg.CallerType;
            if (caller != null)
            {
                // current scope is resolved in compile-time:
                // Template: GetPhpTypeInfo<CallerType>()
                return EmitLoadPhpTypeInfo(cg, caller);
            }
            else
            {
                // Template: Operators.GetSelf( {caller type handle} )
                cg.EmitCallerTypeHandle();
                return cg.EmitCall(ILOpCode.Call, throwOnError
                    ? cg.CoreMethods.Operators.GetSelf_RuntimeTypeHandle
                    : cg.CoreMethods.Operators.GetSelfOrNull_RuntimeTypeHandle);
            }
        }

        static TypeSymbol EmitLoadParent(CodeGenerator cg)
        {
            var caller = cg.CallerType;
            if (caller != null)
            {
                // current scope is resolved in compile-time:
                // Template: Operators.GetParent( GetPhpTypeInfo<CallerType>() )
                EmitLoadPhpTypeInfo(cg, caller);
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetParent_PhpTypeInfo);
            }
            else
            {
                // Template: Operators.GetParent( {caller type handle} )
                cg.EmitCallerTypeHandle();
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetParent_RuntimeTypeHandle);
            }
        }

        internal static TypeSymbol EmitLoadPhpTypeInfo(CodeGenerator cg, ITypeSymbol t)
        {
            Contract.ThrowIfNull(t);

            // CALL GetPhpTypeInfo<T>()
            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.GetPhpTypeInfo_T.Symbol.Construct(t));
        }

        public virtual TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitTypeRef(this);
    }

    partial class BoundMultipleTypeRef
    {
        internal override void EmitClassName(CodeGenerator cg)
        {
            throw new NotSupportedException();
        }

        internal override TypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            throw new NotSupportedException();
        }

        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitMultipleTypeRef(this);
    }
}
