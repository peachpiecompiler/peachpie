using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core
{
    [DebuggerNonUserCode, DebuggerStepThrough]
    public static class Operators
    {
        #region Numeric

        /// <summary>
        /// Bit mask corresponding to the sign in <see cref="long"/> value.
        /// </summary>
        internal const long LONG_SIGN_MASK = (1L << (8 * sizeof(long) - 1));

        /// <summary>
        /// Performs bitwise and operation.
        /// </summary>
        internal static PhpValue BitAnd(in PhpValue x, in PhpValue y)
        {
            var bx = x.ToBytesOrNull();
            if (bx != null)
            {
                var by = y.ToBytesOrNull();
                if (by != null)
                {
                    return PhpValue.Create(BitAnd(bx, by));
                }
            }

            //
            return PhpValue.Create(x.ToLong() & y.ToLong());
        }

        /// <summary>
        /// Performs bitwise or operation.
        /// </summary>
        internal static PhpValue BitOr(in PhpValue x, in PhpValue y)
        {
            var bx = x.ToBytesOrNull();
            if (bx != null)
            {
                var by = y.ToBytesOrNull();
                if (by != null)
                {
                    return PhpValue.Create(BitOr(bx, by));
                }
            }

            //
            return PhpValue.Create(x.ToLong() | y.ToLong());
        }

        /// <summary>
        /// Performs exclusive or operation.
        /// </summary>
        internal static PhpValue BitXor(in PhpValue x, in PhpValue y)
        {
            var bx = x.ToBytesOrNull();
            if (bx != null)
            {
                var by = y.ToBytesOrNull();
                if (by != null)
                {
                    return PhpValue.Create(BitXor(bx, by));
                }
            }

            //
            return PhpValue.Create(x.ToLong() ^ y.ToLong());
        }

        static byte[] BitAnd(byte[] bx, byte[] by)
        {
            int length = Math.Min(bx.Length, by.Length);
            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] result = new byte[length];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (byte)(bx[i] & by[i]);
            }

            return result;
        }

        static byte[] BitOr(byte[] bx, byte[] by)
        {
            if (bx.Length == 0 && by.Length == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] result, or;
            if (bx.Length > by.Length)
            {
                result = (byte[])bx.Clone();
                or = by;
            }
            else
            {
                result = (byte[])by.Clone();
                or = bx;
            }

            for (int i = 0; i < or.Length; i++)
            {
                result[i] |= or[i];
            }

            return result;
        }

        static byte[] BitXor(byte[] bx, byte[] by)
        {
            int length = Math.Min(bx.Length, by.Length);
            byte[] result = new byte[length];

            return BitXor(result, bx, by);
        }

        /// <summary>
        /// Performs specified binary operation on arrays of bytes.
        /// </summary>
        /// <param name="result">An array where to store the result. Data previously stored here will be overwritten.</param>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand</param>
        /// <returns>The reference to the the <paramref name="result"/> array.</returns>
        static byte[] BitXor(byte[]/*!*/ result, byte[]/*!*/ x, byte[]/*!*/ y)
        {
            Debug.Assert(result != null && x != null && y != null && result.Length <= x.Length && result.Length <= y.Length);

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = unchecked((byte)(x[i] ^ y[i]));
            }

            // remaining bytes are ignored //
            return result;
        }

        /// <summary>
        /// Performs bitwise negation.
        /// </summary>
        internal static PhpValue BitNot(in PhpValue x)
        {
            switch (x.TypeCode)
            {
                case PhpTypeCode.Long: return PhpValue.Create(~x.Long);

                case PhpTypeCode.Alias: return BitNot(in x.Alias.Value);

                case PhpTypeCode.String:
                case PhpTypeCode.MutableString:
                    throw new NotImplementedException();    // bitwise negation of each character in string

                case PhpTypeCode.Object:
                    if (x.Object == null)
                    {
                        return PhpValue.Null;
                    }
                    goto default;

                default:
                    // TODO: Err UnsupportedOperandTypes
                    return PhpValue.Null;
            }
        }

        /// <summary>
        /// Performs division according to PHP semantics.
        /// </summary>
        /// <remarks>The division operator ("/") returns a float value unless the two operands are integers
        /// (or strings that get converted to integers) and the numbers are evenly divisible,
        /// in which case an integer value will be returned.</remarks>
        internal static PhpNumber Div(in PhpValue x, in PhpValue y)
        {
            var info = x.ToNumber(out var nx) | y.ToNumber(out var ny);

            if ((info & Convert.NumberInfo.IsPhpArray) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return PhpNumber.Create(0.0);
                throw new NotImplementedException();     // PhpException
            }

            // TODO: // division by zero:
            //if (y == 0)
            //{
            //    PhpException.Throw(PhpError.Warning, CoreResources.GetString("division_by_zero"));
            //    return false;
            //}

            return nx / ny;
        }

        #endregion

        #region Assignment

        /// <summary>
        /// Assigns a PHP value by value according to the PHP semantics.
        /// </summary>
        /// <param name="target">Target of the assignment.</param>
        /// <param name="value">Value to be assigned.</param>
        public static void SetValue(ref PhpValue target, PhpValue value)
        {
            Debug.Assert(!value.IsAlias);
            if (target.Object is PhpAlias alias)
            {
                alias.Value = value;
            }
            else
            {
                target = value;
            }
        }

        /// <summary>
        /// Assigns a PHP value to an aliased place.
        /// </summary>
        /// <param name="target">Target of the assignment.</param>
        /// <param name="value">Value to be assigned.</param>
        public static void SetValue(PhpAlias/*!*/target, PhpValue value)
        {
            Debug.Assert(target != null);
            Debug.Assert(!value.IsAlias);
            target.Value = value;
        }

        #endregion

        #region Ensure

        /// <summary>
        /// Ensures given variable is not <c>null</c>.
        /// </summary>
        public static object EnsureObject(ref object obj) => obj ?? (obj = new stdClass());

        /// <summary>
        /// Ensures given variable is not <c>null</c>.
        /// </summary>
        public static PhpArray EnsureArray(ref PhpArray arr) => arr ?? (arr = new PhpArray());

        /// <summary>
        /// Ensures given variable is not <c>null</c>.
        /// </summary>
        public static IPhpArray EnsureArray(ref IPhpArray arr) => arr ?? (arr = new PhpArray());

        /// <summary>
        /// Ensures the value is <see cref="PhpString"/> and gets mutable access to the value (non-shared).
        /// </summary>
        /// <returns>Object on which edit operations can be performed. Cannot be <c>null</c>.</returns>
        public static PhpString.Blob EnsureWritableString(ref PhpValue value)
        {
            PhpString.Blob blob;

            switch (value.TypeCode)
            {
                case PhpTypeCode.MutableString:
                    if ((blob = value.MutableStringBlob).IsShared)
                    {
                        value = new PhpValue(blob = value.MutableStringBlob.ReleaseOne());
                    }
                    break;

                case PhpTypeCode.Null:
                    blob = new PhpString.Blob();
                    value = new PhpValue(blob);
                    break;

                case PhpTypeCode.String:
                    blob = new PhpString.Blob(value.String);
                    value = new PhpValue(blob);
                    break;

                case PhpTypeCode.Alias:
                    blob = EnsureWritableString(ref value.Alias.Value);
                    break;

                default:
                    blob = new PhpString.Blob(value.ToStringUtf8());
                    value = new PhpValue(blob);
                    break;
            }

            return blob;
        }

        #endregion

        #region IsSet, IsEmpty

        /// <summary>
        /// Implementation of PHP <c>isset</c> operator.
        /// </summary>
        /// <remarks>Value (eventualy dereferenced value) is not <c>NULL</c>.</remarks>
        public static bool IsSet(PhpValue value) => !value.IsNull;

        /// <summary>
        /// Implements <c>empty</c> operator.
        /// </summary>
        public static bool IsEmpty(PhpValue value) => value.IsEmpty;

        /// <summary>
        /// Implements <c>empty</c> operator on objects.
        /// </summary>
        public static bool IsEmpty(object value) => ReferenceEquals(value, null);

        #endregion

        #region Array Access

        /// <summary>
        /// Provides <see cref="IPhpArray"/> interface for <see cref="ArrayAccess"/> instance.
        /// </summary>
        sealed class ArrayAccessAsPhpArray : IPhpArray
        {
            readonly ArrayAccess _array;

            public ArrayAccessAsPhpArray(ArrayAccess array)
            {
                Debug.Assert(array != null);
                _array = array;
            }

            public int Count
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public void AddValue(PhpValue value) => SetItemValue(PhpValue.Null, value);

            public PhpAlias EnsureItemAlias(IntStringKey key)
            {
                var item = _array.offsetGet(key);
                return PhpValue.EnsureAlias(ref item);
            }

            public IPhpArray EnsureItemArray(IntStringKey key)
            {
                var item = _array.offsetGet(key);
                return PhpValue.EnsureArray(ref item);
            }

            public object EnsureItemObject(IntStringKey key)
            {
                var item = _array.offsetGet(key);
                return PhpValue.EnsureObject(ref item);
            }

            public PhpValue GetItemValue(IntStringKey key) => _array.offsetGet(PhpValue.Create(key));

            public PhpValue GetItemValue(PhpValue index) => _array.offsetGet(index);

            public void RemoveKey(IntStringKey key) => _array.offsetUnset(PhpValue.Create(key));

            public void RemoveKey(PhpValue index) => _array.offsetUnset(index);

            public void SetItemAlias(IntStringKey key, PhpAlias alias) => _array.offsetSet(PhpValue.Create(key), PhpValue.Create(alias));

            public void SetItemAlias(PhpValue index, PhpAlias alias) => _array.offsetSet(index, PhpValue.Create(alias));

            public void SetItemValue(IntStringKey key, PhpValue value) => _array.offsetSet(PhpValue.Create(key), value);

            public void SetItemValue(PhpValue index, PhpValue value) => _array.offsetSet(index, value);
        }

        sealed class ListAsPhpArray : IPhpArray
        {
            readonly IList _array;

            public ListAsPhpArray(IList array)
            {
                Debug.Assert(array != null);
                _array = array;
            }

            object ToObject(PhpValue value) => value.ToClr();    // TODO, type conversion

            public int Count => _array.Count;

            public void AddValue(PhpValue value)
            {
                _array.Add(ToObject(value));
            }

            public PhpAlias EnsureItemAlias(IntStringKey key)
            {
                var item = GetItemValue(key);
                return PhpValue.EnsureAlias(ref item);
            }

            public IPhpArray EnsureItemArray(IntStringKey key)
            {
                var item = GetItemValue(key);
                return PhpValue.EnsureArray(ref item);
            }

            public object EnsureItemObject(IntStringKey key)
            {
                var item = GetItemValue(key);
                return PhpValue.EnsureObject(ref item);
            }

            public PhpValue GetItemValue(IntStringKey key)
            {
                if (key.IsInteger && Utilities.NumberUtils.IsInt32(key.Integer))
                {
                    return PhpValue.FromClr(_array[unchecked((int)key.Integer)]);
                }
                else
                {
                    throw new ArgumentException(nameof(key));
                }
            }

            public PhpValue GetItemValue(PhpValue index) => GetItemValue(index.ToIntStringKey());

            public void RemoveKey(IntStringKey key)
            {
                if (key.IsInteger && Utilities.NumberUtils.IsInt32(key.Integer))
                {
                    _array.RemoveAt(unchecked((int)key.Integer));
                }
                else
                {
                    throw new ArgumentException(nameof(key));
                }
            }

            public void RemoveKey(PhpValue index) => RemoveKey(index.ToIntStringKey());

            public void SetItemAlias(IntStringKey key, PhpAlias alias)
            {
                if (key.IsInteger && Utilities.NumberUtils.IsInt32(key.Integer))
                {
                    _array[unchecked((int)key.Integer)] = ToObject(alias.Value);
                }
                else
                {
                    throw new ArgumentException(nameof(key));
                }
            }

            public void SetItemAlias(PhpValue index, PhpAlias alias) => SetItemAlias(index.ToIntStringKey(), alias);

            public void SetItemValue(IntStringKey key, PhpValue value)
            {
                if (key.IsInteger && Utilities.NumberUtils.IsInt32(key.Integer))
                {
                    _array[unchecked((int)key.Integer)] = ToObject(value);
                }
                else
                {
                    throw new ArgumentException(nameof(key));
                }
            }

            public void SetItemValue(PhpValue index, PhpValue value) => SetItemValue(index.ToIntStringKey(), value);
        }

        /// <summary>
        /// Helper class representing array access for classes with CLR "get_Item" / "set_Item" indexer method.
        /// </summary>
        sealed class GetSetItemAsPhpArray : IPhpArray
        {
            readonly object/*!*/_instance;

            public GetSetItemAsPhpArray(object/*!*/instance)
            {
                _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            }

            public int Count => throw new NotSupportedException();

            public void AddValue(PhpValue value) => throw new NotSupportedException();

            public PhpAlias EnsureItemAlias(IntStringKey key)
            {
                var item = GetItemValue(key);
                return PhpValue.EnsureAlias(ref item);
            }

            public IPhpArray EnsureItemArray(IntStringKey key)
            {
                var item = GetItemValue(key);
                return PhpValue.EnsureArray(ref item);
            }

            public object EnsureItemObject(IntStringKey key)
            {
                var item = GetItemValue(key);
                return PhpValue.EnsureObject(ref item);
            }

            public PhpValue GetItemValue(IntStringKey key) => GetItemValue((PhpValue)key);

            public PhpValue GetItemValue(PhpValue index)
            {
                var getter = _instance.GetPhpTypeInfo().RuntimeMethods[TypeMethods.MagicMethods.get_item];
                if (getter != null)
                {
                    // TODO: Context is null, should no be used but ...
                    return getter.Invoke(null, _instance, index);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            public void RemoveKey(IntStringKey key) => RemoveKey((PhpValue)key);

            public void RemoveKey(PhpValue index) => throw new NotSupportedException();

            public void SetItemAlias(IntStringKey key, PhpAlias alias) => SetItemAlias((PhpValue)key, alias);

            public void SetItemAlias(PhpValue index, PhpAlias alias)
            {
                throw new NotImplementedException();
            }

            public void SetItemValue(IntStringKey key, PhpValue value) => SetItemValue((PhpValue)key, value);

            public void SetItemValue(PhpValue index, PhpValue value)
            {
                var setter = _instance.GetPhpTypeInfo().RuntimeMethods[TypeMethods.MagicMethods.set_item];
                if (setter != null)
                {
                    // TODO: Context is null, should no be used but ...
                    setter.Invoke(null, _instance, index, value);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public static IPhpArray EnsureArray(ArrayAccess obj)
        {
            Debug.Assert(obj != null);
            return new ArrayAccessAsPhpArray(obj);
        }

        public static IPhpArray EnsureArray(object obj)
        {
            // ArrayAccess
            if (obj is ArrayAccess) return EnsureArray((ArrayAccess)obj);

            // IPhpArray
            if (obj is IPhpArray) return (IPhpArray)obj;

            // IList
            if (obj is IList) return new ListAsPhpArray((IList)obj);

            // TODO: IDictionary

            // get_Item
            if (obj.GetPhpTypeInfo().RuntimeMethods[TypeMethods.MagicMethods.get_item] != null)
            {
                return new GetSetItemAsPhpArray(obj);
            }

            // Fatal error: Uncaught Error: Cannot use object of type {0} as array
            PhpException.Throw(PhpError.Error, Resources.ErrResources.object_used_as_array, obj.GetPhpTypeInfo().Name);
            throw new ArgumentException(nameof(obj));
        }

        /// <summary>
        /// Gets <see cref="IPhpArray"/> instance providing access to the value with array operators.
        /// Returns <c>null</c> if underlaying value does provide array access.
        /// </summary>
        public static IPhpArray GetArrayAccess(ref PhpValue value) => value.TypeCode switch
        {
            // TODO // CONSIDER: what is this?

            PhpTypeCode.PhpArray => value.Array,
            PhpTypeCode.String => PhpValue.EnsureArray(ref value),
            PhpTypeCode.MutableString => value.MutableStringBlob,
            PhpTypeCode.Object => EnsureArray(value.Object),
            PhpTypeCode.Alias => GetArrayAccess(ref value.Alias.Value),
            _ => null,
        };

        /// <summary>
        /// Gets <see cref="IPhpArray"/> to be used as R-value of <c>list</c> expression.
        /// </summary>
        public static IPhpArray GetListAccess(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.PhpArray: return value.Array;
                case PhpTypeCode.Object: return EnsureArray(value.Object);
                case PhpTypeCode.Alias: return GetListAccess(value.Alias.Value);
                default:
                    // TODO: some kind of debug log would be nice, PHP does not do that
                    return PhpArray.Empty;
            }
        }

        /// <summary>
        /// Gets <see cref="IPhpArray"/> to be used as R-value of <c>list</c> expression.
        /// </summary>
        public static IPhpArray GetListAccess(object value) => EnsureArray(value);

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="string"/>.
        /// </summary>
        /// <param name="value">String to be accessed as array.</param>
        /// <param name="index">Index.</param>
        /// <returns>Character on index or empty string if index is our of range.</returns>
        public static string GetItemValue(string value, long index)
        {
            return (value != null && index >= 0 && index < value.Length)
                ? value[unchecked((int)index)].ToString()
                : string.Empty; // TODO: quiet ?
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="string"/>.
        /// </summary>
        public static string GetItemValue(string value, IntStringKey key)
        {
            var index = key.IsInteger
                ? key.Integer
                : Convert.StringToLongInteger(key.String);

            return GetItemValue(value, index);
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="string"/> with <c>isset</c> semantics.
        /// </summary>
        public static string GetItemValueOrNull(string value, IntStringKey key)
        {
            var index = key.IsInteger
                ? key.Integer
                : Convert.StringToLongInteger(key.String);

            return (value != null && index >= 0 && index < value.Length)
                ? value[unchecked((int)index)].ToString()
                : null;
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="string"/>.
        /// </summary>
        public static string GetItemValue(string value, PhpValue index, bool quiet)
        {
            if (value != null && index.TryToIntStringKey(out var key))
            {
                long i;

                if (key.IsInteger)
                {
                    i = key.Integer;
                }
                else
                {
                    if (quiet) return null;

                    i = Convert.StringToLongInteger(key.String);
                }

                if (i >= 0 && i < value.Length)
                {
                    return value[(int)i].ToString();
                }
            }

            //
            if (quiet)
            {
                // used by isset() and empty()
                return null;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.ErrResources.illegal_string_offset, index.ToString());
                return string.Empty;
            }
        }

        /// <summary>
        /// Shortcut for calling <c>ord($s[$i])</c> on a <see cref="PhpValue"/>
        /// without any extra allocation.
        /// </summary>
        public static long GetItemOrdValue(PhpValue value, long index)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.String:
                    return GetItemOrdValue(value.String, index);

                case PhpTypeCode.MutableString:
                    return GetItemOrdValue(value.MutableString, index);

                case PhpTypeCode.Alias:
                    return GetItemOrdValue(value.Alias.Value, index);

                default:
                    var item = value.GetArrayItem(index);
                    if (item.IsMutableString(out var itemPhpString))
                    {
                        return itemPhpString.IsEmpty ? 0 : itemPhpString[0];
                    }
                    else
                    {
                        var str = item.ToStringUtf8();
                        return string.IsNullOrEmpty(str) ? 0 : str[0];
                    }
            }
        }

        /// <summary>
        /// Shortcut for calling <c>ord($s[$i])</c> on a <see cref="string"/>
        /// without any extra allocation.
        /// </summary>
        public static long GetItemOrdValue(string value, long index)
        {
            if (value != null && index >= 0 && index < value.Length)
            {
                return value[(int)index];
            }

            //
            PhpException.Throw(PhpError.Warning, Resources.ErrResources.illegal_string_offset, index.ToString());
            return 0;
        }

        /// <summary>
        /// Shortcut for calling <c>ord($s[$i])</c> on a <see cref="PhpString"/>
        /// without any extra allocation.
        /// </summary>
        public static long GetItemOrdValue(PhpString value, long index)
        {
            if (index >= 0 && index < value.Length)
            {
                return value[(int)index];
            }

            PhpException.Throw(PhpError.Warning, Resources.ErrResources.illegal_string_offset, index.ToString());
            return 0;
        }

        public static object EnsureItemObject(this IPhpArray array, PhpValue index)
        {
            if (index.TryToIntStringKey(out var key))
            {
                return array.EnsureItemObject(key);
            }
            else
            {
                throw PhpException.TypeErrorException(Resources.ErrResources.illegal_offset_type);
            }
        }

        public static IPhpArray EnsureItemArray(this IPhpArray array, PhpValue index)
        {
            if (index.TryToIntStringKey(out var key))
            {
                return array.EnsureItemArray(key);
            }
            else
            {
                throw PhpException.TypeErrorException(Resources.ErrResources.illegal_offset_type);
            }
        }

        public static PhpAlias EnsureItemAlias(this IPhpArray array, PhpValue index, bool quiet)
        {
            if (index.TryToIntStringKey(out var key))
            {
                return array.EnsureItemAlias(key);
            }
            else
            {
                if (!quiet)
                {
                    PhpException.IllegalOffsetType();
                }

                return new PhpAlias(PhpValue.Null);
            }
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="PhpValue"/>.
        /// </summary>
        public static PhpValue GetItemValue(PhpValue value, PhpValue index, bool quiet = false)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.String:
                    var item = Operators.GetItemValue(value.String, index, quiet);
                    if (quiet && string.IsNullOrEmpty(item))
                    {
                        return PhpValue.Null;
                    }
                    return item;

                case PhpTypeCode.MutableString:
                    return ((IPhpArray)value.MutableStringBlob).GetItemValue(index); // quiet);

                case PhpTypeCode.PhpArray:
                    return value.Array.GetItemValue(index); // , quiet);

                case PhpTypeCode.Object:
                    return Operators.GetItemValue(value.Object, index, quiet);

                case PhpTypeCode.Alias:
                    return value.Alias.Value.GetArrayItem(index, quiet);

                default:
                    return PhpValue.Null;
            }
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="PhpValue"/>.
        /// </summary>
        public static PhpValue GetItemValue(object obj, PhpValue index, bool quiet = false)
        {
            // IPhpArray.GetItemValue
            if (obj is IPhpArray arr)
            {
                return arr.GetItemValue(index); // , quiet);
            }

            // ArrayAccess.offsetGet()
            if (obj is ArrayAccess arracces)
            {
                return arracces.offsetGet(index);
            }

            // IList[]
            if (obj is IList list)
            {
                if (index.TryToIntStringKey(out var key) && key.IsInteger)
                {
                    if (key.Integer >= 0 && key.Integer < list.Count)
                    {
                        return PhpValue.FromClr(list[(int)key.Integer]);
                    }
                    else if (!quiet)
                    {
                        PhpException.UndefinedOffset(key);
                    }
                }
                else if (!quiet)
                {
                    PhpException.IllegalOffsetType();
                }

                return PhpValue.Null;
            }

            //
            if (!quiet)
            {
                PhpException.Throw(
                    PhpError.Error,
                    Resources.ErrResources.object_used_as_array, obj != null ? obj.GetPhpTypeInfo().Name : PhpVariable.TypeNameNull);
            }

            //
            return PhpValue.Null;
        }

        public static bool TryGetItemValue(this PhpArray value, string index, out PhpValue item)
        {
            if (value != null && value.TryGetValue(index, out item) && IsSet(item))
            {
                return true;
            }
            else
            {
                item = default;
                return false;
            }
        }

        public static bool TryGetItemValue(this PhpArray value, PhpValue index, out PhpValue item)
        {
            if (value != null && index.TryToIntStringKey(out var key) && value.TryGetValue(key, out item) && IsSet(item))
            {
                return true;
            }
            else
            {
                item = default;
                return false;
            }
        }

        public static bool TryGetItemValue(this PhpValue value, PhpValue index, out PhpValue item)
        {
            if (value.IsPhpArray(out var array))
            {
                // Specialized call for array
                return TryGetItemValue(array, index, out item);
            }
            else
            {
                // Otherwise use the original semantics of isset($x[$y]) ? $x[$y] : ...;
                if (offsetExists(value, index))
                {
                    item = GetItemValue(value, index);
                    return true;
                }
                else
                {
                    item = default;
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static PhpAlias EnsureItemAlias_Old(PhpValue value, PhpValue index, bool quiet = false)
        {
            Debug.WriteLineIf(value.IsNull, "NULL value won't be changed to array!");

            return EnsureItemAlias(ref value, index, quiet);
        }

        /// <summary>
        /// Implements <c>&amp;[]</c> operator on <see cref="PhpValue"/>.
        /// Ensures the value is an array and item at given <paramref name="index"/> is an alias.
        /// </summary>
        public static PhpAlias EnsureItemAlias(ref PhpValue value, PhpValue index, bool quiet = false)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Null:
                    // TODO: Err: Warning: Creating default object from empty value
                    var arr = new PhpArray();
                    value = PhpValue.Create(arr);
                    return arr.EnsureItemAlias(index, quiet);

                case PhpTypeCode.PhpArray:
                    return value.Array.EnsureItemAlias(index, quiet);

                case PhpTypeCode.String:
                    throw new NotImplementedException();

                case PhpTypeCode.MutableString:
                    throw new NotImplementedException();

                case PhpTypeCode.Object:
                    if (value.Object is IPhpArray array)
                    {
                        return EnsureItemAlias(array, index, quiet);
                    }

                    if (!quiet) // NOTE: PHP does not report this error (?)
                    {
                        PhpException.Throw(PhpError.Error, Resources.ErrResources.object_used_as_array, value.Object.GetPhpTypeInfo().Name);
                    }

                    break;

                case PhpTypeCode.Alias:
                    return EnsureItemAlias(ref value.Alias.Value, index, quiet);
            }

            // TODO: Warning
            return new PhpAlias(PhpValue.Null);
        }

        public static bool offsetExists(this PhpArray value, long index) =>
            value != null &&
            value.TryGetValue((int)index, out var x) &&
            IsSet(x);

        public static bool offsetExists(this PhpArray value, string index) =>
            value != null &&
            value.TryGetValue(index, out var x) &&
            IsSet(x);

        public static bool offsetExists(this PhpArray value, PhpValue index) =>
            value != null &&
            index.TryToIntStringKey(out var key) &&
            value.TryGetValue(key, out var x) &&
            IsSet(x);

        public static bool offsetExists(this string value, PhpValue index)
        {
            return index.TryToIntStringKey(out var key) && key.IsInteger && offsetExists(value, key.Integer);
        }

        public static bool offsetExists(this string value, long index)
        {
            return value != null && index >= 0 && index < value.Length;
        }

        public static bool offsetExists(this PhpString value, long index)
        {
            return index >= 0 && index < value.Length;
        }

        public static bool offsetExists(object obj, PhpValue index)
        {
            if (obj is ArrayAccess arrrayAccess)
            {
                return arrrayAccess.offsetExists(index);
            }
            else if (obj is IPhpArray arr)
            {
                return IsSet(arr.GetItemValue(index));
            }
            else if (obj is IList list)
            {
                return index.TryToIntStringKey(out var key) && key.IsInteger && key.Integer >= 0 && key.Integer < list.Count;
            }
            // TODO: IDictionary

            return false;
        }

        public static bool offsetExists(PhpAlias alias, PhpValue index) => offsetExists(alias.Value, index);

        public static bool offsetExists(this PhpValue value, PhpValue index)
        {
            if (value.Object is PhpArray array)
            {
                return offsetExists(array, index);
            }
            else if (value.Object is string str)
            {
                return offsetExists(str, index);
            }
            else if (value.Object is PhpString.Blob blob)
            {
                return index.TryToIntStringKey(out var key) && key.IsInteger && key.Integer >= 0 && key.Integer < blob.Length;
            }
            else if (value.Object is PhpAlias alias)
            {
                return offsetExists(alias.Value, index);
            }
            else if (value.Object != null)
            {
                // class instance
                return offsetExists(value.Object, index);
            }

            // scalar or NULL
            return false;
        }

        #endregion

        #region Object

        public static bool PropertyExists(RuntimeTypeHandle caller, object instance, PhpValue prop)
        {
            var tinfo = instance.GetPhpTypeInfo();

            // 1. instance property

            // 2. runtime property

            // 3. __isset

            // false

            throw new NotImplementedException();
        }

        public static PhpValue PropertyGetValue(RuntimeTypeHandle caller, object instance, PhpValue propertyName)
        {
            var tinfo = instance.GetPhpTypeInfo();

            // 1. instance property

            // 2. runtime property

            // 3. __get

            // error

            throw new NotImplementedException();
        }

        public static void PropertySetValue(RuntimeTypeHandle caller, object instance, PhpValue prop, PhpValue value)
        {
            var tinfo = instance.GetPhpTypeInfo();

            // 1. instance property

            // 2. overwrite runtime property

            // 3. __set ?? runtime property

            // error

            throw new NotImplementedException();
        }

        public static void PropertyUnset(RuntimeTypeHandle caller, object instance, PhpValue prop)
        {
            var tinfo = instance.GetPhpTypeInfo();

            // 1. instance property

            // 2. unset runtime property

            // 3. __unset

            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> from a string or an object instance.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="object">String or object. Other value types cause an exception.</param>
        /// <returns>Corresponding <see cref="PhpTypeInfo"/> descriptor. Cannot be <c>null</c>.</returns>
        public static PhpTypeInfo TypeNameOrObjectToType(Context ctx, PhpValue @object)
        {
            object obj;
            string str;

            if ((obj = (@object.AsObject())) != null)
            {
                return obj.GetType().GetPhpTypeInfo();
            }
            else if ((str = PhpVariable.AsString(@object)) != null)
            {
                return ctx.GetDeclaredType(str, true);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Resolves the runtime property by looking into runtime properties and eventually invoking the <c>__get</c> magic method.
        /// </summary>
        public static PhpValue RuntimePropertyGetValue(Context/*!*/ctx, object/*!*/instance, string propertyName)
        {
            return RuntimePropertyGetValue(ctx, instance.GetPhpTypeInfo(), instance, propertyName);
        }

        /// <summary>
        /// Resolves the runtime property by looking into runtime properties and eventually invoking the <c>__get</c> magic method.
        /// </summary>
        public static PhpValue RuntimePropertyGetValue(Context/*!*/ctx, PhpTypeInfo/*!*/type, object/*!*/instance, string propertyName)
        {
            var runtimeFields = type.GetRuntimeFields(instance);
            if (runtimeFields != null && runtimeFields.TryGetValue(propertyName, out var value))
            {
                return value;
            }

            var __get = type.RuntimeMethods[TypeMethods.MagicMethods.__get];
            if (__get != null)
            {
                // NOTE: magic methods must have public visibility, therefore the visibility check is unnecessary

                // int subkey1 = access.Write() ? 1 : access.Unset() ? 2 : access.Isset() ? 3 : 4;
                int subkey = propertyName.GetHashCode() ^ (1 << 4/*subkey1*/);

                using (var token = new Context.RecursionCheckToken(ctx, instance, subkey))
                {
                    if (!token.IsInRecursion)
                    {
                        return __get.Invoke(ctx, instance, propertyName);
                    }
                }
            }

            //
            PhpException.UndefinedProperty(type.Name, propertyName);
            return PhpValue.Null;
        }

        #endregion

        #region self, parent

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of self.
        /// Throws in case of self being used out of class context.
        /// </summary>
        public static PhpTypeInfo GetSelf(RuntimeTypeHandle self)
        {
            if (self.Equals(default(RuntimeTypeHandle)))
            {
                PhpException.ThrowSelfOutOfClass();
            }

            //
            return self.GetPhpTypeInfo();
        }

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of self or <c>null</c>.
        /// </summary>
        public static PhpTypeInfo GetSelfOrNull(RuntimeTypeHandle self) => self.GetPhpTypeInfo();

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of parent.
        /// Throws in case of parent being used out of class context or within a parentless class.
        /// </summary>
        public static PhpTypeInfo GetParent(RuntimeTypeHandle self) => GetParent(self.GetPhpTypeInfo());

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of parent.
        /// Throws in case of parent being used out of class context or within a parentless class.
        /// </summary>
        public static PhpTypeInfo GetParent(PhpTypeInfo self)
        {
            if (self == null)
            {
                PhpException.Throw(PhpError.Error, Resources.ErrResources.parent_used_out_of_class);
            }
            else
            {
                var t = self.BaseType;
                if (t != null)
                {
                    return t;
                }
                else
                {
                    PhpException.Throw(PhpError.Error, Resources.ErrResources.parent_accessed_in_parentless_class);
                }
            }

            //
            throw new ArgumentException(nameof(self));
        }

        #endregion

        #region GetForeachEnumerator

        /// <summary>
        /// Provides the <see cref="IPhpEnumerator"/> interface by wrapping a user-implemeted <see cref="Iterator"/>.
        /// </summary>
        /// <remarks>
        /// Instances of this class are iterated when <c>foreach</c> is used on object of a class
        /// that implements <see cref="Iterator"/> or <see cref="IteratorAggregate"/>.
        /// </remarks>
        [DebuggerNonUserCode, DebuggerStepThrough]
        private sealed class PhpIteratorEnumerator : IPhpEnumerator
        {
            readonly Iterator _iterator;
            bool _hasmoved;

            public PhpIteratorEnumerator(Iterator iterator)
            {
                Debug.Assert(iterator != null);
                _iterator = iterator;
                Reset();
            }

            public bool AtEnd
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public KeyValuePair<PhpValue, PhpValue> Current => new KeyValuePair<PhpValue, PhpValue>(CurrentKey, CurrentValue);

            public PhpValue CurrentKey => _iterator.key().DeepCopy();

            public PhpValue CurrentValue => _iterator.current().DeepCopy();

            public PhpAlias CurrentValueAliased
            {
                get
                {
                    var value = _iterator.current();
                    return PhpValue.EnsureAlias(ref value);
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveFirst()
            {
                Reset();
                return _iterator.valid();
            }

            public bool MoveLast()
            {
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                if (_hasmoved)
                {
                    _iterator.next();
                }
                else
                {
                    _hasmoved = true;
                }

                return _iterator.valid();
            }

            public bool MovePrevious()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                _hasmoved = false;
                _iterator.rewind();
            }
        }

        /// <summary>
        /// Provides <see cref="IPhpEnumerator"/> implementation enumerating class instance fields and runtime fields.
        /// </summary>
        private sealed class PhpFieldsEnumerator : IPhpEnumerator
        {
            readonly IEnumerator<KeyValuePair<IntStringKey, PhpValue>> _enumerator;
            bool _valid;

            public PhpFieldsEnumerator(object obj, RuntimeTypeHandle caller)
            {
                Debug.Assert(obj != null);
                _enumerator = TypeMembersUtils.EnumerateVisibleInstanceFields(obj, caller).GetEnumerator();
                _valid = true;
            }

            public bool AtEnd => !_valid;

            public KeyValuePair<PhpValue, PhpValue> Current
            {
                get
                {
                    var current = _enumerator.Current;
                    return new KeyValuePair<PhpValue, PhpValue>(PhpValue.Create(current.Key), current.Value);
                }
            }

            public PhpValue CurrentKey => PhpValue.Create(_enumerator.Current.Key);

            public PhpValue CurrentValue => _enumerator.Current.Value.GetValue().DeepCopy();

            public PhpAlias CurrentValueAliased
            {
                get
                {
                    var value = _enumerator.Current.Value;
                    return PhpValue.EnsureAlias(ref value);
                }
            }

            object IEnumerator.Current => _enumerator.Current;

            public void Dispose() => _enumerator.Dispose();

            public bool MoveFirst()
            {
                Reset();
                return MoveNext();
            }

            public bool MoveLast()
            {
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                return (_valid = _enumerator.MoveNext());
            }

            public bool MovePrevious()
            {
                throw new NotSupportedException();
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Implements empty enumeration.
        /// </summary>
        private sealed class PhpEmptyEnumerator : IPhpEnumerator
        {
            public static readonly IPhpEnumerator Instance = new PhpEmptyEnumerator();

            private PhpEmptyEnumerator() { }

            public bool AtEnd => false;

            public KeyValuePair<PhpValue, PhpValue> Current => default(KeyValuePair<PhpValue, PhpValue>);

            public PhpValue CurrentKey => PhpValue.Null;

            public PhpValue CurrentValue => PhpValue.Null;

            public PhpAlias CurrentValueAliased => new PhpAlias(PhpValue.Null);

            object IEnumerator.Current => null;

            public void Dispose() { }

            public bool MoveFirst() => false;

            public bool MoveLast() => false;

            public bool MoveNext() => false;

            public bool MovePrevious() => false;

            public void Reset() { }
        }

        #region ClrEnumeratorFactory

        abstract class ClrEnumeratorFactory
        {
            public static IPhpEnumerator CreateEnumerator(IEnumerable enumerable)
            {
                Debug.Assert(enumerable != null);

                // special cases before using reflection
                if (enumerable is IEnumerable<(PhpValue, PhpValue)> valval) return new ValueTupleEnumerator<PhpValue, PhpValue>(valval);
                if (enumerable is IDictionary) return new DictionaryEnumerator(((IDictionary)enumerable).GetEnumerator());
                if (enumerable is IEnumerable<KeyValuePair<object, object>> kv) return new KeyValueEnumerator<object, object>(kv);
                if (enumerable is IEnumerable<object>) return new EnumerableEnumerator(enumerable.GetEnumerator());

                // TODO: cache following for the enumerable type

                // find IEnumerable<>
                foreach (var iface_type in enumerable.GetType().GetInterfaces())
                {
                    if (iface_type.IsGenericType && iface_type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        var item_type = iface_type.GenericTypeArguments[0];
                        if (item_type.IsGenericType)
                        {
                            // ValueTuple<A, B>
                            if (item_type.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
                            {
                                return (ClrEnumerator)Activator.CreateInstance(
                                    typeof(ValueTupleEnumerator<,>).MakeGenericType(item_type.GetGenericArguments()),
                                    enumerable);
                            }

                            // KeyValuePair<A, B>
                            if (item_type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                            {
                                return (ClrEnumerator)Activator.CreateInstance(
                                    typeof(KeyValueEnumerator<,>).MakeGenericType(item_type.GetGenericArguments()),
                                    enumerable);
                            }
                        }
                    }
                }

                // generic
                return new EnumerableEnumerator(enumerable.GetEnumerator());
            }

            abstract class ClrEnumerator : IPhpEnumerator
            {
                abstract protected IEnumerator Enumerator { get; }

                /// <summary>
                /// Current key and value.
                /// </summary>
                PhpValue _key, _value;

                abstract protected void FetchCurrent(ref PhpValue key, ref PhpValue value);

                public bool AtEnd => throw new NotSupportedException();

                public PhpValue CurrentValue => _value;

                public PhpAlias CurrentValueAliased => _value.IsAlias ? _value.Alias : throw new InvalidOperationException();

                public PhpValue CurrentKey => _key;

                public KeyValuePair<PhpValue, PhpValue> Current => new KeyValuePair<PhpValue, PhpValue>(CurrentKey, CurrentValue);

                object IEnumerator.Current => Enumerator.Current;

                public void Dispose()
                {
                    _key = _value = PhpValue.Null;
                    (Enumerator as IDisposable)?.Dispose();
                }

                public virtual bool MoveFirst()
                {
                    Enumerator.Reset();
                    return MoveNext();
                }

                public virtual bool MoveLast()
                {
                    throw new NotImplementedException();
                }

                public virtual bool MoveNext()
                {
                    if (Enumerator.MoveNext())
                    {
                        FetchCurrent(ref _key, ref _value);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public virtual bool MovePrevious()
                {
                    throw new NotSupportedException();
                }

                void IEnumerator.Reset() => Enumerator.Reset();
            }

            /// <summary>
            /// Enumerator of <see cref="IEnumerable"/>.
            /// </summary>
            sealed class EnumerableEnumerator : ClrEnumerator
            {
                readonly IEnumerator _enumerator;

                protected override IEnumerator Enumerator => _enumerator;

                long _key;

                public override bool MoveFirst()
                {
                    _key = -1;
                    return base.MoveFirst();
                }

                public override bool MoveNext()
                {
                    _key++;
                    return base.MoveNext();
                }

                protected override void FetchCurrent(ref PhpValue key, ref PhpValue value)
                {
                    key = _key;
                    value = PhpValue.FromClr(_enumerator.Current);
                }

                public EnumerableEnumerator(IEnumerator enumerator)
                {
                    Debug.Assert(enumerator != null);
                    _enumerator = enumerator;
                    _key = -1;
                }
            }

            /// <summary>
            /// Enumerator of <see cref="IDictionary"/>
            /// </summary>
            sealed class DictionaryEnumerator : ClrEnumerator
            {
                readonly IDictionaryEnumerator _enumerator;
                protected override IEnumerator Enumerator => _enumerator;

                protected override void FetchCurrent(ref PhpValue key, ref PhpValue value)
                {
                    var entry = _enumerator.Entry;
                    key = PhpValue.FromClr(entry.Key);
                    value = PhpValue.FromClr(entry.Value);
                }

                public DictionaryEnumerator(IDictionaryEnumerator enumerator)
                {
                    Debug.Assert(enumerator != null);
                    _enumerator = enumerator;
                }
            }

            sealed class ValueTupleEnumerator<K, V> : ClrEnumerator
            {
                readonly IEnumerator<(K, V)> _enumerator;
                protected override IEnumerator Enumerator => _enumerator;

                protected override void FetchCurrent(ref PhpValue key, ref PhpValue value)
                {
                    var entry = _enumerator.Current;
                    key = PhpValue.FromClr(entry.Item1);
                    value = PhpValue.FromClr(entry.Item2);
                }

                public ValueTupleEnumerator(IEnumerable<(K, V)> enumerable)
                {
                    Debug.Assert(enumerable != null);
                    _enumerator = enumerable.GetEnumerator();
                }
            }

            sealed class KeyValueEnumerator<K, V> : ClrEnumerator
            {
                readonly IEnumerator<KeyValuePair<K, V>> _enumerator;
                protected override IEnumerator Enumerator => _enumerator;

                protected override void FetchCurrent(ref PhpValue key, ref PhpValue value)
                {
                    var entry = _enumerator.Current;
                    key = PhpValue.FromClr(entry.Key);
                    value = PhpValue.FromClr(entry.Value);
                }

                public KeyValueEnumerator(IEnumerable<KeyValuePair<K, V>> enumerable)
                {
                    Debug.Assert(enumerable != null);
                    _enumerator = enumerable.GetEnumerator();
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets <see cref="Iterator"/> object enumerator.
        /// </summary>
        /// <returns>Instance of the enumerator. Cannot be <c>null</c>.</returns>
        public static IPhpEnumerator GetForeachEnumerator(Iterator it)
        {
            Debug.Assert(it != null);
            return new PhpIteratorEnumerator(it);
        }

        /// <summary>
        /// Resolves object enumerator.
        /// </summary>
        /// <exception cref="Exception">Object cannot be enumerated.</exception>
        /// <returns>Instance of the object enumerator. Cannot be <c>null</c>.</returns>
        public static IPhpEnumerator GetForeachEnumerator(object obj, bool aliasedValues, RuntimeTypeHandle caller)
        {
            Debug.Assert(obj != null);

            if (obj is Iterator)
            {
                return GetForeachEnumerator((Iterator)obj);
            }
            else if (obj is IteratorAggregate)
            {
                var last_obj = obj;

                do
                {
                    obj = ((IteratorAggregate)obj).getIterator();
                } while (obj is IteratorAggregate);

                if (obj is Iterator)
                {
                    return GetForeachEnumerator((Iterator)obj);
                }
                else
                {
                    var errmessage = string.Format(Resources.ErrResources.getiterator_must_return_traversable, last_obj.GetType().GetPhpTypeInfo().Name);
                    // throw new (SPL)Exception(ctx, message, 0, null)
                    //Library.SPL.Exception.ThrowSplException(
                    //    _ctx => new Library.SPL.Exception(_ctx, true),
                    //    context,
                    //    string.Format(CoreResources.getiterator_must_return_traversable, last_obj.TypeName), 0, null);
                    throw new ArgumentException(errmessage);
                }
            }
            else if (obj is IPhpEnumerable phpenumerable)
            {
                return phpenumerable.GetForeachEnumerator(aliasedValues, caller);
            }
            else if (obj is IEnumerable enumerable)
            {
                // IDictionaryEnumerator, IEnumerable<ValueTuple>, IEnumerable<KeyValuePair>, IEnumerable, ...
                return GetForeachEnumerator(enumerable);
            }
            else
            {
                // PHP property enumeration
                return new PhpFieldsEnumerator(obj, caller);
            }
        }

        /// <summary>
        /// Gets <see cref="IPhpEnumerator"/> from regular .NET <see cref="IEnumerable"/>.
        /// Enumerator is reflected to properly unwrap <c>key</c> and <c>value</c> of PHP enumeration.
        /// Supported interfaces are <see cref="IDictionaryEnumerator"/>, <see cref="IEnumerable{ValueTuple}"/>, <see cref="IEnumerable{KeyValuePair}"/>, <see cref="IEnumerable"/> etc.
        /// See <see cref="ClrEnumeratorFactory"/> for more details.
        /// </summary>
        internal static IPhpEnumerator GetForeachEnumerator(IEnumerable enumerable) => ClrEnumeratorFactory.CreateEnumerator(enumerable);

        /// <summary>
        /// Gets PHP enumerator of <c>NULL</c> or <b>empty</b> value.
        /// </summary>
        public static IPhpEnumerator GetEmptyForeachEnumerator() => PhpEmptyEnumerator.Instance;

        /// <summary>
        /// Gets enumerator object for given value.
        /// </summary>
        public static IPhpEnumerator GetForeachEnumerator(PhpValue value, bool aliasedValues, RuntimeTypeHandle caller) => value.GetForeachEnumerator(aliasedValues, caller);

        #endregion

        #region Copy, Unpack

        /// <summary>
        /// Gets copy of given value.
        /// </summary>
        public static PhpValue DeepCopy(PhpValue value) => value.DeepCopy();

        /// <summary>
        /// Deep copies the value in-place.
        /// Called when this has been passed by value and inplace dereferencing and copying is necessary.
        /// </summary>
        [DebuggerNonUserCode, DebuggerStepThrough]
        public static void PassValue(ref PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.MutableString:
                    // lazy copy
                    value.MutableStringBlob.AddRef();
                    break;
                case PhpTypeCode.PhpArray:
                    // lazy copy
                    value = new PhpValue(value.Array.DeepCopy());
                    break;
                case PhpTypeCode.Alias:
                    // dereference & lazy copy
                    value = value.Alias.Value.DeepCopy();
                    break;
            }
        }

        /// <summary>
        /// Performs <c>clone</c> operation on given object.
        /// </summary>
        public static object Clone(Context ctx, object value)
        {
            if (value is IPhpCloneable cloneable)
            {
                value = cloneable.Clone();
            }
            else if (value != null)
            {
                value = CloneRaw(ctx, value);
            }
            else
            {
                PhpException.Throw(PhpError.Error, Resources.ErrResources.clone_called_on_non_object);
            }

            //

            return value;
        }

        /// <summary>
        /// Performs memberwise clone of the object.
        /// Calling <c>__clone</c> eventually.
        /// </summary>
        public static object CloneRaw(Context ctx, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var tinfo = value.GetPhpTypeInfo();

            // memberwise clone
            var newobj = tinfo.CreateUninitializedInstance(ctx);
            if (newobj != null)
            {
                Serialization.MemberwiseClone(tinfo, value, newobj);

                //
                value = newobj;

                // __clone(), only if __clone() is public
                var __clone = tinfo.RuntimeMethods[TypeMethods.MagicMethods.__clone];
                if (__clone != null && __clone.IsPublic())
                {
                    __clone.Invoke(ctx, value);
                }
            }
            else
            {
                PhpException.Throw(PhpError.Error, Resources.ErrResources.class_instantiation_failed, tinfo.Name);
            }

            //
            return value;
        }

        /// <summary>
        /// Every property of type <see cref="PhpValue"/> will be deeply copied inplace, including runtime fields.
        /// Calling <c>__clone</c> eventually.
        /// </summary>
        public static object CloneInPlace(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var tinfo = value.GetPhpTypeInfo();

            // clone runtime fields:
            if (tinfo.RuntimeFieldsHolder != null)
            {
                var runtimefields = (PhpArray)tinfo.RuntimeFieldsHolder.GetValue(value);
                tinfo.RuntimeFieldsHolder.SetValue(value, runtimefields?.Clone());
            }

            // deep copy instance fields (of type PhpValue)
            foreach (var p in tinfo.DeclaredFields.InstanceProperties.OfType<PhpPropertyInfo.ClrFieldProperty>())
            {
                if (p.Field.FieldType == typeof(PhpValue))
                {
                    var oldvalue = (PhpValue)p.Field.GetValue(value);
                    p.Field.SetValue(value, (object)oldvalue.DeepCopy());
                }
            }

            // __clone(), only if __clone() is public
            var __clone = tinfo.RuntimeMethods[TypeMethods.MagicMethods.__clone];
            if (__clone != null && __clone.IsPublic())
            {
                __clone.Invoke(null, value); // 'ctx' is not needed ... probably
            }

            //
            return value;
        }

        /// <summary>
        /// The method implements <c>...</c> unpack operator.
        /// Unpacks <paramref name="argument"/> into <paramref name="stack"/>.
        /// </summary>
        /// <param name="stack">The list with unpacked arguments.</param>
        /// <param name="argument">Value to be unpacked.</param>
        /// <param name="byrefs">Bit mask of parameters that are passed by reference. Arguments corresponding to <c>1</c>-bit are aliased.</param>
        public static void Unpack(List<PhpValue> stack, PhpValue argument, ulong byrefs)
        {
            // https://wiki.php.net/rfc/argument_unpacking

            switch (argument.TypeCode)
            {
                case PhpTypeCode.PhpArray:
                    Unpack(stack, argument.Array, byrefs);
                    break;

                case PhpTypeCode.Object:
                    if (argument.Object is Traversable traversable)
                    {
                        Unpack(stack, traversable, byrefs);
                        break;
                    }
                    else if (argument.Object is Array array)
                    {
                        Unpack(stack, array, byrefs);
                        break;
                    }
                    else
                    {
                        goto default;
                    }

                case PhpTypeCode.Alias:
                    Unpack(stack, argument.Alias.Value, byrefs);
                    break;

                default:
                    // TODO: Warning: Only arrays and Traversables can be unpacked
                    // do not add item to the arguments list // stack.Add(argument);
                    break;
            }
        }

        /// <summary>
        /// The method implements <c>...</c> unpack operator.
        /// Unpacks <paramref name="array"/> into <paramref name="stack"/>.
        /// </summary>
        /// <param name="stack">The list with unpacked arguments.</param>
        /// <param name="array">Value to be unpacked.</param>
        /// <param name="byrefs">Bit mask of parameters that are passed by reference. Arguments corresponding to <c>1</c>-bit are aliased.</param>
        public static void Unpack(List<PhpValue> stack, PhpArray array, ulong byrefs)
        {
            Debug.Assert(array != null);

            var enumerator = array.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.CurrentKey.IsString)
                {
                    // TODO: E_RECOVERABLE error
                    break;  // no further arguments will be unpacked
                }

                if ((byrefs & (1ul << stack.Count)) == 0)
                {
                    // pass by value
                    stack.Add(enumerator.CurrentValue);
                }
                else
                {
                    // pass by reference
                    stack.Add(PhpValue.Create(enumerator.CurrentValueAliased));
                }
            }
        }

        /// <summary>
        /// The method implements <c>...</c> unpack operator.
        /// Unpacks <paramref name="array"/> into <paramref name="stack"/>.
        /// </summary>
        /// <param name="stack">The list with unpacked arguments.</param>
        /// <param name="array">Value to be unpacked.</param>
        /// <param name="byrefs">Bit mask of parameters that are passed by reference. Arguments corresponding to <c>1</c>-bit are aliased.</param>
        static void Unpack(List<PhpValue> stack, Array array, ulong byrefs)
        {
            for (int i = 0; i < array.Length; i++)
            {
                stack.Add(PhpValue.FromClr(array.GetValue(i)));
            }
        }

        /// <summary>
        /// The method implements <c>...</c> unpack operator.
        /// Unpacks <paramref name="traversable"/> into <paramref name="stack"/>.
        /// </summary>
        /// <param name="stack">The list with unpacked arguments.</param>
        /// <param name="traversable">Value to be unpacked.</param>
        /// <param name="byrefs">Bit mask of parameters that are passed by reference. Arguments corresponding to <c>1</c>-bit are aliased.</param>
        public static void Unpack(List<PhpValue> stack, Traversable traversable, ulong byrefs)
        {
            Debug.Assert(traversable != null);

            if (traversable is IteratorAggregate aggr)
            {
                Unpack(stack, aggr.getIterator(), byrefs);
            }
            else if (traversable is Iterator iterator)
            {
                iterator.rewind();
                while (iterator.valid())
                {
                    Debug.Assert((byrefs & (1ul << stack.Count)) == 0, "Cannot pass by-reference when unpacking a Traversable");
                    //{
                    //    // TODO: Warning: Cannot pass by-reference argument {stack.Count + 1} of {function_name}() by unpacking a Traversable, passing by-value instead
                    //}

                    stack.Add(iterator.current());
                    iterator.next();
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

        #endregion

        #region ReadConstant

        /// <summary>
        /// Gets constant value, throws <c>notice</c> if constant is not defined.
        /// </summary>
        public static PhpValue ReadConstant(Context ctx, string name, ref int idx)
        {
            Debug.Assert(name != null, nameof(name));

            if (ctx.TryGetConstant(name, out var value, ref idx))
            {
                return value;
            }
            else
            {
                // Warning: undefined constant
                PhpException.Throw(PhpError.Notice, Resources.ErrResources.undefined_constant, name);
                return name;
            }
        }

        /// <summary>
        /// Gets constant value, throws <c>notice</c> if constant is not defined.
        /// </summary>
        public static PhpValue ReadConstant(Context ctx, string name, ref int idx, string fallbackName)
        {
            Debug.Assert(name != null, nameof(name));
            Debug.Assert(fallbackName != null, nameof(fallbackName));

            if (ctx.TryGetConstant(name, out var value, ref idx) ||
                ctx.TryGetConstant(fallbackName, out value))
            {
                return value;
            }
            else
            {
                // Warning: undefined constant
                PhpException.Throw(PhpError.Notice, Resources.ErrResources.undefined_constant, fallbackName);
                return fallbackName;
            }
        }

        /// <summary>
        /// Constant declaration.
        /// </summary>
        public static void DeclareConstant(Context ctx, string name, ref int idx, PhpValue value)
        {
            ctx.DefineConstant(name, value, ref idx, ignorecase: false);
        }

        #endregion

        #region Closure

        public static RoutineInfo AnonymousRoutine(string name, RuntimeMethodHandle handle) => new PhpAnonymousRoutineInfo(name, handle);

        /// <summary>
        /// Create <see cref="Closure"/> with specified anonymous function and used parameters.
        /// </summary>
        public static Closure BuildClosure(Context/*!*/ctx, IPhpCallable routine, object @this, RuntimeTypeHandle scope, PhpTypeInfo statictype, PhpArray/*!*/parameter, PhpArray/*!*/@static)
            => new Closure(ctx, routine, @this, scope, statictype, parameter, @static);

        public static Context Context(this Closure closure) => closure._ctx;

        /// <summary>Resolves late static bound type of closiure. Can be <c>null</c> reference.</summary>
        public static PhpTypeInfo Static(this Closure closure)
        {
            if (closure._this != null)
            {
                // typeof $this
                return closure._this.GetPhpTypeInfo();
            }

            if (closure._statictype != null)
            {
                // static
                return closure._statictype;
            }

            // self or NULL
            return closure._scope.GetPhpTypeInfo();
        }

        public static RuntimeTypeHandle Scope(this Closure closure) => closure._scope;

        public static object This(this Closure closure) => closure._this;

        /// <summary>
        /// Gets internal <see cref="IPhpCallable"/> object invoked by the closure.
        /// </summary>
        public static IPhpCallable Callable(this Closure closure) => closure._callable;

        #endregion

        #region Generator

        /// <summary>
        /// Create <see cref="Generator"/> with specified state machine function and parameters.
        /// </summary>
        public static Generator BuildGenerator(Context ctx, PhpArray locals, PhpArray tmpLocals, GeneratorStateMachineDelegate smmethod, RuntimeMethodHandle ownerhandle) => new Generator(ctx, locals, tmpLocals, smmethod, ownerhandle);

        public static int GetGeneratorState(Generator g) => g._state;

        public static void SetGeneratorState(Generator g, int newState) => g._state = newState;

        /// <summary>
        /// In case generator has an exception, throws it.
        /// The current exception is then nullified.
        /// </summary>
        [DebuggerNonUserCode, DebuggerHidden]
        public static void HandleGeneratorException(Generator g)
        {
            var exception = g._currException;
            g._currException = null;

            if (exception != null)
            {
                throw exception;
            }
        }

        /// <summary>Set yielded value from generator where key is not specified.</summary>
        public static void SetGeneratorCurrent(Generator g, PhpValue value)
        {
            g._currValue = value;
            g._currKey = (PhpValue)(++g._maxNumericalKey);
        }

        /// <summary>
        /// Sets yielded value from generator with key.
        /// This operator does not update auto-incremented Generator key.
        /// </summary>
        public static void SetGeneratorCurrentFrom(Generator g, PhpValue value, PhpValue key)
        {
            g._currValue = value;
            g._currKey = key;
        }

        /// <summary>Set yielded value from generator with key.</summary>
        public static void SetGeneratorCurrent(Generator g, PhpValue value, PhpValue key)
        {
            SetGeneratorCurrentFrom(g, value, key);

            // update the Generator auto-increment key
            if (key.IsLong(out var ikey) && ikey > g._maxNumericalKey)
            {
                g._maxNumericalKey = ikey;
            }
        }

        public static PhpValue GetGeneratorSentItem(Generator g) => g._currSendItem;

        public static void SetGeneratorReturnedValue(Generator g, PhpValue value) => g._returnValue = value;

        public static object GetGeneratorThis(Generator g) => g._this;

        public static Generator SetGeneratorThis(this Generator generator, object @this)
        {
            generator._this = @this;
            return generator;
        }

        /// <summary>
        /// Resolves generator's <c>static</c> type.
        /// </summary>
        /// <returns><see cref="PhpTypeInfo"/> refering to the lazy static bound type. Cannot be <c>null</c>.</returns>
        public static PhpTypeInfo GetGeneratorLazyStatic(this Generator generator)
        {
            return generator._static ?? generator._this?.GetPhpTypeInfo() ?? throw new InvalidOperationException();
        }

        public static Generator SetGeneratorLazyStatic(this Generator generator, PhpTypeInfo @static)
        {
            generator._static = @static;
            return generator;
        }

        public static Context GetGeneratorContext(Generator g) => g._ctx;

        public static GeneratorStateMachineDelegate GetGeneratorMethod(Generator g) => g._stateMachineMethod;

        public static MethodInfo GetGeneratorOwnerMethod(Generator g) => (MethodInfo)MethodBase.GetMethodFromHandle(g._ownerhandle);

        public static Generator SetGeneratorDynamicScope(this Generator g, RuntimeTypeHandle scope)
        {
            g._scope = scope;
            return g;
        }

        public static RuntimeTypeHandle GetGeneratorDynamicScope(this Generator g) => g._scope;

        #endregion

        #region Dynamic

        /// <summary>
        /// Performs dynamic code evaluation in given context.
        /// </summary>
        /// <returns>Evaluated code return value.</returns>
        public static PhpValue Eval(Context ctx, PhpArray locals, object @this, RuntimeTypeHandle self, string code, string currentpath, int line, int column)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(locals != null);

            if (string.IsNullOrEmpty(code))
            {
                return PhpValue.Null;
            }

            var script = Core.Context.DefaultScriptingProvider.CreateScript(
                new Context.ScriptOptions()
                {
                    Context = ctx,
                    Location = new Location(Path.Combine(ctx.RootPath, currentpath), line, column),
                    EmitDebugInformation = Debugger.IsAttached,   // CONSIDER // DOC
                    IsSubmission = true,
                },
                code);

            //
            return script.Evaluate(ctx, locals, @this, self);
        }

        #endregion

        #region Paths

        /// <summary>
        /// Normalizes path's slashes for the current platform.
        /// </summary>
        public static string NormalizePath(string value) => Utilities.CurrentPlatform.NormalizeSlashes(value);

        #endregion

        #region BindTargetToMethod

        /// <summary>
        /// Helper lightweight class to reuse already bound <see cref="PhpInvokable"/> to be used as <see cref="PhpCallable"/>
        /// by calling it on a given target.
        /// </summary>
        sealed class BoundTargetCallable : IPhpCallable
        {
            readonly object _target;
            readonly PhpInvokable _invokable;

            public BoundTargetCallable(object target, PhpInvokable invokable)
            {
                _target = target;
                _invokable = invokable;
            }

            public PhpValue Invoke(Context ctx, params PhpValue[] arguments) => _invokable.Invoke(ctx, _target, arguments);

            public PhpValue ToPhpValue() => PhpValue.Null;
        }

        /// <summary>
        /// Creates an <see cref="IPhpCallable"/> from an instance method, binding the target to call the method on.
        /// </summary>
        public static IPhpCallable BindTargetToMethod(object targetInstance, RoutineInfo routine)
        {
            if (routine is PhpMethodInfo methodInfo)
            {
                return new BoundTargetCallable(targetInstance, methodInfo.PhpInvokable);
            }

            return PhpCallback.CreateInvalid();
        }

        #endregion
    }
}
