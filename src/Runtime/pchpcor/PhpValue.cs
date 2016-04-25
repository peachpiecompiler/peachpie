using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Represents a non-aliased PHP value.
    /// </summary>
    /// <remarks>
    /// Note, <c>default(PhpValue)</c> does not represent a valid state of the object.</remarks>
    [DebuggerDisplay("{DisplayString,nq}", Type= "{DebugTypeName,nq}")]
    [DebuggerTypeProxy(typeof(PhpValueDebugView))]
    [StructLayout(LayoutKind.Sequential)]   // {_type} has to be first for performance reasons.
    public partial struct PhpValue : IPhpConvertible, IEquatable<PhpValue> // <T>
    {
        #region DisplayString

        /// <summary>
        /// Debug textual representation of the value.
        /// </summary>
        internal string DisplayString => _type.DisplayString(ref this);

        #endregion

        #region DebuggerTypeProxy

        /// <summary>
        /// Gets php type name of the value.
        /// </summary>
        internal string DebugTypeName => PhpVariable.GetTypeName(this);

        [DebuggerDisplay("{_value.DisplayString,nq}", Type = "{_value.DebugTypeName,nq}")]
        internal sealed class PhpValueDebugView
        {
            readonly PhpValue _value;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object DebugValue
            {
                get
                {
                    switch (_value.TypeCode)
                    {
                        case PhpTypeCode.Alias: return _value.Alias;
                        case PhpTypeCode.Boolean: return _value.Boolean;
                        case PhpTypeCode.Double: return _value.Double;
                        case PhpTypeCode.Int32: return _value._value.Int;
                        case PhpTypeCode.Long: return _value.Long;
                        case PhpTypeCode.Object: return _value.Object;
                        case PhpTypeCode.PhpArray: return _value.Array;
                        case PhpTypeCode.String: return _value.String;
                        case PhpTypeCode.WritableString: return _value.WritableString.ToString();
                        default: return null;
                    }
                }
            }

            public PhpValueDebugView(PhpValue value)
            {
                _value = value;
            }
        }

        #endregion

        #region Nested struct: ValueField

        /// <summary>
        /// Union for possible value types.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct ValueField
        {
            [FieldOffset(0)]
            public long Long;
            [FieldOffset(0)]
            public double Double;
            [FieldOffset(0)]
            public int Int;
            [FieldOffset(0)]
            public bool Bool;
        }

        #endregion

        #region Nested struct: ObjectField

        /// <summary>
        /// Union for reference types.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct ObjectField
        {
            [FieldOffset(0)]
            public object Obj;
            [FieldOffset(0)]
            public string String;
            [FieldOffset(0)]
            public PhpArray Array;
            [FieldOffset(0)]
            public PhpAlias Alias;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The value type.
        /// </summary>
        TypeTable _type;

        /// <summary>
        /// A reference type container.
        /// </summary>
        ObjectField _obj;
        
        /// <summary>
        /// A value type container.
        /// </summary>
        ValueField _value;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the underlaying value type.
        /// </summary>
        public PhpTypeCode TypeCode => _type.Type;

        /// <summary>
        /// Gets value indicating whether the value is a <c>NULL</c>.
        /// </summary>
        public bool IsNull => _type.IsNull;

        /// <summary>
        /// Gets value indicating whether the value is set.
        /// </summary>
        public bool IsSet => (TypeCode != PhpTypeCode.Undefined);

        /// <summary>
        /// Gets value indicating whether the value is an alias containing another value.
        /// </summary>
        public bool IsAlias => (TypeCode == PhpTypeCode.Alias);

        /// <summary>
        /// Gets value indicating the value represents an object.
        /// </summary>
        public bool IsObject => (TypeCode == PhpTypeCode.Object);

        /// <summary>
        /// Gets value indicating the value represents PHP array.
        /// </summary>
        public bool IsArray => (TypeCode == PhpTypeCode.PhpArray);

        /// <summary>
        /// Gets the long field of the value.
        /// Does not perform a conversion, expects the value is of type long.
        /// </summary>
        public long Long { get { Debug.Assert(TypeCode == PhpTypeCode.Long); return _value.Long; } }

        /// <summary>
        /// Gets the double field of the value.
        /// Does not perform a conversion, expects the value is of type double.
        /// </summary>
        public double Double { get { Debug.Assert(TypeCode == PhpTypeCode.Double); return _value.Double; } }

        /// <summary>
        /// Gets the boolean field of the value.
        /// Does not perform a conversion, expects the value is of type boolean.
        /// </summary>
        public bool Boolean { get { Debug.Assert(TypeCode == PhpTypeCode.Boolean); return _value.Bool; } }

        /// <summary>
        /// Gets the object field of the value as string.
        /// Does not perform a conversion, expects the value is of type (readonly UTF16) string.
        /// </summary>
        public string String { get { Debug.Assert(_obj.Obj is string); return _obj.String; } }

        /// <summary>
        /// Gets the object field of the value as PHP writable string.
        /// Does not perform a conversion, expects the value is of type (writable UTF16 or single-byte) string.
        /// </summary>
        public PhpString WritableString { get { Debug.Assert(_obj.Obj is PhpString); return (PhpString)_obj.Obj; } }

        /// <summary>
        /// Gets underlaying reference object.
        /// </summary>
        public object Object { get { Debug.Assert(TypeCode == PhpTypeCode.Object); return _obj.Obj; } }

        // <summary>
        /// Gets underlaying array object.
        /// </summary>
        public PhpArray Array { get { Debug.Assert(TypeCode == PhpTypeCode.PhpArray); return _obj.Array; } }

        /// <summary>
        /// Gets underlaying alias object.
        /// </summary>
        public PhpAlias Alias { get { Debug.Assert(_obj.Obj is PhpAlias); return _obj.Alias; } }

        #endregion

        #region Operators

        public object ToClass(Context ctx) => _type.ToClass(ref this, ctx);

        public long ToLong() => _type.ToLong(ref this);

        public double ToDouble() => _type.ToDouble(ref this);

        public bool ToBoolean() => _type.ToBoolean(ref this);

        public Convert.NumberInfo ToNumber(out PhpNumber number) => _type.ToNumber(ref this, out number);

        public string ToString(Context ctx) => _type.ToString(ref this, ctx);

        public string ToStringOrThrow(Context ctx) => _type.ToStringOrThrow(ref this, ctx);

        public object EnsureObject(Context ctx) => _type.EnsureObject(ref this, ctx);

        public PhpArray EnsureArray() => _type.EnsureArray(ref this);

        public PhpAlias EnsureAlias() => _type.EnsureAlias(ref this);

        /// <summary>
        /// Creates a deep copy of PHP value.
        /// In case of scalars, the shallow copy is returned.
        /// In case of classes or aliases, the same reference is returned.
        /// In case of array or string, its copy is returned.
        /// </summary>
        public PhpValue DeepCopy() => _type.DeepCopy(ref this);

        /// <summary>
        /// Gets underlaying value or object as <see cref="System.Object"/>.
        /// </summary>
        /// <returns></returns>
        public object ToClr()
        {
            switch (this.TypeCode)
            {
                case PhpTypeCode.Boolean: return Boolean;
                case PhpTypeCode.Double: return Double;
                case PhpTypeCode.Int32: return _value.Int;
                case PhpTypeCode.Long: return Long;
                case PhpTypeCode.Object: return Object;
                case PhpTypeCode.PhpArray: return Array;
                case PhpTypeCode.String: return String;
                case PhpTypeCode.WritableString: return WritableString.ToString();
                case PhpTypeCode.Alias: return Alias.Value.ToClr();
                default:
                    throw new ArgumentException();
            }
        }

        #endregion

        #region IEquatable<PhpValue>

        public bool Equals(PhpValue other)
        {
            return _type == other._type && _obj.Obj == other._obj.Obj && _value.Long == other._value.Long;
        }

        #endregion

        #region Construction

        /// <summary>
        /// Singleton of PhpValue representing <c>void</c>.
        /// </summary>
        public static readonly PhpValue Void = new PhpValue(new VoidTable());

        /// <summary>
        /// Singleton of PhpValue representing <c>null</c>.
        /// </summary>
        public static readonly PhpValue Null = new PhpValue(new NullTable());

        private PhpValue(long value) : this()
        {
            _type = TypeTable.LongTable;
            _value.Long = value;
        }

        private PhpValue(double value) : this()
        {
            _type = TypeTable.DoubleTable;
            _value.Double = value;
        }

        private PhpValue(bool value) : this()
        {
            _type = TypeTable.BoolTable;
            _value.Bool = value;
        }

        private PhpValue(TypeTable type, object obj) : this()
        {
            _type = (obj != null) ? type : TypeTable.NullTable;
            _obj.Obj = obj;
        }

        private PhpValue(TypeTable type) : this()
        {
            _type = type;
            Debug.Assert(IsNull || !IsSet);
        }

        public static PhpValue Create(PhpNumber number)
            => (number.IsLong)
                 ? Create(number.Long)
                 : Create(number.Double);

        public static PhpValue Create(long value) => new PhpValue(value);

        public static PhpValue Create(double value) => new PhpValue(value);

        public static PhpValue Create(int value) => new PhpValue(value);

        public static PhpValue Create(bool value) => new PhpValue(value);

        public static PhpValue Create(string value) => new PhpValue(TypeTable.StringTable, value);

        public static PhpValue Create(PhpString value) => new PhpValue(TypeTable.WritableStringTable, value);

        public static PhpValue Create(PhpArray value) => new PhpValue(TypeTable.ArrayTable, value);

        public static PhpValue Create(PhpAlias value) => new PhpValue(TypeTable.AliasTable, value);

        public static PhpValue FromClass(object value)
        {
            Debug.Assert(!(value is int || value is long || value is bool || value is string || value is double || value is PhpAlias || value is PhpString || value is PhpArray));
            return new PhpValue(TypeTable.ClassTable, value);
        }

        /// <summary>
        /// Implicitly converts a CLR type to PHP type.
        /// </summary>
        public static PhpValue FromClr(object value)
        {
            // implicit conversion from CLR types to PHP types
            if (value != null)
            {
                if (value.GetType() == typeof(int)) return Create((int)value);
                if (value.GetType() == typeof(long)) return Create((long)value);
                if (value.GetType() == typeof(double)) return Create((double)value);
                if (value.GetType() == typeof(bool)) return Create((bool)value);
                if (value.GetType() == typeof(string)) return Create((string)value);
                if (value.GetType() == typeof(PhpString)) return Create((PhpString)value);
                if (value.GetType() == typeof(PhpAlias)) return Create((PhpAlias)value);
                if (value.GetType() == typeof(PhpArray)) return Create((PhpArray)value);
                if (value.GetType() == typeof(PhpValue)) return (PhpValue)value;
                
                //                
                return FromClass(value);
            }
            else
            {
                return Null;
            }
        }

        #endregion
    }
}
