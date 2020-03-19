using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
using static Devsense.PHP.Syntax.Ast.ReservedTypeRef;

namespace Pchp.CodeAnalysis.Semantics.TypeRef
{
    #region BoundPrimitiveTypeRef

    [DebuggerDisplay("BoundPrimitiveTypeRef ({_type})")]
    sealed class BoundPrimitiveTypeRef : BoundTypeRef
    {
        public PhpTypeCode TypeCode => _type;
        readonly PhpTypeCode _type;

        public BoundPrimitiveTypeRef(PhpTypeCode type)
        {
            _type = type;

            //
            IsNullable = type == PhpTypeCode.Null;
        }

        /// <summary>
        /// Gets value indicating the type is <c>long</c> or <c>double</c>.
        /// </summary>
        public bool IsNumber => _type == PhpTypeCode.Long || _type == PhpTypeCode.Double;

        public override bool IsObject => _type == PhpTypeCode.Object;

        public override bool IsArray => _type == PhpTypeCode.PhpArray;

        public override bool IsPrimitiveType => _type != PhpTypeCode.Object;

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            // primitive type does not have (should not have) PhpTypeInfo
            throw new NotSupportedException();
        }

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation)
        {
            var ct = compilation.CoreTypes;

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
                case PhpTypeCode.Iterable: return ct.PhpValue.Symbol; // array | Traversable
                case PhpTypeCode.Callable: return ct.PhpValue.Symbol; // array | string | object
                default:
                    throw ExceptionUtilities.UnexpectedValue(_type);
            }
        }

        public override TypeRefMask GetTypeRefMask(TypeRefContext ctx)
        {
            TypeRefMask result;

            switch (_type)
            {
                case PhpTypeCode.Void: result = 0; break;
                case PhpTypeCode.Boolean: result = ctx.GetBooleanTypeMask(); break;
                case PhpTypeCode.Long: result = ctx.GetLongTypeMask(); break;
                case PhpTypeCode.Double: result = ctx.GetDoubleTypeMask(); break;
                case PhpTypeCode.String: result = ctx.GetStringTypeMask(); break;
                case PhpTypeCode.WritableString: result = ctx.GetWritableStringTypeMask(); break;
                case PhpTypeCode.PhpArray: result = ctx.GetArrayTypeMask(); break;
                case PhpTypeCode.Resource: result = ctx.GetResourceTypeMask(); break;
                case PhpTypeCode.Object: result = ctx.GetSystemObjectTypeMask(); break;
                case PhpTypeCode.Null: return ctx.GetNullTypeMask();
                case PhpTypeCode.Iterable: result = ctx.GetArrayTypeMask() | ctx.GetTypeMask(ctx.BoundTypeRefFactory.TraversableTypeRef, true); break;   // array | Traversable
                case PhpTypeCode.Callable: result = ctx.GetArrayTypeMask() | ctx.GetStringTypeMask() | ctx.GetSystemObjectTypeMask(); break;// array | string | object
                default:
                    throw ExceptionUtilities.UnexpectedValue(_type);
            }

            if (IsNullable)
            {
                result |= ctx.GetNullTypeMask();
            }

            return result;
        }

        public override string ToString()
        {
            switch (_type)
            {
                case PhpTypeCode.Void: return "void"; // report "void" instead of "undefined"
                case PhpTypeCode.Long: return "integer";
                case PhpTypeCode.String:
                case PhpTypeCode.WritableString: return "string";
                case PhpTypeCode.PhpArray: return "array";
                default:
                    return _type.ToString().ToLowerInvariant();
            }
        }

        public override bool Equals(IBoundTypeRef other) => base.Equals(other) || (other is BoundPrimitiveTypeRef pt && pt._type == this._type);
    }

    #endregion

    #region BoundReservedTypeRef

    sealed class BoundReservedTypeRef : BoundTypeRef
    {
        public ReservedType ReservedType => _type;
        readonly ReservedType _type;

        readonly SourceTypeSymbol _self;

        public BoundReservedTypeRef(ReservedType type, SourceTypeSymbol self = null)
        {
            _type = type;
            _self = self;
        }

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            switch (_type)
            {
                case ReservedType.@static:
                    return cg.EmitLoadStaticPhpTypeInfo();

                case ReservedType.self:
                    return cg.EmitLoadSelf(throwOnError: true);

                case ReservedType.parent:
                    return cg.EmitLoadParent();

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public override string ToString() => _type.ToString().ToLowerInvariant();

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation)
        {
            if (this.ResolvedType != null)
            {
                return this.ResolvedType;
            }

            if (_self == null || _self.IsTrait)
            {
                // no self, parent, static resolvable in compile-time:
                return new MissingMetadataTypeSymbol(ToString(), 0, false);
            }

            // resolve types that parser skipped
            switch (_type)
            {
                case ReservedType.self:
                    return _self;

                case ReservedType.parent:
                    var btype = _self.BaseType;
                    return (btype == null || btype.IsObjectType()) // no "System.Object" in PHP, invalid parent
                        ? new MissingMetadataTypeSymbol(ToString(), 0, false)
                        : btype;

                case ReservedType.@static:
                    if (_self.IsSealed)
                    {
                        // `static` == `self` <=> self is sealed
                        return _self;
                    }
                    break;
            }

            // unk
            return null;
        }

        public override TypeRefMask GetTypeRefMask(TypeRefContext ctx)
        {
            switch (_type)
            {
                case ReservedType.@static:
                    return ctx.GetStaticTypeMask();

                case ReservedType.self:
                    return ctx.GetSelfTypeMask();

                case ReservedType.parent:
                    return ctx.GetParentTypeMask();

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public override IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target)
        {
            switch (_type)
            {
                case ReservedType.@static:
                    if (source.ThisType != null)
                        return new BoundTypeRefFromSymbol(source.ThisType);
                    break;

                case ReservedType.self:
                    if (_self != null)
                        return new BoundTypeRefFromSymbol(source.SelfType);
                    break;

                case ReservedType.parent:
                    if (_self?.BaseType != null)
                        return new BoundTypeRefFromSymbol(source.ThisType.BaseType);
                    break;
            }

            // unk
            return target.BoundTypeRefFactory.ObjectTypeRef;
        }
    }

    #endregion

    #region BoundArrayTypeRef

    [DebuggerDisplay("BoundArrayTypeRef ({_elementType})")]
    sealed class BoundArrayTypeRef : BoundTypeRef
    {
        readonly TypeRefMask _elementType;

        public BoundArrayTypeRef(TypeRefMask elementType)
        {
            _elementType = elementType;
        }

        public override bool IsArray => true;

        public override bool IsPrimitiveType => true;

        public override TypeRefMask ElementType => _elementType;

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            // primitive type does not have (should not have) PhpTypeInfo
            throw new NotSupportedException();
        }

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation)
        {
            return compilation.CoreTypes.PhpArray.Symbol;
        }

        public override string ToString() => PhpTypeCode.PhpArray.ToString().ToLowerInvariant();

        public override IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target)
        {
            Contract.ThrowIfNull(source);
            Contract.ThrowIfNull(target);

            if (source == target || _elementType.IsVoid || _elementType.IsAnyType)
                return this;

            // note: there should be no circular dependency
            return new BoundArrayTypeRef(target.AddToContext(source, _elementType));
        }

        public override bool Equals(IBoundTypeRef other) => base.Equals(other) || (other is BoundArrayTypeRef at && at.ElementType == this.ElementType);
    }

    #endregion

    #region BoundLambdaTypeRef

    [DebuggerDisplay("BoundLambdaTypeRef ({_returnType})")]
    sealed class BoundLambdaTypeRef : BoundTypeRef
    {
        readonly TypeRefMask _returnType;

        // TODO: signature

        public BoundLambdaTypeRef(TypeRefMask returnType)
        {
            _returnType = returnType;
        }

        public override bool IsObject => true; // Closure

        public override bool IsLambda => true;

        public override TypeRefMask LambdaReturnType => _returnType;

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            // primitive type does not have (should not have) PhpTypeInfo
            throw ExceptionUtilities.Unreachable;
        }

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation)
        {
            return compilation.CoreTypes.Closure.Symbol;
        }

        public override IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target)
        {
            if (source == target || _returnType.IsVoid || _returnType.IsAnyType)
                return this;

            // note: there should be no circular dependency
            return new BoundLambdaTypeRef(target.AddToContext(source, _returnType)/*, _signature*/);
        }

        public override bool Equals(IBoundTypeRef other) => base.Equals(other) || (other is BoundLambdaTypeRef lt && lt._returnType == this._returnType);

        public override string ToString() => NameUtils.SpecialNames.Closure.ToString();
    }

    #endregion

    #region BoundClassTypeRef

    [DebuggerDisplay("BoundClassTypeRef ({ToString(),nq})")]
    sealed class BoundClassTypeRef : BoundTypeRef
    {
        public QualifiedName ClassName { get; }

        readonly SourceRoutineSymbol _routine;
        readonly SourceTypeSymbol _self;
        readonly int _arity;

        public BoundClassTypeRef(QualifiedName qname, SourceRoutineSymbol routine, SourceTypeSymbol self, int arity = -1)
        {
            if (qname.IsReservedClassName)
            {
                throw new ArgumentException();
            }

            ClassName = qname;
            _routine = routine;
            _self = self;
            _arity = arity;
        }

        public override bool IsObject => true;

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            var t = ResolvedType ?? (TypeSymbol)ResolveTypeSymbol(cg.DeclaringCompilation);
            if (t.IsValidType())
            {
                return cg.EmitLoadPhpTypeInfo(t);
            }
            else
            {
                // CALL <ctx>.GetDeclaredType(<typename>, autoload: true)
                cg.EmitLoadContext();
                cg.Builder.EmitStringConstant(ClassName.ToString());
                cg.Builder.EmitBoolConstant(true);

                return cg.EmitCall(ILOpCode.Call, throwOnError
                    ? cg.CoreMethods.Context.GetDeclaredTypeOrThrow_string_bool
                    : cg.CoreMethods.Context.GetDeclaredType_string_bool);
            }
        }

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation)
        {
            if (ResolvedType.IsValidType() && !ResolvedType.IsUnreachable)
            {
                return ResolvedType;
            }

            TypeSymbol type = null;

            if (_self != null)
            {
                if (_self.FullName == ClassName) type = _self;
                else if (_self.BaseType != null && _self.BaseType.PhpQualifiedName() == ClassName) type = _self.BaseType;
            }

            if (type == null)
            {
                type = (_arity <= 0)
                 ? (TypeSymbol)compilation.GlobalSemantics.ResolveType(ClassName)
                 // generic types only exist in external references, use this method to resolve the symbol including arity (needs metadataname instead of QualifiedName)
                 : compilation.GlobalSemantics.GetTypeFromNonExtensionAssemblies(MetadataHelpers.ComposeAritySuffixedMetadataName(ClassName.ClrName(), _arity));
            }

            var containingFile = _routine?.ContainingFile ?? _self?.ContainingFile;

            if (type is AmbiguousErrorTypeSymbol ambiguous && containingFile != null)
            {
                TypeSymbol best = null;

                // choose the one declared in this file unconditionally
                foreach (var x in ambiguous
                    .CandidateSymbols
                    .Cast<TypeSymbol>()
                    .Where(t => !t.IsUnreachable)
                    .Where(x => x is SourceTypeSymbol srct && !srct.Syntax.IsConditional && srct.ContainingFile == containingFile))
                {
                    if (best == null)
                    {
                        best = x;
                    }
                    else
                    {
                        best = null;
                        break;
                    }
                }

                if (best != null)
                {
                    type = (NamedTypeSymbol)best;
                }
            }

            // translate trait prototype to constructed trait type
            if (type.IsTraitType())
            {
                // <!TSelf> -> <T<Object>>
                var t = (NamedTypeSymbol)type;
                type = t.Construct(t.Construct(compilation.CoreTypes.Object));
            }

            //
            return (ResolvedType = type);
        }

        public override string ToString() => ClassName.ToString();

        public override TypeRefMask GetTypeRefMask(TypeRefContext ctx) => ctx.GetTypeMask(this, true);

        public override bool Equals(IBoundTypeRef other) => base.Equals(other) || (other is BoundClassTypeRef ct && ct.ClassName == this.ClassName && ct.TypeArguments.IsDefaultOrEmpty);
    }

    #endregion

    #region BoundGenericClassTypeRef

    [DebuggerDisplay("BoundGenericClassTypeRef ({_targetType,nq}`{_typeArguments.Length})")]
    sealed class BoundGenericClassTypeRef : BoundTypeRef
    {
        readonly IBoundTypeRef _targetType;
        readonly ImmutableArray<BoundTypeRef> _typeArguments;

        public BoundGenericClassTypeRef(IBoundTypeRef targetType, ImmutableArray<BoundTypeRef> typeArguments)
        {
            _targetType = targetType ?? throw ExceptionUtilities.ArgumentNull(nameof(targetType));
            _typeArguments = typeArguments;
        }

        public override bool IsObject => true;

        public override ImmutableArray<IBoundTypeRef> TypeArguments => _typeArguments.CastArray<IBoundTypeRef>();

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            var t = ResolveTypeSymbol(cg.DeclaringCompilation);
            return cg.EmitLoadPhpTypeInfo(t);
        }

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation)
        {
            var resolved = (NamedTypeSymbol)(_targetType.Type ?? _targetType.ResolveTypeSymbol(compilation));

            if (resolved.IsValidType())
            {
                // TODO: check _typeArguments are bound (no ErrorSymbol)

                var boundTypeArgs = _typeArguments.SelectAsArray(tref => (TypeSymbol)(tref.Type ?? tref.ResolveTypeSymbol(compilation)));

                return resolved.Construct(boundTypeArgs);
            }
            else
            {
                // TODO: error type symbol
                return compilation.CoreTypes.Object.Symbol;
            }
        }

        public override string ToString() => _targetType.ToString() + "`" + _typeArguments.Length;

        public override TypeRefMask GetTypeRefMask(TypeRefContext ctx) => ctx.GetTypeMask(this, true);

        public override bool Equals(IBoundTypeRef other)
        {
            if (ReferenceEquals(this, other)) return true;

            if (other is BoundGenericClassTypeRef gt && gt._targetType.Equals(_targetType) && gt._typeArguments.Length == _typeArguments.Length)
            {
                for (int i = 0; i < _typeArguments.Length; i++)
                {
                    if (!other.TypeArguments[i].Equals(this.TypeArguments[i])) return false;
                }

                return true;
            }

            return false;
        }
    }

    #endregion

    #region BoundIndirectTypeRef

    sealed class BoundIndirectTypeRef : BoundTypeRef
    {
        public BoundExpression TypeExpression => _typeExpression;
        readonly BoundExpression _typeExpression;

        public BoundIndirectTypeRef(BoundExpression typeExpression, bool objectTypeInfoSemantic)
        {
            _typeExpression = typeExpression ?? throw ExceptionUtilities.ArgumentNull();
            _objectTypeInfoSemantic = objectTypeInfoSemantic;
        }

        public BoundIndirectTypeRef Update(BoundExpression typeExpression, bool objectTypeInfoSemantic)
        {
            if (typeExpression == _typeExpression && objectTypeInfoSemantic == _objectTypeInfoSemantic)
            {
                return this;
            }
            else
            {
                return new BoundIndirectTypeRef(typeExpression, _objectTypeInfoSemantic).WithSyntax(PhpSyntax);
            }
        }

        /// <summary>
        /// Gets value determining the indirect type reference can refer to an object instance which type is used to get the type info.
        /// </summary>
        public bool ObjectTypeInfoSemantic => _objectTypeInfoSemantic;
        readonly bool _objectTypeInfoSemantic;

        /// <summary>
        /// Always <c>false</c>.
        /// </summary>
        public override bool IsNullable
        {
            get { return false; }
            set { Debug.Assert(value == false); }
        }

        public override bool IsObject => true;

        public void EmitClassName(CodeGenerator cg)
        {
            cg.EmitConvert(_typeExpression, cg.CoreTypes.String);
        }

        /// <summary>
        /// Whether this is <c>$this</c> variable used as a type.
        /// </summary>
        bool IsThisVariable
        {
            get
            {
                if (_objectTypeInfoSemantic && _typeExpression is BoundVariableRef varref)
                {
                    return varref.Variable is ThisVariableReference;
                }

                return false;
            }
        }

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
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

        public override bool Equals(IBoundTypeRef other) => base.Equals(other) || (other is BoundIndirectTypeRef it && it._typeExpression == _typeExpression);

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation)
        {
            // MOVED TO GRAPH REWRITER:

            //// string:
            //if (_typeExpression.ConstantValue.TryConvertToString(out var tname))
            //{
            //    return (TypeSymbol)_model.ResolveType(NameUtils.MakeQualifiedName(tname, true));
            //}
            //else if (IsThisVariable)
            //{
            //    // $this:
            //    if (_typeExpression is BoundVariableRef varref && varref.Name.NameValue.IsThisVariableName)
            //    {
            //        if (TypeCtx.ThisType != null && TypeCtx.ThisType.IsSealed)
            //        {
            //            return TypeCtx.ThisType; // $this, self
            //        }
            //    }
            //    //else if (IsClassOnly(tref.TypeExpression.TypeRefMask))
            //    //{
            //    //    // ...
            //    //}
            //}

            return null; // type cannot be resolved
        }

        public override IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target)
        {
            if (source == target) return this;

            // it is "an" object within another routine:
            return new BoundPrimitiveTypeRef(PhpTypeCode.Object) { IsNullable = false };
        }

        public override TypeRefMask GetTypeRefMask(TypeRefContext ctx)
        {
            if (IsThisVariable)
            {
                return ctx.GetThisTypeMask();
            }

            return ctx.GetSystemObjectTypeMask();
        }

        public override string ToString() => "{?}";

        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitIndirectTypeRef(this);
    }

    #endregion

    #region BoundMultipleTypeRef

    sealed class BoundMultipleTypeRef : BoundTypeRef
    {
        public ImmutableArray<BoundTypeRef> TypeRefs { get; private set; }

        public override bool IsObject => true;

        public BoundMultipleTypeRef(ImmutableArray<BoundTypeRef> trefs)
        {
            this.TypeRefs = trefs;
        }

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            throw new NotImplementedException();
        }

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation)
        {
            throw new NotImplementedException();
        }

        public override string ToString() => string.Join("|", TypeRefs);

        public override TypeRefMask GetTypeRefMask(TypeRefContext ctx)
        {
            TypeRefMask result = 0;

            foreach (var t in TypeRefs)
            {
                result |= t.GetTypeRefMask(ctx);
            }

            return result;
        }

        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitMultipleTypeRef(this);

        public BoundMultipleTypeRef Update(ImmutableArray<BoundTypeRef> trefs)
        {
            if (trefs == this.TypeRefs)
            {
                return this;
            }
            else
            {
                return new BoundMultipleTypeRef(trefs).WithSyntax(PhpSyntax);
            }
        }
    }

    #endregion

    #region Helper implementations:

    /// <summary>
    /// <see cref="IBoundTypeRef"/> refering to resolved reference type symbol.
    /// </summary>
    sealed class BoundTypeRefFromSymbol : BoundTypeRef
    {
        readonly ITypeSymbol _symbol;

        bool IsPeachpieCorLibrary => _symbol.ContainingAssembly is AssemblySymbol ass && ass.IsPeachpieCorLibrary;

        public override bool IsObject
        {
            get
            {
                switch (_symbol.SpecialType)
                {
                    // value types acting like PHP objects:
                    case SpecialType.System_DateTime:
                        return true;

                    case SpecialType.System_String:
                        return false;

                    case SpecialType.None:
                        // not PhpArray, PhpResource // TODO: unify this
                        if (_symbol.Is_PhpArray() ||
                            _symbol.Is_PhpAlias())
                        {
                            return false;
                        }

                        return _symbol.IsReferenceType;

                    default:
                        return _symbol.IsReferenceType;
                }
            }
        }

        public override bool IsArray => IsPeachpieCorLibrary && _symbol.Name == "PhpArray";

        public override bool IsLambda => IsPeachpieCorLibrary && _symbol.Name == "Closure";

        public override ITypeSymbol Type => _symbol;

        public BoundTypeRefFromSymbol(ITypeSymbol symbol)
        {
            Debug.Assert(((TypeSymbol)symbol).IsValidType());

            Debug.Assert(!symbol.Is_PhpValue());
            Debug.Assert(!symbol.Is_PhpAlias());

            _symbol = symbol ?? throw ExceptionUtilities.ArgumentNull(nameof(symbol));
        }

        public override string ToString() => _symbol.ToString();

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false) => cg.EmitLoadPhpTypeInfo(_symbol);

        public override IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target) => this;

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation) => _symbol;

        public override TypeRefMask GetTypeRefMask(TypeRefContext ctx)
        {
            var t = (TypeSymbol)_symbol;

            switch (t.SpecialType)
            {
                case SpecialType.System_Void: return 0;
                case SpecialType.System_Boolean: return WithNullableMask(ctx.GetBooleanTypeMask(), ctx);
                case SpecialType.System_Int32:
                case SpecialType.System_Int64: return WithNullableMask(ctx.GetLongTypeMask(), ctx);
                case SpecialType.System_String: return WithNullableMask(ctx.GetStringTypeMask(), ctx);
                case SpecialType.System_Single:
                case SpecialType.System_Double: return WithNullableMask(ctx.GetDoubleTypeMask(), ctx);
                case SpecialType.None:
                    if (IsPeachpieCorLibrary)
                    {
                        if (t.Name == "PhpValue") return TypeRefMask.AnyType;
                        if (t.Name == "PhpAlias") return TypeRefMask.AnyType.WithRefFlag;
                        if (t.Name == "PhpNumber") return WithNullableMask(ctx.GetNumberTypeMask(), ctx);
                        if (t.Name == "PhpString") return WithNullableMask(ctx.GetWritableStringTypeMask(), ctx);
                        if (t.Name == "PhpArray") return WithNullableMask(ctx.GetArrayTypeMask(), ctx);
                        if (t.Name == "IPhpCallable") return WithNullableMask(ctx.GetCallableTypeMask(), ctx);
                        if (t.Name == "PhpResource") return WithNullableMask(ctx.GetResourceTypeMask(), ctx);
                    }

                    break;
            }

            //
            return base.GetTypeRefMask(ctx);
        }

        public override bool Equals(IBoundTypeRef other) => base.Equals(other) || (other is BoundTypeRefFromSymbol ts && ts._symbol == _symbol);
    }

    /// <summary>
    /// Refers to a variable that contains value of <c>PhpTypeInfo</c>.
    /// The type cannot be resolved statically, only <see cref="EmitLoadTypeInfo(CodeGenerator, bool)"/> is applicable.
    /// </summary>
    sealed class BoundTypeRefFromPlace : BoundTypeRef
    {
        readonly IPlace _place;

        public BoundTypeRefFromPlace(IPlace place)
        {
            Debug.Assert(place != null);
            Debug.Assert(place.Type == null || place.Type.Name == "PhpTypeInfo");

            _place = place ?? throw ExceptionUtilities.ArgumentNull(nameof(place));
        }

        public override bool IsObject => true;

        public override ITypeSymbol EmitLoadTypeInfo(CodeGenerator cg, bool throwOnError = false)
        {
            return _place
                .EmitLoad(cg.Builder)
                .Expect(cg.CoreTypes.PhpTypeInfo);
        }

        public override bool Equals(IBoundTypeRef other) => base.Equals(other) || (other is BoundTypeRefFromPlace pt && pt._place == _place);

        public override ITypeSymbol ResolveTypeSymbol(PhpCompilation compilation) => throw ExceptionUtilities.Unreachable;

        public override IBoundTypeRef Transfer(TypeRefContext source, TypeRefContext target) => throw ExceptionUtilities.Unreachable;
    }

    #endregion
}
