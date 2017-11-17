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
            else if (this.ResolvedType != null)
            {
                // type was resolved by analysis
                cg.Builder.EmitStringConstant(((IPhpTypeSymbol)this.ResolvedType).FullName.ToString());
            }
            else if (_typeRef is ReservedTypeRef)
            {
                // a reserved type, handle separately
                switch (((ReservedTypeRef)_typeRef).Type)
                {
                    case ReservedTypeRef.ReservedType.@static:
                        EmitLoadStaticPhpTypeInfo(cg);
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetName_PhpTypeInfo.Getter)
                            .Expect(SpecialType.System_String);
                        return;

                    default:
                        throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(((ReservedTypeRef)_typeRef).Type);
                }
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

            TypeSymbol t;

            if (!this.ResolvedType.IsErrorTypeOrNull())
            {
                t = EmitLoadPhpTypeInfo(cg, this.ResolvedType);
            }
            else if (_typeRef is ReservedTypeRef) // late static bound
            {
                switch (((ReservedTypeRef)_typeRef).Type)
                {
                    case ReservedTypeRef.ReservedType.@static:
                        t = EmitLoadStaticPhpTypeInfo(cg);
                        break;

                    case ReservedTypeRef.ReservedType.self:
                        t = EmitLoadSelf(cg);
                        break;

                    case ReservedTypeRef.ReservedType.parent:
                        t = EmitLoadParent(cg);
                        break;

                    default:
                        throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
                }
            }
            else
            {
                if (_objectTypeInfoSemantic && this.TypeExpression != null) // type of object instance handled // only makes sense if type is indirect
                {
                    Debug.Assert(throwOnError); // expecting we throw on error
                    cg.EmitLoadContext();
                    cg.EmitConvertToPhpValue(this.TypeExpression);
                    t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.TypeNameOrObjectToType_Context_PhpValue);
                }
                else
                {
                    // CALL <ctx>.GetDeclaredType(<typename>, autoload:true)
                    cg.EmitLoadContext();
                    this.EmitClassName(cg);
                    cg.Builder.EmitBoolConstant(true);
                    t = cg.EmitCall(ILOpCode.Call, throwOnError
                        ? cg.CoreMethods.Context.GetDeclaredTypeOrThrow_string_bool
                        : cg.CoreMethods.Context.GetDeclaredType_string_bool);
                }
            }

            return t;
        }

        ITypeSymbol IBoundTypeRef.EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError)
            => EmitLoadTypeInfo(cg, throwOnError);

        /// <summary>
        /// Emits <c>PhpTypeInfo</c> of late static bound type.
        /// </summary>
        static TypeSymbol  EmitLoadStaticPhpTypeInfo(CodeGenerator cg)
        {
            if (cg.ThisPlaceOpt != null)
            {
                // Template: GetPhpTypeInfo(this)
                cg.EmitThis();
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.GetPhpTypeInfo_Object);
            }
            else if (cg.Routine != null)
            {
                // Template: LOAD @static   // ~ @static parameter passed by caller
                return new ParamPlace(cg.Routine.ImplicitParameters.First(SpecialParameterSymbol.IsLateStaticParameter))
                    .EmitLoad(cg.Builder);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        static TypeSymbol EmitLoadSelf(CodeGenerator cg)
        {
            cg.EmitCallerRuntimeTypeHandle();
            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetSelf_RuntimeTypeHandle);
        }

        static TypeSymbol EmitLoadParent(CodeGenerator cg)
        {
            cg.EmitCallerRuntimeTypeHandle();
            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetParent_RuntimeTypeHandle);
        }

        internal static TypeSymbol EmitLoadPhpTypeInfo(CodeGenerator cg, ITypeSymbol t)
        {
            Contract.ThrowIfNull(t);

            // CALL GetPhpTypeInfo<T>()
            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.GetPhpTypeInfo_T.Symbol.Construct(t));
        }

        public virtual void Accept(PhpOperationVisitor visitor) => visitor.VisitTypeRef(this);
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

        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitMultipleTypeRef(this);
    }
}
