using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.TypeRef
{
    [DebuggerDisplay("BoundPrimitiveTypeRef ({_type})")]
    sealed class BoundPrimitiveTypeRef : IBoundTypeRef
    {
        readonly PhpTypeCode _type;

        public BoundPrimitiveTypeRef(PhpTypeCode type)
        {
            _type = type;
        }

        public bool IsNullable { get; set; }

        public bool IsObject => _type == PhpTypeCode.Object;

        public bool IsArray => _type == PhpTypeCode.PhpArray;

        public bool IsPrimitiveType => true;

        public bool IsLambda => false;

        public TypeRefMask LambdaReturnType => TypeRefMask.AnyType;

        public TypeRefMask ElementType => TypeRefMask.AnyType;

        public ImmutableArray<IBoundTypeRef> TypeArguments => ImmutableArray<IBoundTypeRef>.Empty;

        public ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            // primitive type does not have (should not have) PhpTypeInfo
            throw new NotSupportedException();
        }

        public ITypeSymbol ResolveTypeSymbol(SourceRoutineSymbol routine, SourceTypeSymbol self)
        {
            var ct = routine.DeclaringCompilation.CoreTypes;

            switch (_type)
            {
                case PhpTypeCode.Void: return ct.Void.Symbol;
                case PhpTypeCode.Boolean: return ct.Boolean.Symbol;
                case PhpTypeCode.Long: return ct.Long.Symbol;
                case PhpTypeCode.Double: return ct.Double.Symbol;
                case PhpTypeCode.String: return ct.String.Symbol;
                case PhpTypeCode.WritableString: return ct.PhpString.Symbol;
                case PhpTypeCode.PhpArray: return ct.PhpArray.Symbol;
                case PhpTypeCode.Resource: return ct.PhpResource.Symbol;
                case PhpTypeCode.Object: return ct.Object.Symbol;
                case PhpTypeCode.Null: return ct.Object.Symbol;
                default:
                    throw ExceptionUtilities.UnexpectedValue(_type);
            }
        }

        public IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target) => this;

        public bool Equals(IBoundTypeRef other) => ReferenceEquals(this, other) || (other is BoundPrimitiveTypeRef pt && pt._type == this._type);
    }

    [DebuggerDisplay("BoundArrayTypeRef ({_elementType})")]
    sealed class BoundArrayTypeRef : IBoundTypeRef
    {
        readonly TypeRefMask _elementType;

        public BoundArrayTypeRef(TypeRefMask elementType)
        {
            _elementType = elementType;
        }

        public bool IsNullable { get; set; }

        public bool IsObject => false;

        public bool IsArray => true;

        public bool IsPrimitiveType => true;

        public bool IsLambda => false;

        public TypeRefMask LambdaReturnType => TypeRefMask.AnyType;

        public TypeRefMask ElementType => _elementType;

        public ImmutableArray<IBoundTypeRef> TypeArguments => ImmutableArray<IBoundTypeRef>.Empty;

        public ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            // primitive type does not have (should not have) PhpTypeInfo
            throw new NotSupportedException();
        }

        public ITypeSymbol ResolveTypeSymbol(SourceRoutineSymbol routine, SourceTypeSymbol self)
        {
            return routine.DeclaringCompilation.CoreTypes.PhpArray.Symbol;
        }

        public IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target)
        {
            Contract.ThrowIfNull(source);
            Contract.ThrowIfNull(target);

            if (source == target || _elementType.IsVoid || _elementType.IsAnyType)
                return this;

            // note: there should be no circular dependency
            return new BoundArrayTypeRef(target.AddToContext(source, _elementType));
        }

        public bool Equals(IBoundTypeRef other) => ReferenceEquals(this, other) || (other is BoundArrayTypeRef at && at.ElementType == this.ElementType);
    }

    [DebuggerDisplay("BoundLambdaTypeRef ({_returnType})")]
    sealed class BoundLambdaTypeRef : IBoundTypeRef
    {
        readonly TypeRefMask _returnType;

        // TODO: signature

        public BoundLambdaTypeRef(TypeRefMask returnType)
        {
            _returnType = returnType;
        }

        public bool IsNullable { get; set; }

        public bool IsObject => true; // Closure

        public bool IsArray => false;

        public bool IsPrimitiveType => false;

        public bool IsLambda => true;

        public TypeRefMask LambdaReturnType => _returnType;

        public TypeRefMask ElementType => TypeRefMask.AnyType;

        public ImmutableArray<IBoundTypeRef> TypeArguments => ImmutableArray<IBoundTypeRef>.Empty;

        public ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            // primitive type does not have (should not have) PhpTypeInfo
            throw new NotSupportedException();
        }

        public ITypeSymbol ResolveTypeSymbol(SourceRoutineSymbol routine, SourceTypeSymbol self)
        {
            return routine.DeclaringCompilation.CoreTypes.Closure.Symbol;
        }

        public IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target)
        {
            if (source == target || _returnType.IsVoid || _returnType.IsAnyType)
                return this;

            // note: there should be no circular dependency
            return new BoundLambdaTypeRef(target.AddToContext(source, _returnType)/*, _signature*/);
        }

        public bool Equals(IBoundTypeRef other) => ReferenceEquals(this, other) || (other is BoundLambdaTypeRef lt && lt._returnType == this._returnType);
    }

    [DebuggerDisplay("BoundClassTypeRef ({_qname})")]
    class BoundClassTypeRef : IBoundTypeRef
    {
        protected readonly QualifiedName _qname;

        public BoundClassTypeRef(QualifiedName qname)
        {
            _qname = qname;
        }

        public bool IsNullable { get; set; }

        public bool IsObject => true;

        public bool IsArray => false;

        public bool IsPrimitiveType => false;

        public bool IsLambda => false;

        public TypeRefMask LambdaReturnType => TypeRefMask.AnyType;

        public TypeRefMask ElementType => TypeRefMask.AnyType;

        public virtual ImmutableArray<IBoundTypeRef> TypeArguments => ImmutableArray<IBoundTypeRef>.Empty;

        public virtual ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            var t = ResolveTypeSymbol(cg.Routine, cg.CallerType as SourceTypeSymbol);

            // see BoundTypeRef.EmitLoadTypeInfo

            throw new NotImplementedException();
        }

        public virtual ITypeSymbol ResolveTypeSymbol(SourceRoutineSymbol routine, SourceTypeSymbol self)
        {
            if (self != null)
            {
                if (self.FullName == _qname) return self;
                if (self.BaseType is IPhpTypeSymbol phpt && phpt.FullName == _qname) return self.BaseType;
            }

            var compilation = routine.DeclaringCompilation;

            var t = (NamedTypeSymbol)compilation.GlobalSemantics.ResolveType(_qname);
            return t.IsErrorTypeOrNull() ? compilation.CoreTypes.Object.Symbol : t;
        }

        public virtual IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target) => this;

        public virtual bool Equals(IBoundTypeRef other) => ReferenceEquals(this, other) || (other is BoundClassTypeRef ct && ct._qname == this._qname && ct.TypeArguments.IsDefaultOrEmpty);
    }

    [DebuggerDisplay("BoundGenericClassTypeRef ({_qname}`{_typeArguments.Length})")]
    sealed class BoundGenericClassTypeRef : BoundClassTypeRef
    {
        readonly ImmutableArray<IBoundTypeRef> _typeArguments;

        public BoundGenericClassTypeRef(QualifiedName qname, ImmutableArray<IBoundTypeRef> typeArguments)
            : base(qname)
        {
            _typeArguments = typeArguments;
        }

        public override ImmutableArray<IBoundTypeRef> TypeArguments => _typeArguments;

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            var t = ResolveTypeSymbol(cg.Routine, cg.CallerType as SourceTypeSymbol);

            // see BoundTypeRef.EmitLoadTypeInfo

            throw new NotImplementedException();
        }

        public override ITypeSymbol ResolveTypeSymbol(SourceRoutineSymbol routine, SourceTypeSymbol self)
        {
            var resolved = (NamedTypeSymbol)base.ResolveTypeSymbol(routine, self);

            if (resolved.IsValidType())
            {
                // TODO: check _typeArguments are bound (no ErrorSymbol)

                var boundTypeArgs = _typeArguments.SelectAsArray(tref => (TypeSymbol)tref.ResolveTypeSymbol(routine, self));

                return resolved.Construct(boundTypeArgs);
            }
            else
            {
                return routine.DeclaringCompilation.CoreTypes.Object.Symbol;
            }
        }

        public override IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target) => this;

        public override bool Equals(IBoundTypeRef other)
        {
            if (ReferenceEquals(this, other)) return true;

            if (other is BoundGenericClassTypeRef gt && gt._qname == _qname && other.TypeArguments.Length == this.TypeArguments.Length)
            {
                for (int i = 0; i < TypeArguments.Length; i++)
                {
                    if (!other.TypeArguments[i].Equals(this.TypeArguments[i])) return false;
                }

                return true;
            }

            return false;
        }
    }

    sealed class BoundIndirectTypeRef : IBoundTypeRef
    {
        readonly BoundExpression _typeExpression;

        public BoundIndirectTypeRef(BoundExpression typeExpression, bool objectTypeInfoSemantic)
        {
            _typeExpression = typeExpression ?? throw ExceptionUtilities.ArgumentNull();
            _objectTypeInfoSemantic = objectTypeInfoSemantic;
        }

        /// <summary>
        /// Gets value determining the indirect type reference can refer to an object instance which type is used to get the type info.
        /// </summary>
        public bool ObjectTypeInfoSemantic => _objectTypeInfoSemantic;
        readonly bool _objectTypeInfoSemantic;

        public bool IsNullable { get; set; }

        public bool IsObject => true;

        public bool IsArray => false;

        public bool IsPrimitiveType => false;

        public bool IsLambda => false;

        public TypeRefMask LambdaReturnType => TypeRefMask.AnyType;

        public TypeRefMask ElementType => TypeRefMask.AnyType;

        public ImmutableArray<IBoundTypeRef> TypeArguments => ImmutableArray<IBoundTypeRef>.Empty;

        public void EmitClassName(CodeGenerator cg)
        {
            cg.EmitConvert(_typeExpression, cg.CoreTypes.String);
        }

        public ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            if (ObjectTypeInfoSemantic) // type of object instance handled // only makes sense if type is indirect
            {
                // TODO: throwOnError
                cg.EmitLoadContext();
                cg.EmitConvertToPhpValue(_typeExpression);
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.TypeNameOrObjectToType_Context_PhpValue);
            }
            else
            {
                // CALL <ctx>.GetDeclaredType(<typename>, autoload: true)
                cg.EmitLoadContext();
                this.EmitClassName(cg);
                cg.Builder.EmitBoolConstant(true);
                return cg.EmitCall(ILOpCode.Call, throwOnError
                    ? cg.CoreMethods.Context.GetDeclaredTypeOrThrow_string_bool
                    : cg.CoreMethods.Context.GetDeclaredType_string_bool);
            }
        }

        public bool Equals(IBoundTypeRef other) => ReferenceEquals(this, other) || (other is BoundIndirectTypeRef it && it._typeExpression == _typeExpression);

        public ITypeSymbol ResolveTypeSymbol(SourceRoutineSymbol routine, SourceTypeSymbol self)
        {
            // System.Object
            return routine.DeclaringCompilation.CoreTypes.Object.Symbol;
        }

        public IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target)
        {
            if (source == target) return this;

            // it is "an" object within another routine:
            return new BoundPrimitiveTypeRef(PhpTypeCode.Object) { IsNullable = false };
        }
    }
}
