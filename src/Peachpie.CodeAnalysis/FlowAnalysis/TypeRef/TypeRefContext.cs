using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.TypeRef;
using Pchp.CodeAnalysis.Symbols;
using AST = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Context of <see cref="TypeRefMask"/> and <see cref="IBoundTypeRef"/> instances.
    /// Contains additional information for routine context like current namespace, current type context etc.
    /// </summary>
    public sealed partial class TypeRefContext
    {
        #region Fields & Properties

        /// <summary>
        /// Bit masks initialized when such type is added to the context.
        /// Its bits corresponds to <see cref="_typeRefs"/> indices.
        /// </summary>
        private ulong _isNullMask, _isObjectMask, _isArrayMask, _isLongMask, _isDoubleMask, _isBoolMask, _isStringMask, _isWritableStringMask, _isLambdaMask;
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
        private readonly List<IBoundTypeRef>/*!*/_typeRefs = new List<IBoundTypeRef>();

        /// <summary>
        /// Contains type of current context (refers to <c>self</c>).
        /// Can be <c>null</c>.
        /// </summary>
        internal NamedTypeSymbol SelfType => _selfType;
        private readonly SourceTypeSymbol _selfType;

        /// <summary>
        /// Type corresponding to <c>$this</c> variable.
        /// Can be <c>null</c> if <c>$this</c> is resolved in runtime.
        /// </summary>
        internal NamedTypeSymbol ThisType => _thisType;
        private readonly SourceTypeSymbol _thisType;

        /// <summary>
        /// When resolved, contains type mask of <c>static</c> type.
        /// </summary>
        private TypeRefMask _staticTypeMask;

        #endregion

        #region Initialization

        internal TypeRefContext(SourceTypeSymbol selfType)
            : this(selfType, thisType: selfType)
        { }

        internal TypeRefContext(SourceTypeSymbol selfType, SourceTypeSymbol thisType)
        {
            _selfType = selfType;
            _thisType = thisType;
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
        public int AddToContext(IBoundTypeRef/*!*/typeRef)
        {
            Contract.ThrowIfNull(typeRef);

            var types = _typeRefs;
            var index = this.GetTypeIndex(typeRef);
            if (index < 0 && this.Types.Count < TypeRefMask.IndicesCount)
                index = this.AddToContextNoCheck(typeRef);

            //
            return index;
        }

        private int AddToContextNoCheck(IBoundTypeRef/*!*/typeRef)
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
        private void UpdateMasks(IBoundTypeRef/*!*/typeRef, int index)
        {
            Debug.Assert(index >= 0 && index < TypeRefMask.IndicesCount);

            ulong mask = (ulong)1 << index;

            if (typeRef.IsObject) _isObjectMask |= mask;
            if (typeRef.IsArray) _isArrayMask |= mask;
            if (typeRef.IsLambda) _isLambdaMask |= mask;

            if (typeRef is BoundPrimitiveTypeRef pt)
            {
                switch (pt.TypeCode)
                {
                    case PhpTypeCode.Boolean:
                        _isBoolMask = mask;
                        break;
                    case PhpTypeCode.Long:
                        _isLongMask = mask;
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
        private IList<IBoundTypeRef>/*!!*/GetTypes(TypeRefMask typemask, ulong bitmask)
        {
            var mask = typemask.Mask & bitmask & ~TypeRefMask.FlagsMask;
            if (mask == (ulong)0 || typemask.IsAnyType)
                return EmptyArray<IBoundTypeRef>.Instance;

            var result = new List<IBoundTypeRef>(1);
            for (int i = 0; mask != 0; i++, mask = (mask & ~(ulong)1) >> 1)
                if ((mask & 1) != 0)
                {
                    Debug.Assert(i < _typeRefs.Count);
                    result.Add(_typeRefs[i]);
                }

            return result;
        }

        private TypeRefMask GetPrimitiveTypeRefMask(IBoundTypeRef/*!*/typeref)
        {
            Debug.Assert(typeref.IsPrimitiveType);

            // primitive type cannot include subclasses
            var index = AddToContext(typeref);
            return TypeRefMask.CreateFromTypeIndex(index);
        }

        /// <summary>
        /// Does not lookup existing types whether there is typeref already.
        /// </summary>
        private TypeRefMask GetPrimitiveTypeRefMaskNoCheck(IBoundTypeRef/*!*/typeref)
        {
            Debug.Assert(typeref.IsPrimitiveType);

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

        #endregion

        #region GetTypeMask

        /// <summary>
        /// Helper method that builds <see cref="TypeRefMask"/> for given type in this context.
        /// </summary>
        public TypeRefMask GetTypeMask(IBoundTypeRef/*!*/typeref, bool includesSubclasses)
        {
            var index = AddToContext(typeref);
            var mask = TypeRefMask.CreateFromTypeIndex(index);

            if (includesSubclasses && typeref.IsObject)
            {
                mask.SetIncludesSubclasses();
            }

            if (typeref.IsNullable)
            {
                mask |= GetNullTypeMask();
            }

            return mask;
        }

        /// <summary>
        /// Gets type mask corresponding to <see cref="System.Object"/>.
        /// </summary>
        public TypeRefMask GetSystemObjectTypeMask() => GetTypeMask(BoundTypeRefFactory.ObjectTypeRef, true);

        /// <summary>
        /// Gets type mask corresponding to <c>NULL</c>.
        /// </summary>
        public TypeRefMask GetNullTypeMask()
        {
            if (_isNullMask != 0)
            {
                return _isNullMask;
            }
            else
            {
                return GetPrimitiveTypeRefMaskNoCheck(BoundTypeRefFactory.NullTypeRef);
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
                return GetPrimitiveTypeRefMaskNoCheck(BoundTypeRefFactory.StringTypeRef);
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
                return GetPrimitiveTypeRefMaskNoCheck(BoundTypeRefFactory.WritableStringRef);
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
                return GetPrimitiveTypeRefMaskNoCheck(BoundTypeRefFactory.LongTypeRef);
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
                return GetPrimitiveTypeRefMaskNoCheck(BoundTypeRefFactory.BoolTypeRef);
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
                return GetPrimitiveTypeRefMaskNoCheck(BoundTypeRefFactory.DoubleTypeRef);
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
            return GetPrimitiveTypeRefMask(BoundTypeRefFactory.ResourceTypeRef);
        }

        /// <summary>
        /// Gets type mask of a closure.
        /// </summary>
        public TypeRefMask GetClosureTypeMask()
        {
            return GetTypeMask(BoundTypeRefFactory.ClosureTypeRef, false);
        }

        /// <summary>
        /// Gets type mask of all callable types.
        /// </summary>
        public TypeRefMask GetCallableTypeMask()
        {
            // string | Closure | array | object
            return GetStringTypeMask() | GetClosureTypeMask() | GetArrayTypeMask() | GetSystemObjectTypeMask();
        }

        /// <summary>
        /// Gets type mask of generic <c>array</c> with element of any type.
        /// </summary>
        public TypeRefMask GetArrayTypeMask()
        {
            return GetPrimitiveTypeRefMask(BoundTypeRefFactory.ArrayTypeRef);
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
                result = GetTypeMask(new BoundArrayTypeRef(0), false);   // empty array
            }
            else if (elementType.IsSingleType)
            {
                result = GetTypeMask(new BoundArrayTypeRef(elementType), false);
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
                        result |= GetTypeMask(new BoundArrayTypeRef((ulong)1 << i), false);
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
            TypeRefMask result;

            if (_selfType != null && !_selfType.IsTrait)
            {
                result = GetTypeMask(BoundTypeRefFactory.Create(_selfType), includesSubclasses: false);
            }
            else
            {
                result = GetSystemObjectTypeMask();
            }

            return result;
        }

        /// <summary>
        /// Gets type of <c>$this</c> in current context.
        /// </summary>
        public TypeRefMask GetThisTypeMask()
        {
            if (_thisType != null)
            {
                return GetTypeMask(BoundTypeRefFactory.Create(_thisType), includesSubclasses: !_thisType.IsSealed);
            }
            else
            {
                return GetSystemObjectTypeMask();
            }
        }

        /// <summary>
        /// Gets <c>parent</c> type for this context.
        /// </summary>
        public TypeRefMask GetParentTypeMask()
        {
            if (_selfType != null && _selfType.Syntax.BaseClass != null)
            {
                return GetTypeMask(BoundTypeRefFactory.Create(_selfType.BaseType), false);
            }
            else
            {
                return GetSystemObjectTypeMask();
            }
        }

        /// <summary>
        /// Gets <c>static</c> type for this context.
        /// </summary>
        public TypeRefMask GetStaticTypeMask()
        {
            if (_staticTypeMask == 0)
            {
                _staticTypeMask = GetThisTypeMask();    // including subclasses
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
        public IList<IBoundTypeRef>/*!*/Types { get { return _typeRefs; } }

        /// <summary>
        /// Gets types referenced by given type mask.
        /// </summary>
        public IList<IBoundTypeRef>/*!*/GetTypes(TypeRefMask mask)
        {
            return GetTypes(mask, TypeRefMask.AnyTypeMask);
        }

        /// <summary>
        /// Gets types of type <c>object</c> (classes, interfaces, traits) referenced by given type mask.
        /// </summary>
        public IList<IBoundTypeRef>/*!*/GetObjectTypes(TypeRefMask mask)
        {
            if (mask.IsAnyType)
                return EmptyArray<IBoundTypeRef>.Instance;

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
                    IBoundTypeRef elementtype = null;
                    var elementmask = GetElementType(arrmask);
                    if (elementmask.IsSingleType)
                        elementtype = GetTypes(elementmask).FirstOrDefault();

                    if (elementtype != null)
                        types.Add(elementtype.ToString() + "[]");
                    else
                        types.Add(QualifiedName.Array.ToString());
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
                types.AddRange(GetTypes(mask).Select(t => t.ToString()));

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
        public int GetTypeIndex(IBoundTypeRef/*!*/typeref) { return _typeRefs.IndexOf(typeref); }

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
