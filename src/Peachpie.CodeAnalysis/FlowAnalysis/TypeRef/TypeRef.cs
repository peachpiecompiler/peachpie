using Devsense.PHP.Syntax;
using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AST = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents a direct type reference.
    /// </summary>
    internal class ClassTypeRef : ITypeRef, IEquatable<ClassTypeRef>
    {
        private readonly QualifiedName _qname;

        public ClassTypeRef(QualifiedName qname)
        {
            Debug.Assert(!string.IsNullOrEmpty(qname.Name.Value));
            Debug.Assert(!qname.IsReservedClassName);    // not self, parent, static
            _qname = qname;
        }

        #region ITypeRef Members

        public QualifiedName QualifiedName { get { return _qname; } }

        public bool IsObject { get { return true; } }

        public bool IsArray { get { return false; } }

        public bool IsPrimitiveType { get { return false; } }

        public bool IsLambda { get { return false; } }

        public IEnumerable<object> Keys { get { throw new InvalidOperationException(); } }

        public TypeRefMask ElementType { get { return default(TypeRefMask); } }

        public TypeRefMask LambdaReturnType { get { throw new InvalidOperationException(); } }

        public AST.Signature LambdaSignature { get { throw new InvalidOperationException(); } }

        public PhpTypeCode TypeCode { get { return PhpTypeCode.Object; } }

        public ITypeRef/*!*/Transfer(TypeRefContext/*!*/source, TypeRefContext/*!*/target) { return this; }   // there is nothing depending on the context
        
        #endregion

        #region IEquatable<ITypeRef> Members

        public override int GetHashCode()
        {
            return _qname.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ClassTypeRef);
        }

        public bool Equals(ITypeRef other)
        {
            return Equals(other as ClassTypeRef);
        }

        #endregion

        #region IEquatable<ClassTypeRef> Members

        public bool Equals(ClassTypeRef other)
        {
            return other != null && other._qname == _qname;
        }

        #endregion
    }

    /// <summary>
    /// Represents an array type.
    /// </summary>
    internal sealed class ArrayTypeRef : ITypeRef, IEquatable<ArrayTypeRef>
    {
        // TODO: manage keys, handle duplicities (HashSet?), fast merging
        // flag whether there might be more keys in addition to listed\
        // limit number of keys, ignore numeric keys? remember just strings and constant names

        private readonly HashSet<object> _keys;
        private readonly TypeRefMask _elementType;

        public ArrayTypeRef(IEnumerable<object> keys, TypeRefMask elementType)
        {
            _keys = null;// new HashSet<object>(keys);
            _elementType = elementType;
        }

        #region ITypeRef Members

        public QualifiedName QualifiedName
        {
            get { return QualifiedName.Array; }
        }

        public bool IsObject { get { return false; } }

        public bool IsArray { get { return true; } }

        public bool IsPrimitiveType { get { return true; } }

        public bool IsLambda { get { return false; } }

        public IEnumerable<object> Keys { get { return (IEnumerable<object>)_keys ?? ArrayUtils.EmptyObjects; } }

        public TypeRefMask ElementType { get { return _elementType; } }

        public TypeRefMask LambdaReturnType { get { throw new InvalidOperationException(); } }

        public AST.Signature LambdaSignature { get { throw new InvalidOperationException(); } }

        public PhpTypeCode TypeCode { get { return PhpTypeCode.PhpArray; } }

        public ITypeRef/*!*/Transfer(TypeRefContext/*!*/source, TypeRefContext/*!*/target)
        {
            Contract.ThrowIfNull(source);
            Contract.ThrowIfNull(target);

            // TODO: keys

            if (source == target || _elementType.IsVoid || _elementType.IsAnyType)
                return this;

            // note: there should be no circular dependency
            return new ArrayTypeRef(_keys, target.AddToContext(source, _elementType));
        }

        #endregion

        #region IEquatable<ITypeRef> Members

        public override bool Equals(object obj)
        {
            return Equals(obj as ArrayTypeRef);
        }

        public override int GetHashCode()
        {
            return ~_elementType.GetHashCode();
        }

        public bool Equals(ITypeRef other)
        {
            return Equals(other as ArrayTypeRef);
        }

        #endregion

        #region IEquatable<ArrayTypeRef> Members

        public bool Equals(ArrayTypeRef other)
        {
            return other != null && other._elementType == _elementType;    // TODO: keys
        }

        #endregion
    }

    /// <summary>
    /// Represents a PHP primitive type.
    /// </summary>
    internal sealed class PrimitiveTypeRef : ITypeRef, IEquatable<PrimitiveTypeRef>
    {
        private readonly PhpTypeCode _code;

        public PrimitiveTypeRef(PhpTypeCode code)
        {
            _code = code;
        }

        #region ITypeRef Members

        public QualifiedName QualifiedName
        {
            get
            {
                switch (_code)
                {
                    case PhpTypeCode.Boolean: return QualifiedName.Boolean;
                    case PhpTypeCode.Int32:
                    case PhpTypeCode.Long: return QualifiedName.Integer;
                    case PhpTypeCode.Double: return QualifiedName.Double;
                    case PhpTypeCode.WritableString:
                    case PhpTypeCode.String: return QualifiedName.String;
                    case PhpTypeCode.PhpArray: return QualifiedName.Array;
                    case PhpTypeCode.Null: return QualifiedName.Null;
                    default:
                        throw new ArgumentException();
                }
            }
        }

        public bool IsObject { get { return _code == PhpTypeCode.Object; } }

        public bool IsArray { get { return _code == PhpTypeCode.PhpArray; } }

        public bool IsPrimitiveType { get { return true; } }

        public bool IsLambda { get { return _code == PhpTypeCode.Callable; } }

        public IEnumerable<object> Keys { get { return null; } }

        public TypeRefMask ElementType { get { return TypeRefMask.AnyType; } }

        public TypeRefMask LambdaReturnType { get { return TypeRefMask.AnyType; } }

        public AST.Signature LambdaSignature { get { return default(AST.Signature); } }

        /// <summary>
        /// Gets underlaying type code of the primitive type.
        /// </summary>
        public PhpTypeCode TypeCode { get { return _code; } }

        public ITypeRef/*!*/Transfer(TypeRefContext/*!*/source, TypeRefContext/*!*/target) { return this; }

        #endregion

        #region IEquatable<ITypeRef> Members

        public override int GetHashCode()
        {
            return (int)_code;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PrimitiveTypeRef);
        }

        public bool Equals(ITypeRef other)
        {
            return Equals(other as PrimitiveTypeRef);
        }

        #endregion

        #region IEquatable<PrimitiveTypeRef> Members

        public bool Equals(PrimitiveTypeRef other)
        {
            return other != null && other._code == _code;
        }

        #endregion
    }

    /// <summary>
    /// Represents a lambda function with known return type and parameters optionally.
    /// </summary>
    internal sealed class LambdaTypeRef : ITypeRef, IEquatable<LambdaTypeRef>
    {
        private readonly TypeRefMask _returnType;
        private readonly AST.Signature _signature;
        
        public LambdaTypeRef(TypeRefMask returnType, AST.Signature signature)
        {
            _returnType = returnType;
            _signature = signature;
        }

        #region ITypeRef Members

        /// <summary>
        /// Lambda function is of sub class of <c>Closure</c>.
        /// </summary>
        public QualifiedName QualifiedName
        {
            get { return NameUtils.SpecialNames.Closure; }
        }

        /// <summary>
        /// Lambda function is an object of type <c>Closure</c>.
        /// This handles case when lambda is used as object with methods <c>bindTo</c> or <c>__invoke</c>.
        /// </summary>
        public bool IsObject
        {
            get { return true; }
        }

        public bool IsArray
        {
            get { return false; }
        }

        public bool IsPrimitiveType
        {
            get { return false; }
        }

        public bool IsLambda
        {
            get { return true; }
        }

        public IEnumerable<object> Keys
        {
            get { throw new InvalidOperationException(); }
        }

        public TypeRefMask ElementType
        {
            get { throw new InvalidOperationException(); }
        }

        public TypeRefMask LambdaReturnType { get { return _returnType; } }

        public AST.Signature LambdaSignature { get { return _signature; } }

        public PhpTypeCode TypeCode { get { return PhpTypeCode.Object; } }

        public ITypeRef Transfer(TypeRefContext source, TypeRefContext target)
        {
            if (source == target || _returnType.IsVoid || _returnType.IsAnyType)
                return this;

            // note: there should be no circular dependency
            return new LambdaTypeRef(target.AddToContext(source, _returnType), _signature);
        }

        #endregion

        #region IEquatable<ITypeRef> Members

        public override int GetHashCode()
        {
            return _returnType.GetHashCode() ^ 0x777;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LambdaTypeRef);
        }

        public bool Equals(ITypeRef other)
        {
            return Equals(other as LambdaTypeRef);
        }

        #endregion

        #region IEquatable<LambdaTypeRef> Members

        public bool Equals(LambdaTypeRef other)
        {
            return other != null && other._returnType.Equals(_returnType) && Equals(other._signature, _signature);
        }

        private static bool Equals(AST.FormalParam[] params1, AST.FormalParam[] params2)
        {
            if (params1.Length == params2.Length)
            {
                for (int i = 0; i < params1.Length; i++)
                    if (params1[i].Name.Name != params2[i].Name.Name)
                        return false;

                return true;
            }

            return false;
        }

        #endregion
    }
}
