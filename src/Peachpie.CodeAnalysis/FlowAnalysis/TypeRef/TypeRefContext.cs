using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Pchp.CodeAnalysis.Utilities;
using Pchp.Core;
using Pchp.CodeAnalysis.Symbols;
using Devsense.PHP.Syntax;
using AST = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Context of <see cref="TypeRefMask"/> and <see cref="ITypeRef"/> instances.
    /// Contains additional information for routine context like current namespace, current type context etc.
    /// </summary>
    public sealed partial class TypeRefContext
    {
        #region Fields & Properties

        /// <summary>
        /// Bit masks initialized when such type is added to the context.
        /// Its bits corresponds to <see cref="_typeRefs"/> indices.
        /// </summary>
        private ulong _isNullMask, _isObjectMask, _isArrayMask, _isLongMask, _isDoubleMask, _isBoolMask, _isStringMask, _isWritableStringMask, _isPrimitiveMask, _isLambdaMask;
        private ulong IsNumberMask { get { return _isLongMask | _isDoubleMask; } }
        private ulong IsAStringMask { get { return _isStringMask | _isWritableStringMask; } }
        private ulong IsNullableMask { get { return _isNullMask | _isObjectMask | _isArrayMask | IsAStringMask | _isLambdaMask; } }

        ///// <summary>
        ///// Allowed types for array key.
        ///// </summary>
        //private ulong IsArrayKeyMask { get { return _isStringMask | _isBoolMask | _isIntMask | _isDoubleMask | _isResourceMask; } }

        /// <summary>
        /// List of types occuring in the context.
        /// </summary>
        private readonly List<ITypeRef>/*!*/_typeRefs;

        /// <summary>
        /// Contains type of current context (refers to <c>self</c> or <c>$this</c>).
        /// Can be <c>null</c>.
        /// </summary>
        internal NamedTypeSymbol ContainingType => _containingType;
        private readonly SourceTypeSymbol _containingType;

        /// <summary>
        /// When resolved, contains type mask of <c>static</c> type.
        /// </summary>
        private TypeRefMask _staticTypeMask;

        /// <summary>
        /// Current source unit. Used for resolving current file name, elements position etc. Can be <c>null</c>.
        /// </summary>
        public SourceUnit SourceUnit { get { return _sourceUnit; } }
        private readonly SourceUnit _sourceUnit;

        #endregion

        #region Initialization

        internal TypeRefContext(SourceUnit sourceUnit, SourceTypeSymbol containingType)
        {
            _sourceUnit = sourceUnit;
            _typeRefs = new List<ITypeRef>();
            _containingType = containingType;
        }

        /// <summary>
        /// Gets type mask corresponding to <c>self</c> with <c>includesSubclasses</c> flag set whether type is not final.
        /// </summary>
        private TypeRefMask GetTypeCtxMask(AST.TypeDecl typeCtx)
        {
            if (typeCtx != null)
            {
                var typeIsFinal = (typeCtx.MemberAttributes & PhpMemberAttributes.Final) != 0;
                return GetTypeMask(new ClassTypeRef(NameUtils.MakeQualifiedName(typeCtx)), !typeIsFinal);
            }
            else
            {
                return TypeRefMask.AnyType;
            }
        }

        /// <summary>
        /// Explicitly defines late static bind type (type of <c>static</c>).
        /// </summary>
        /// <param name="staticTypeMask">Type mask of <c>static</c> or <c>void</c> if this information is unknown.</param>
        internal void SetLateStaticBindType(TypeRefMask staticTypeMask)
        {
            _staticTypeMask = staticTypeMask;
        }

        #endregion

        #region AddToContext

        /// <summary>
        /// Ensures given type is in the context.
        /// </summary>
        /// <param name="typeRef">Type reference to be in the context.</param>
        /// <returns>Index of the type within the context. Can return <c>-1</c> if there is too many types in the context already.</returns>
        public int AddToContext(ITypeRef/*!*/typeRef)
        {
            Contract.ThrowIfNull(typeRef);

            var types = _typeRefs;
            var index = this.GetTypeIndex(typeRef);
            if (index < 0 && this.Types.Count < TypeRefMask.IndicesCount)
                index = this.AddToContextNoCheck(typeRef);

            //
            return index;
        }

        private int AddToContextNoCheck(ITypeRef/*!*/typeRef)
        {
            Contract.ThrowIfNull(typeRef);
            Debug.Assert(_typeRefs.IndexOf(typeRef) == -1);

            int index = _typeRefs.Count;
            this.UpdateMasks(typeRef, index);

            _typeRefs.Add(typeRef);

            //
            return index;
        }

        /// <summary>
        /// Updates internal masks for newly added type.
        /// </summary>
        /// <param name="typeRef">Type.</param>
        /// <param name="index">Type index.</param>
        private void UpdateMasks(ITypeRef/*!*/typeRef, int index)
        {
            Debug.Assert(index >= 0 && index < TypeRefMask.IndicesCount);

            ulong mask = (ulong)1 << index;

            if (typeRef.IsObject) _isObjectMask |= mask;
            if (typeRef.IsArray) _isArrayMask |= mask;
            if (typeRef.IsLambda) _isLambdaMask |= mask;

            if (typeRef.IsPrimitiveType)
            {
                _isPrimitiveMask |= mask;
                switch (typeRef.TypeCode)
                {
                    case PhpTypeCode.Boolean:
                        _isBoolMask = mask;
                        break;
                    case PhpTypeCode.Long:
                        _isLongMask |= mask;
                        break;
                    case PhpTypeCode.Double:
                        _isDoubleMask = mask;
                        break;
                    case PhpTypeCode.String:
                        _isStringMask = mask;
                        break;
                    case PhpTypeCode.Null:
                        _isNullMask = mask;
                        break;
                    case PhpTypeCode.WritableString:
                        _isWritableStringMask = mask;
                        break;
                }
            }
        }

        /// <summary>
        /// Adds properly types from another context.
        /// </summary>
        /// <param name="other">Another type context which types will be added to this one.</param>
        internal void AddToContext(TypeRefContext/*!*/other)
        {
            Contract.ThrowIfNull(other);

            foreach (var typeref in other.Types)
            {
                AddToContext(typeref.Transfer(other, this));
            }
        }

        /// <summary>
        /// Adds properly types from another context matching given mask.
        /// </summary>
        /// <param name="context">Context of <paramref name="mask"/>.</param>
        /// <param name="mask">Type mask representing types in <paramref name="context"/>.</param>
        /// <returns>Returns type mask in this context representing <paramref name="mask"/> as <paramref name="context"/>.</returns>
        public TypeRefMask AddToContext(TypeRefContext/*!*/context, TypeRefMask mask)
        {
            Contract.ThrowIfNull(context);

            if (mask.IsAnyType || mask.IsVoid || object.ReferenceEquals(this, context))
                return mask;

            var result = default(TypeRefMask);

            var types = context.Types;
            var count = Math.Min(types.Count, TypeRefMask.IndicesCount);
            for (int i = 0; i < count; i++)
            {
                if (mask.HasType(i))
                {
                    var index = AddToContext(types[i].Transfer(context, this));
                    result.AddType(index);
                }
            }

            //
            result.IsRef = mask.IsRef;
            result.IncludesSubclasses = mask.IncludesSubclasses;

            //
            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets enumeration of types matching given masks.
        /// </summary>
        private IList<ITypeRef>/*!!*/GetTypes(TypeRefMask typemask, ulong bitmask)
        {
            var mask = typemask.Mask & bitmask & ~TypeRefMask.FlagsMask;
            if (mask == (ulong)0 || typemask.IsAnyType)
                return EmptyArray<ITypeRef>.Instance;

            var result = new List<ITypeRef>(1);
            for (int i = 0; mask != 0; i++, mask = (mask & ~(ulong)1) >> 1)
                if ((mask & 1) != 0)
                {
                    Debug.Assert(i < _typeRefs.Count);
                    result.Add(_typeRefs[i]);
                }

            return result;
        }

        private TypeRefMask GetPrimitiveTypeRefMask(PrimitiveTypeRef/*!*/typeref)
        {
            // primitive type cannot include subclasses
            var index = AddToContext(typeref);
            return TypeRefMask.CreateFromTypeIndex(index);
        }

        /// <summary>
        /// Does not lookup existing types whether there is typeref already.
        /// </summary>
        private TypeRefMask GetPrimitiveTypeRefMaskNoCheck(PrimitiveTypeRef/*!*/typeref)
        {
            if (this.Types.Count < TypeRefMask.IndicesCount)
            {
                var index = AddToContextNoCheck(typeref);
                return TypeRefMask.CreateFromTypeIndex(index);
            }
            else
            {
                return TypeRefMask.AnyType;
            }
        }

        /// <summary>
        /// Gets type mask for reserved class name (self, parent, static).
        /// </summary>
        private TypeRefMask GetTypeMaskOfReservedClassName(Name name)
        {
            if (name == Name.SelfClassName) return GetSelfTypeMask();
            if (name == Name.ParentClassName) return GetParentTypeMask();
            if (name == Name.StaticClassName) return GetStaticTypeMask();

            throw new ArgumentException();
        }

        #endregion

        #region GetTypeMask

        /// <summary>
        /// Helper method that builds <see cref="TypeRefMask"/> for given type in this context.
        /// </summary>
        public TypeRefMask GetTypeMask(ITypeRef/*!*/typeref, bool includesSubclasses)
        {
            var index = AddToContext(typeref);
            var mask = TypeRefMask.CreateFromTypeIndex(index);

            if (includesSubclasses && typeref.IsObject)
                mask.SetIncludesSubclasses();

            return mask;
        }

        /// <summary>
        /// Gets type mask corresponding to given qualified name within this context.
        /// </summary>
        public TypeRefMask GetTypeMask(QualifiedName qname, bool includesSubclasses = true)
        {
            if (qname.IsReservedClassName)
                return GetTypeMaskOfReservedClassName(qname.Name);

            return GetTypeMask(new ClassTypeRef(qname), includesSubclasses);
        }

        /// <summary>
        /// Gets type mask corresponding to given TypeRef within this context.
        /// </summary>
        public TypeRefMask GetTypeMask(AST.TypeRef/*!*/tref, bool includesSubclasses = true)
        {
            Contract.ThrowIfNull(tref);

            if (tref != null)
            {
                if (tref is AST.PrimitiveTypeRef)
                {
                    switch (((AST.PrimitiveTypeRef)tref).PrimitiveTypeName)
                    {
                        case AST.PrimitiveTypeRef.PrimitiveType.@int: return GetLongTypeMask();
                        case AST.PrimitiveTypeRef.PrimitiveType.@float: return GetDoubleTypeMask();
                        case AST.PrimitiveTypeRef.PrimitiveType.@string: return GetStringTypeMask();
                        case AST.PrimitiveTypeRef.PrimitiveType.@bool: return GetBooleanTypeMask();
                        case AST.PrimitiveTypeRef.PrimitiveType.array: return GetArrayTypeMask();
                        case AST.PrimitiveTypeRef.PrimitiveType.callable: return GetCallableTypeMask();
                        case AST.PrimitiveTypeRef.PrimitiveType.@void: return 0;
                        case AST.PrimitiveTypeRef.PrimitiveType.iterable: return GetArrayTypeMask() | GetTypeMask(NameUtils.SpecialNames.Traversable, true);   // array | Traversable
                        default: throw new ArgumentException();
                    }
                }
                else if (tref is AST.INamedTypeRef) return GetTypeMask(((AST.INamedTypeRef)tref).ClassName, includesSubclasses);
                else if (tref is AST.ReservedTypeRef) return GetTypeMaskOfReservedClassName(((AST.ReservedTypeRef)tref).QualifiedName.Value.Name); // NOTE: should be translated by parser to AliasedTypeRef
                else if (tref is AST.AnonymousTypeRef) return GetTypeMask(((AST.AnonymousTypeRef)tref).TypeDeclaration.GetAnonymousTypeQualifiedName(), false);
                else if (tref is AST.MultipleTypeRef)
                {
                    TypeRefMask result = 0;
                    foreach (var x in ((AST.MultipleTypeRef)tref).MultipleTypes)
                    {
                        result |= GetTypeMask(x, includesSubclasses);
                    }
                    return result;
                }
                else if (tref is AST.NullableTypeRef) return GetTypeMask(((AST.NullableTypeRef)tref).TargetType) | this.GetNullTypeMask();
                else if (tref is AST.GenericTypeRef) return GetTypeMask(((AST.GenericTypeRef)tref).TargetType, includesSubclasses);  // TODO: now we are ignoring type args
                else if (tref is AST.IndirectTypeRef) return GetTypeMask((AST.IndirectTypeRef)tref, true);
            }

            return TypeRefMask.AnyType;
        }

        /// <summary>
        /// Gets type mask corresponding to given TypeRef within this context.
        /// </summary>
        private TypeRefMask GetTypeMask(AST.IndirectTypeRef/*!*/tref, bool includesSubclasses)
        {
            Contract.ThrowIfNull(tref);

            var dvar = tref.ClassNameVar as AST.DirectVarUse;
            if (dvar != null && dvar.IsMemberOf == null && dvar.VarName.IsThisVariableName)
                return GetThisTypeMask();

            //
            return TypeRefMask.AnyType;
        }

        /// <summary>
        /// Gets type mask corresponding to <see cref="System.Object"/>.
        /// </summary>
        public TypeRefMask GetSystemObjectTypeMask()
        {
            return GetTypeMask(NameUtils.SpecialNames.System_Object, true);
        }

        /// <summary>
        /// Gets type mask corresponding to <c>NULL</c>.
        /// </summary>
        public TypeRefMask GetNullTypeMask()
        {
            if (_isNullMask != 0)
            {
                return new TypeRefMask(_isNullMask);
            }
            else
            {
                return GetPrimitiveTypeRefMaskNoCheck(TypeRefFactory.NullTypeRef);
            }
        }

        /// <summary>
        /// Gets <c>string</c> type for this context.
        /// </summary>
        public TypeRefMask GetStringTypeMask()
        {
            if (_isStringMask != 0)
            {
                return new TypeRefMask(_isStringMask);
            }
            else
            {
                return GetPrimitiveTypeRefMaskNoCheck(TypeRefFactory.StringTypeRef);
            }
        }

        /// <summary>
        /// Gets writable <c>string</c> type (a string builder) for this context.
        /// </summary>
        public TypeRefMask GetWritableStringTypeMask()
        {
            if (_isWritableStringMask != 0)
            {
                return new TypeRefMask(_isWritableStringMask);
            }
            else
            {
                return GetPrimitiveTypeRefMaskNoCheck(TypeRefFactory.WritableStringRef);
            }
        }

        /// <summary>
        /// Gets <c>int</c> type for this context.
        /// </summary>
        public TypeRefMask GetLongTypeMask()
        {
            if (_isLongMask != 0)
            {
                return new TypeRefMask(_isLongMask);
            }
            else
            {
                return GetPrimitiveTypeRefMaskNoCheck(TypeRefFactory.LongTypeRef);
            }
        }

        /// <summary>
        /// Gets <c>bool</c> type for this context.
        /// </summary>
        public TypeRefMask GetBooleanTypeMask()
        {
            if (_isBoolMask != 0)
            {
                return new TypeRefMask(_isBoolMask);
            }
            else
            {
                return GetPrimitiveTypeRefMaskNoCheck(TypeRefFactory.BoolTypeRef);
            }
        }

        /// <summary>
        /// Gets <c>double</c> type for this context.
        /// </summary>
        public TypeRefMask GetDoubleTypeMask()
        {
            if (_isDoubleMask != 0)
            {
                return new TypeRefMask(_isDoubleMask);
            }
            else
            {
                return GetPrimitiveTypeRefMaskNoCheck(TypeRefFactory.DoubleTypeRef);
            }
        }

        /// <summary>
        /// Gets <c>number</c> (<c>int</c> and <c>double</c>) type for this context.
        /// </summary>
        public TypeRefMask GetNumberTypeMask()
        {
            return GetLongTypeMask() | GetDoubleTypeMask();
        }

        /// <summary>
        /// Gets type mask of a resource type.
        /// </summary>
        public TypeRefMask GetResourceTypeMask()
        {
            return GetPrimitiveTypeRefMask(TypeRefFactory.ResourceTypeRef);
        }

        /// <summary>
        /// Gets type mask of all callable types.
        /// </summary>
        public TypeRefMask GetCallableTypeMask()
        {
            return GetPrimitiveTypeRefMask(TypeRefFactory.CallableTypeRef);
        }

        /// <summary>
        /// Gets type mask of generic <c>array</c> with element of any type.
        /// </summary>
        public TypeRefMask GetArrayTypeMask()
        {
            return GetPrimitiveTypeRefMask(TypeRefFactory.ArrayTypeRef);
        }

        /// <summary>
        /// Gets type mask of <c>array</c> with elements of given type.
        /// </summary>
        public TypeRefMask GetArrayTypeMask(TypeRefMask elementType)
        {
            TypeRefMask result;

            if (elementType.IsAnyType)
            {
                result = GetArrayTypeMask();  // generic array
            }
            else if (elementType.IsVoid)
            {
                result = GetTypeMask(new ArrayTypeRef(null, 0), false);   // empty array
            }
            else if (elementType.IsSingleType)
            {
                result = GetTypeMask(new ArrayTypeRef(null, elementType), false);
            }
            else
            {
                result = 0;

                if ((elementType & _isArrayMask) != 0)  // array elements contain another arrays
                {
                    // simplify this to generic arrays
                    elementType &= ~_isArrayMask;
                    elementType |= GetArrayTypeMask();
                }

                // construct array type mask from array types with single element type

                // go through all array types
                var mask = elementType & ~(ulong)TypeRefMask.FlagsMask;
                for (int i = 0; mask != 0; i++, mask = (mask & ~(ulong)1) >> 1)
                    if ((mask & 1) != 0)    // _typeRefs[i].IsArray
                    {
                        result |= GetTypeMask(new ArrayTypeRef(null, (ulong)1 << i), false);
                    }
            }

            //
            return result;
        }

        /// <summary>
        /// Gets <c>self</c> type for this context.
        /// </summary>
        public TypeRefMask GetSelfTypeMask()
        {
            TypeRefMask result = TypeRefMask.AnyType;

            if (_containingType != null)
            {
                result = GetTypeCtxMask(_containingType.Syntax);
                result.IncludesSubclasses = false;
            }

            return result;
        }

        /// <summary>
        /// Gets type of <c>$this</c> in current context.
        /// </summary>
        public TypeRefMask GetThisTypeMask()
        {
            return GetTypeCtxMask(_containingType?.Syntax);
        }

        /// <summary>
        /// Gets <c>parent</c> type for this context.
        /// </summary>
        public TypeRefMask GetParentTypeMask()
        {
            if (_containingType != null && _containingType.Syntax.BaseClass != null)
            {
                return GetTypeMask(new ClassTypeRef(_containingType.Syntax.BaseClass.ClassName), false);
            }
            else
            {
                return TypeRefMask.AnyType;
            }
        }

        /// <summary>
        /// Gets <c>static</c> type for this context.
        /// </summary>
        public TypeRefMask GetStaticTypeMask()
        {
            if (_staticTypeMask == 0)
            {
                var mask = GetThisTypeMask();
                if (mask != 0)
                    mask.IncludesSubclasses = true;

                _staticTypeMask = mask;
            }

            return _staticTypeMask;
        }

        /// <summary>
        /// Gets mask representing only array types in given mask.
        /// (Only bits corresponding to an array type will be set).
        /// </summary>
        public TypeRefMask GetArraysFromMask(TypeRefMask mask)
        {
            if (mask.IsAnyType) return 0;
            return mask & _isArrayMask;
        }

        /// <summary>
        /// Gets mask representing only object types in given mask.
        /// (Only bits corresponding to an object type will be set).
        /// </summary>
        public TypeRefMask GetObjectsFromMask(TypeRefMask mask)
        {
            if (mask.IsAnyType) return 0;
            return mask & _isObjectMask;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets enumeration of all types in the context.
        /// </summary>
        public IList<ITypeRef>/*!*/Types { get { return _typeRefs; } }

        /// <summary>
        /// Gets types referenced by given type mask.
        /// </summary>
        public IList<ITypeRef>/*!*/GetTypes(TypeRefMask mask)
        {
            return GetTypes(mask, TypeRefMask.AnyTypeMask);
        }

        /// <summary>
        /// Gets types of type <c>object</c> (classes, interfaces, traits) referenced by given type mask.
        /// </summary>
        public IList<ITypeRef>/*!*/GetObjectTypes(TypeRefMask mask)
        {
            if (mask.IsAnyType)
                return EmptyArray<ITypeRef>.Instance;

            return GetTypes(mask, _isObjectMask);
        }

        /// <summary>
        /// Gets string representation of types contained in given type mask.
        /// </summary>
        public string ToString(TypeRefMask mask)
        {
            if (!mask.IsVoid)
            {
                if (mask.IsAnyType)
                    return TypeRefMask.MixedTypeName;

                //
                var types = new List<string>(1);

                // handle arrays separately
                var arrmask = mask & _isArrayMask;
                if (arrmask != 0)
                {
                    mask &= ~_isArrayMask;
                    ITypeRef elementtype = null;
                    var elementmask = GetElementType(arrmask);
                    if (elementmask.IsSingleType)
                        elementtype = GetTypes(elementmask).FirstOrDefault();

                    if (elementtype != null)
                        types.Add(elementtype.QualifiedName.ToString() + "[]");
                    else
                        types.Add(TypeRefFactory.ArrayTypeRef.QualifiedName.ToString());
                }

                //// int|double => number
                //var isNumber = (_isIntMask != 0 && _isDoubleMask != 0 && (mask & IsNumberMask) == IsNumberMask);
                //if (isNumber)
                //    mask &= ~IsNumberMask;

                //if (IsNull(mask))
                //{
                //    mask &= ~_isNullMask;
                //    types.Add(QualifiedName.Null.ToString());
                //}

                //
                types.AddRange(GetTypes(mask).Select(t => t.QualifiedName.ToString()));

                //if (isNumber)
                //    types.Add("number");

                //
                if (types.Count != 0)
                {
                    types.Sort();
                    return string.Join(PHPDocBlock.TypeVarDescTag.TypeNamesSeparator.ToString(), types.Distinct());
                }
            }

            return TypeRefMask.VoidTypeName;
        }

        /// <summary>
        /// Gets index of the given type within the context. Returns <c>-1</c> if such type is not present.
        /// </summary>
        public int GetTypeIndex(ITypeRef/*!*/typeref) { return _typeRefs.IndexOf(typeref); }

        /// <summary>
        /// Gets value indicating whether given type mask represents a number.
        /// </summary>
        public bool IsNumber(TypeRefMask mask) { return (mask.Mask & IsNumberMask) != 0; }

        /// <summary>
        /// Gets value indicating the type represents <c>NULL</c>.
        /// </summary>
        public bool IsNull(TypeRefMask mask) { return (mask.Mask & _isNullMask) != 0 && !mask.IsAnyType; }

        /// <summary>
        /// Gets value indicating whether given type mask represents a string type (readonly or writable).
        /// </summary>
        public bool IsAString(TypeRefMask mask) { return (mask.Mask & IsAStringMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type mask represents UTF16 readonly string.
        /// </summary>
        public bool IsReadonlyString(TypeRefMask mask) { return (mask.Mask & _isStringMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type mask represents a writablke string (string builder).
        /// </summary>
        public bool IsWritableString(TypeRefMask mask) { return (mask.Mask & _isWritableStringMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type mask represents a boolean.
        /// </summary>
        public bool IsBoolean(TypeRefMask mask) { return (mask.Mask & _isBoolMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type mask represents an integer type.
        /// </summary>
        public bool IsLong(TypeRefMask mask) { return (mask.Mask & _isLongMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type mask represents a double type.
        /// </summary>
        public bool IsDouble(TypeRefMask mask) { return (mask.Mask & _isDoubleMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type mask represents an object.
        /// </summary>
        public bool IsObject(TypeRefMask mask) { return (mask.Mask & _isObjectMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type mask represents an array.
        /// </summary>
        public bool IsArray(TypeRefMask mask) { return (mask.Mask & _isArrayMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type mask represents a lambda function or <c>callable</c> primitive type.
        /// </summary>
        public bool IsLambda(TypeRefMask mask) { return (mask.Mask & _isLambdaMask) != 0; }

        ///// <summary>
        ///// Gets value indicating whether given type mask represents a resource.
        ///// </summary>
        //public bool IsResource(TypeRefMask mask) { return GetObjectTypes(mask).Any(InheritesFromPhpResource); }

        /// <summary>
        /// Gets value indicating whether given type mask represents a primitive type.
        /// </summary>
        public bool IsPrimitiveType(TypeRefMask mask) { return (mask.Mask & _isPrimitiveMask) != 0; }

        /// <summary>
        /// Gets value indicating whether given type can be <c>null</c>.
        /// </summary>
        public bool IsNullable(TypeRefMask mask) { return (mask.Mask & IsNullableMask) != 0; }

        //public bool IsArrayKey(TypeRefMask mask) { return (mask.Mask & IsArrayKeyMask) != 0; }  // TODO: type can be of type object with method __toString() ?

        /// <summary>
        /// In case of array type, gets its possible element types.
        /// </summary>
        public TypeRefMask GetElementType(TypeRefMask mask)
        {
            TypeRefMask result;
            if (IsArray(mask) && !mask.IsAnyType)
            {
                result = default(TypeRefMask);  // uninitalized

                var arrtypes = GetTypes(mask, _isArrayMask);
                foreach (var t in arrtypes)
                {
                    Debug.Assert(t.IsArray);
                    result |= t.ElementType;
                }

                if (result.IsVoid)
                {
                    // empty array
                    //result = TypeRefMask.AnyType;
                }
            }
            else
            {
                result = TypeRefMask.AnyType;
            }

            return result;
        }

        /// <summary>
        /// Remove <c>NULL</c> type from the given mask.
        /// </summary>
        public TypeRefMask WithoutNull(TypeRefMask mask)
        {
            if (IsNull(mask))
            {
                Debug.Assert(!mask.IsAnyType);
                mask = mask & ~_isNullMask;
            }

            return mask;
        }

        #endregion
    }
}
