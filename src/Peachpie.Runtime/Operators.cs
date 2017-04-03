using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core
{
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
        internal static PhpValue BitAnd(ref PhpValue x, ref PhpValue y)
        {
            var bx = x.ToBytesOrNull();
            if (bx != null)
            {
                var by = y.ToBytesOrNull();
                if (by != null)
                {
                    throw new NotImplementedException();
                }
            }

            //
            return PhpValue.Create(x.ToLong() & y.ToLong());
        }

        /// <summary>
        /// Performs bitwise or operation.
        /// </summary>
        internal static PhpValue BitOr(ref PhpValue x, ref PhpValue y)
        {
            var bx = x.ToBytesOrNull();
            if (bx != null)
            {
                var by = y.ToBytesOrNull();
                if (by != null)
                {
                    throw new NotImplementedException();
                }
            }

            //
            return PhpValue.Create(x.ToLong() | y.ToLong());
        }

        /// <summary>
        /// Performs exclusive or operation.
        /// </summary>
        internal static PhpValue BitXor(ref PhpValue x, ref PhpValue y)
        {
            var bx = x.ToBytesOrNull();
            if (bx != null)
            {
                var by = y.ToBytesOrNull();
                if (by != null)
                {
                    return PhpValue.Create(new PhpString(BitXor(bx, by)));
                }
            }

            //
            return PhpValue.Create(x.ToLong() ^ y.ToLong());
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
        internal static PhpValue BitNot(ref PhpValue x)
        {
            switch (x.TypeCode)
            {
                case PhpTypeCode.Long: return PhpValue.Create(~x.Long);

                case PhpTypeCode.Int32: return PhpValue.Create(~x.ToLong());

                case PhpTypeCode.Alias: return BitNot(ref x.Alias.Value);

                case PhpTypeCode.String:
                case PhpTypeCode.WritableString:
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
        internal static PhpNumber Div(ref PhpValue x, ref PhpValue y)
        {
            PhpNumber nx, ny;
            var info = x.ToNumber(out nx) | y.ToNumber(out ny);

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
            if (target.IsAlias)
            {
                target.Alias.Value = value;
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
        public static void SetValue(PhpAlias target, PhpValue value)
        {
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
        /// Implementation of PHP <c>isset</c> operator.
        /// </summary>
        public static bool IsSet(PhpValue value) => value.IsSet && !value.IsNull;   // TODO: !Alias.IsNull

        /// <summary>
        /// Implements <c>empty</c> operator.
        /// </summary>
        public static bool IsEmpty(PhpValue value) => !value.IsSet || value.IsEmpty;

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

            public void AddValue(PhpValue value)
            {
                throw new NotSupportedException();
            }

            public PhpAlias EnsureItemAlias(IntStringKey key)
            {
                return _array.offsetGet(PhpValue.Create(key)).EnsureAlias();
            }

            public IPhpArray EnsureItemArray(IntStringKey key)
            {
                return _array.offsetGet(PhpValue.Create(key)).EnsureArray();
            }

            public object EnsureItemObject(IntStringKey key)
            {
                return _array.offsetGet(PhpValue.Create(key)).EnsureObject();
            }

            public PhpValue GetItemValue(IntStringKey key)
            {
                return _array.offsetGet(PhpValue.Create(key));
            }

            public void RemoveKey(IntStringKey key)
            {
                _array.offsetUnset(PhpValue.Create(key));
            }

            public void SetItemAlias(IntStringKey key, PhpAlias alias)
            {
                _array.offsetSet(PhpValue.Create(key), PhpValue.Create(alias));
            }

            public void SetItemValue(IntStringKey key, PhpValue value)
            {
                _array.offsetSet(PhpValue.Create(key), value);
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

            // TODO: Fatal error: Uncaught Error: Cannot use object of type {0} as array
            throw new InvalidOperationException(string.Format(Resources.ErrResources.object_used_as_array, obj.GetType().FullName));
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="string"/>.
        /// </summary>
        /// <param name="value">String to be accessed as array.</param>
        /// <param name="index">Index.</param>
        /// <returns>Character on index or empty string if index is our of range.</returns>
        public static string GetItemValue(string value, int index)
        {
            return (value != null && index >= 0 && index < value.Length)
                ? value[index].ToString()
                : string.Empty;
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="string"/>.
        /// </summary>
        public static string GetItemValue(string value, IntStringKey key)
        {
            int index = key.IsInteger
                ? key.Integer
                : (int)Convert.StringToLongInteger(key.String);

            return GetItemValue(value, index);
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="PhpValue"/>.
        /// </summary>
        public static PhpValue GetItemValue(PhpValue value, IntStringKey key, bool quiet = false) => value.GetArrayItem(key, quiet);

        /// <summary>
        /// Implements <c>&amp;[]</c> operator on <see cref="PhpValue"/>.
        /// </summary>
        public static PhpAlias EnsureItemAlias(PhpValue value, IntStringKey key, bool quiet = false) => value.EnsureItemAlias(key, quiet);

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

        public static PhpValue PropertyGetValue(RuntimeTypeHandle caller, object instance, PhpValue prop)
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

        #endregion

        #region GetForeachEnumerator

        /// <summary>
        /// Provides the <see cref="IPhpEnumerator"/> interface by wrapping a user-implemeted <see cref="Iterator"/>.
        /// </summary>
        /// <remarks>
        /// Instances of this class are iterated when <c>foreach</c> is used on object of a class
        /// that implements <see cref="Iterator"/> or <see cref="IteratorAggregate"/>.
        /// </remarks>
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

            public PhpAlias CurrentValueAliased => _iterator.current().EnsureAlias();

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

            public PhpAlias CurrentValueAliased => _enumerator.Current.Value.EnsureAlias();

            object IEnumerator.Current => _enumerator.Current;

            public void Dispose() => _enumerator.Dispose();

            public bool MoveFirst()
            {
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
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
                return new PhpIteratorEnumerator((Iterator)obj);
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
                    return new PhpIteratorEnumerator((Iterator)obj);
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
            else
            {
                // TODO: CLR enumerators: IDictionaryEnumerator, IEnumerable

                // PHP property enumeration
                return new PhpFieldsEnumerator(obj, caller);
            }
        }

        /// <summary>
        /// Gets PHP enumerator of <c>NULL</c> or <b>empty</b> value.
        /// </summary>
        public static IPhpEnumerator GetEmptyForeachEnumerator() => PhpEmptyEnumerator.Instance;

        #endregion

        #region Copy

        /// <summary>
        /// Gets copy of given value.
        /// </summary>
        public static PhpValue DeepCopy(PhpValue value) => value.DeepCopy();

        /// <summary>
        /// Performs <c>clone</c> operation on given object.
        /// </summary>
        public static object Clone(Context ctx, object value)
        {
            if (value != null)
            {
                var tinfo = value.GetPhpTypeInfo();

                // memberwise clone
                var newobj = tinfo.GetUninitializedInstance(ctx);
                if (newobj != null)
                {
                    Serialization.MemberwiseClone(tinfo, value, newobj);

                    //
                    value = newobj;

                    // __clone()
                    var __clone = tinfo.RuntimeMethods[TypeMethods.MagicMethods.__clone];
                    if (__clone != null)
                    {
                        // TODO: check __clone does not have parameters -> ErrResources.clone_cannot_take_arguments
                        __clone.Invoke(ctx, value);
                    }
                }
                else
                {
                    PhpException.Throw(PhpError.Error, Resources.ErrResources.class_instantiation_failed, tinfo.Name);
                }
            }
            else
            {
                PhpException.Throw(PhpError.Error, Resources.ErrResources.clone_called_on_non_object);
            }

            return value;
        }

        #endregion

        #region BuildClosure

        /// <summary>
        /// Create <see cref="Closure"/> with specified anonymous function and used parameters.
        /// </summary>
        public static Closure BuildClosure(RoutineInfo routine, PhpArray parameter, PhpArray @static) => new Closure(routine, parameter, @static);

        #endregion

        #region Enumerator

        /// <summary>
        /// Gets enumerator object for given value.
        /// </summary>
        public static IPhpEnumerator GetForeachEnumerator(PhpValue value, bool aliasedValues, RuntimeTypeHandle caller) => value.GetForeachEnumerator(aliasedValues, caller);

        #endregion

        #region Dynamic

        /// <summary>
        /// Performs dynamic code evaluation in given context.
        /// </summary>
        /// <returns>Evaluated code return value.</returns>
        public static PhpValue Eval(Context ctx, PhpArray locals, object @this, string code, string currentpath, int line, int column)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(locals != null);

            if (string.IsNullOrEmpty(code))
            {
                return PhpValue.Null;
            }

            var script = ctx.ScriptingProvider.CreateScript(
                new Context.ScriptOptions()
                {
                    Context = ctx,
                    Location = new Location(Path.Combine(ctx.RootPath, currentpath), line, column),
                    EmitDebugInformation = false,   // TODO
                    IsSubmission = true,
                },
                code);

            //
            return script.Evaluate(ctx, locals, @this);
        }

        #endregion
    }
}
