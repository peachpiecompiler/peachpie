using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Pchp.Core.Reflection;
using System.Reflection;

namespace Pchp.Core
{
    partial struct PhpValue
    {
        /// <summary>
        /// Methods table for <see cref="PhpValue"/> instance.
        /// </summary>
        [DebuggerNonUserCode, DebuggerStepThrough]
        abstract class TypeTable
        {
            #region Singletons

            public static readonly NullTable NullTable = new NullTable();
            public static readonly LongTable LongTable = new LongTable();
            public static readonly DoubleTable DoubleTable = new DoubleTable();
            public static readonly BoolTable BoolTable = new BoolTable();
            public static readonly StringTable StringTable = new StringTable();
            public static readonly TypeTable MutableStringTable = new MutableStringTable();
            public static readonly ClassTable ClassTable = new ClassTable();
            public static readonly ArrayTable ArrayTable = new ArrayTable();
            public static readonly AliasTable AliasTable = new AliasTable();

            #endregion

            public abstract PhpTypeCode Type { get; }
            public virtual bool IsNull(ref PhpValue me) => false;
            public virtual bool IsEmpty(ref PhpValue me) => ToBoolean(ref me) == false;
            public abstract object ToClass(ref PhpValue me);
            public abstract string ToStringQuiet(ref PhpValue me);
            public abstract string ToString(ref PhpValue me, Context ctx);
            public abstract string ToStringOrThrow(ref PhpValue me, Context ctx);
            /// <summary>Implicit conversion to string, preserves <c>null</c>, throws if conversion is not possible.</summary>
            public virtual string AsString(ref PhpValue me, Context ctx) => ToString(ref me, ctx);
            public abstract long ToLong(ref PhpValue me);
            public virtual long ToLongOrThrow(ref PhpValue me) => throw PhpException.TypeErrorException();
            public abstract double ToDouble(ref PhpValue me);
            public abstract bool ToBoolean(ref PhpValue me);
            public abstract Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number);

            public abstract bool TryToIntStringKey(ref PhpValue me, out IntStringKey key);

            /// <summary>
            /// Gets enumerator object used within foreach statement.
            /// </summary>
            public abstract IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller);

            /// <summary>
            /// Compares two value operands.
            /// </summary>
            /// <param name="me">Reference to self, representing the left operand.</param>
            /// <param name="right">The right operand.</param>
            /// <returns>Comparison result.
            /// Zero for equality,
            /// negative value for <paramref name="me"/> &lt; <paramref name="right"/>,
            /// position value for <paramref name="me"/> &gt; <paramref name="right"/>.</returns>
            public abstract int Compare(ref PhpValue me, PhpValue right);

            /// <summary>
            /// Performs strict comparison.
            /// </summary>
            /// <param name="me">Reference to self, representing the left operand.</param>
            /// <param name="right">The right operand.</param>
            /// <returns>The value determining operands are strictly equal.</returns>
            public abstract bool StrictEquals(ref PhpValue me, PhpValue right);

            /// <summary>
            /// Ensures the value is a class object.
            /// In case it isn't, creates stdClass according to PHP semantics.
            /// In case current value is empty, replaces current value with newly created stdClass.
            /// </summary>
            /// <returns>Non-null object.</returns>
            public abstract object EnsureObject(ref PhpValue me);

            /// <summary>
            /// Ensures the value is a PHP array.
            /// In case it isn't, creates PhpArray according to PHP semantics.
            /// In case current value is empty, replaces current value with newly created array.
            /// </summary>
            /// <returns>Non-null object.</returns>
            public abstract IPhpArray EnsureArray(ref PhpValue me);

            /// <summary>
            /// Ensures the value as an alias.
            /// In case it isn't, the value is aliased.
            /// </summary>
            /// <returns>Non-null alias of the value.</returns>
            public virtual PhpAlias EnsureAlias(ref PhpValue me)
            {
                Debug.Assert(Type != PhpTypeCode.Alias, "To be overriden!");
                var alias = new PhpAlias(me, 1);
                me = Create(alias);
                return alias;
            }

            /// <summary>
            /// Gets <see cref="IPhpArray"/> instance providing access to the value with array operators.
            /// Returns <c>null</c> if underlaying value does provide array access.
            /// </summary>
            public virtual IPhpArray GetArrayAccess(ref PhpValue me) => null;

            /// <summary>
            /// Accesses the value as an array and gets item at given index.
            /// Gets <c>void</c> value in case the key is not found.
            /// </summary>
            public virtual PhpValue GetArrayItem(ref PhpValue me, PhpValue index, bool quiet) => PhpValue.Null;

            /// <summary>
            /// Accesses the value as an array and gets item at given index.
            /// Gets empty value in case the key is not found.
            /// </summary>
            public virtual PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => new PhpAlias(PhpValue.Null);

            /// <summary>
            /// Converts value to an array.
            /// </summary>
            public abstract PhpArray ToArray(ref PhpValue me);

            /// <summary>
            /// Gets <see cref="PhpArray"/> or throws an exception.
            /// </summary>
            public virtual PhpArray ArrayOrThrow(ref PhpValue me) => throw PhpException.TypeErrorException();

            /// <summary>
            /// Gets underlaying class instance or <c>null</c>.
            /// </summary>
            public virtual object AsObject(ref PhpValue me) => null;

            /// <summary>
            /// Gets callable wrapper for dynamic object invocation.
            /// </summary>
            /// <param name="me"></param>
            /// <param name="callerCtx">Current caller type.</param>
            /// <param name="callerObj">Current caller <c>$this</c>. Used to resolve <c>parent</c> and <c>self</c> instances.</param>
            /// <returns>Instance of a callable object, cannot be <c>null</c>, can be invalid.</returns>
            public virtual IPhpCallable AsCallable(ref PhpValue me, RuntimeTypeHandle callerCtx, object callerObj) => PhpCallback.CreateInvalid();

            /// <summary>
            /// Creates a deep copy of PHP variable.
            /// </summary>
            /// <returns>A deep copy of the value.</returns>
            public virtual PhpValue DeepCopy(ref PhpValue me) => me;

            /// <summary>
            /// Performs dereferencing and deep copying of the value inplace.
            /// </summary>
            public virtual void PassValue(ref PhpValue me) { }

            /// <summary>
            /// Outputs current value to the <see cref="Context.Output"/> or <see cref="Context.OutputStream"/>.
            /// </summary>
            public abstract void Output(ref PhpValue me, Context ctx);

            /// <summary>
            /// Debug textual representation of the value.
            /// </summary>
            public abstract string DisplayString(ref PhpValue me);

            /// <summary>
            /// Calls corresponding <c>Accept</c> method on visitor.
            /// </summary>
            /// <param name="me">Reference to this.</param>
            /// <param name="visitor">Visitor to be called. Cannot be <c>null</c>.</param>
            public abstract void Accept(ref PhpValue me, PhpVariableVisitor visitor);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        class NullTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Null;
            public override bool IsNull(ref PhpValue me) => true;
            public override bool IsEmpty(ref PhpValue me) => true;
            public override object ToClass(ref PhpValue me) => new stdClass();
            public override string ToStringQuiet(ref PhpValue me) => string.Empty;
            public override string ToString(ref PhpValue me, Context ctx) => string.Empty;
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => string.Empty;
            public override string AsString(ref PhpValue me, Context ctx) => null;
            public override long ToLong(ref PhpValue me) => 0;
            public override double ToDouble(ref PhpValue me) => 0.0;
            public override bool ToBoolean(ref PhpValue me) => false;
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                number = PhpNumber.Create(0L);
                return Convert.NumberInfo.LongInteger;
            }
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) { key = IntStringKey.EmptyStringKey; return true; }
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => Operators.GetEmptyForeachEnumerator();
            public override int Compare(ref PhpValue me, PhpValue right) => Comparison.CompareNull(right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right) => right.IsNull;
            public override object EnsureObject(ref PhpValue me)
            {
                // TODO: Err: Warning: Creating default object from empty value
                var obj = ToClass(ref me);
                me = PhpValue.FromClass(obj);
                return obj;
            }
            public override IPhpArray EnsureArray(ref PhpValue me)
            {
                var arr = new PhpArray();
                me = PhpValue.Create(arr);
                return arr;
            }
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet)
            {
                var arr = new PhpArray();
                me = PhpValue.Create(arr);
                return arr.EnsureItemAlias(index, quiet);
            }
            public override PhpArray ToArray(ref PhpValue me) => PhpArray.NewEmpty();
            public override PhpArray ArrayOrThrow(ref PhpValue me) => null;
            public override void Output(ref PhpValue me, Context ctx) { }
            public override string DisplayString(ref PhpValue me) => "null";    // lowercased `null` as it is shown for other CLR null references
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.AcceptNull();
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class VoidTable : NullTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Undefined;
            //public override string DisplayString(ref PhpValue me) => null;
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class LongTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Long;
            public override object ToClass(ref PhpValue me) => new stdClass(me);	// new stdClass(){ $scalar = VALUE }
            public override string ToStringQuiet(ref PhpValue me) => me.Long.ToString();
            public override string ToString(ref PhpValue me, Context ctx) => me.Long.ToString();
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => me.Long.ToString();
            public override long ToLong(ref PhpValue me) => me.Long;
            public override long ToLongOrThrow(ref PhpValue me) => me.Long;
            public override double ToDouble(ref PhpValue me) => (double)me.Long;
            public override bool ToBoolean(ref PhpValue me) => me.Long != 0;
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                number = PhpNumber.Create(me.Long);
                return Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger;
            }
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) { key = new IntStringKey((int)me.Long); return true; }
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => Operators.GetEmptyForeachEnumerator();
            public override int Compare(ref PhpValue me, PhpValue right) => Comparison.Compare(me.Long, right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right) => right.IsLong(out long l) && me.Long == l;
            public override object EnsureObject(ref PhpValue me) => PhpValue.FromClass(ToClass(ref me)); // me is not changed
            public override IPhpArray EnsureArray(ref PhpValue me) => new PhpArray(); // me is not changed
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => new PhpAlias(PhpValue.Null);
            public override PhpArray ToArray(ref PhpValue me) => PhpArray.New(me);
            public override string DisplayString(ref PhpValue me) => me.Long.ToString();
            public override void Output(ref PhpValue me, Context ctx) => ctx.Echo(me.Long);
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.Accept(me.Long);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class DoubleTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Double;
            public override object ToClass(ref PhpValue me) => new stdClass(me);	// new stdClass(){ $scalar = VALUE }
            public override string ToStringQuiet(ref PhpValue me) => Convert.ToString(me.Double);
            public override string ToString(ref PhpValue me, Context ctx) => Convert.ToString(me.Double, ctx);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => Convert.ToString(me.Double, ctx);
            public override long ToLong(ref PhpValue me) => (long)me.Double;
            public override long ToLongOrThrow(ref PhpValue me) => ToLong(ref me);
            public override double ToDouble(ref PhpValue me) => me.Double;
            public override bool ToBoolean(ref PhpValue me) => me.Double != 0;
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                number = PhpNumber.Create(me.Double);
                return Convert.NumberInfo.IsNumber | Convert.NumberInfo.Double;
            }
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) { key = new IntStringKey((int)me.Double); return true; }
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => Operators.GetEmptyForeachEnumerator();
            public override int Compare(ref PhpValue me, PhpValue right) => Comparison.Compare(me.Double, right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right) => right.IsDouble(out double d) && me.Double == d;
            public override object EnsureObject(ref PhpValue me) => PhpValue.FromClass(ToClass(ref me)); // me is not changed
            public override IPhpArray EnsureArray(ref PhpValue me) => new PhpArray(); // me is not changed
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => new PhpAlias(PhpValue.Null);
            public override PhpArray ToArray(ref PhpValue me) => PhpArray.New(me);
            public override string DisplayString(ref PhpValue me) => me.Double.ToString();
            public override void Output(ref PhpValue me, Context ctx) => ctx.Echo(me.Double);
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.Accept(me.Double);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class BoolTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Boolean;
            public override object ToClass(ref PhpValue me) => new stdClass(me);	// new stdClass(){ $scalar = VALUE }
            public override string ToStringQuiet(ref PhpValue me) => Convert.ToString(me.Boolean);
            public override string ToString(ref PhpValue me, Context ctx) => Convert.ToString(me.Boolean);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => Convert.ToString(me.Boolean);
            public override long ToLong(ref PhpValue me) => me.Boolean ? 1L : 0L;
            public override long ToLongOrThrow(ref PhpValue me) => ToLong(ref me);
            public override double ToDouble(ref PhpValue me) => me.Boolean ? 1.0 : 0.0;
            public override bool ToBoolean(ref PhpValue me) => me.Boolean;
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                number = PhpNumber.Create(me.Boolean ? 1L : 0L);
                return Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger;
            }
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) { key = new IntStringKey(me.Boolean ? 1 : 0); return true; }
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => Operators.GetEmptyForeachEnumerator();
            public override int Compare(ref PhpValue me, PhpValue right) => Comparison.Compare(me.Boolean, right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right) => right.IsBoolean(out bool by) && by == me.Boolean;
            public override object EnsureObject(ref PhpValue me)
            {
                var obj = new stdClass();   // empty class

                // me is changed if me.Boolean == FALSE
                if (me.Boolean == false)
                    me = PhpValue.FromClass(obj);

                return obj;
            }
            public override IPhpArray EnsureArray(ref PhpValue me)
            {
                var arr = new PhpArray();

                // me is changed if me.Boolean == FALSE
                if (me.Boolean == false)
                    me = PhpValue.Create(arr);

                return arr;
            }
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => new PhpAlias(PhpValue.Null);
            public override PhpArray ToArray(ref PhpValue me) => PhpArray.New(me);
            public override string DisplayString(ref PhpValue me) => me.Boolean ? PhpVariable.True : PhpVariable.False;
            public override void Output(ref PhpValue me, Context ctx) => ctx.Echo(me.Boolean);
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.Accept(me.Boolean);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class StringTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.String;
            public override object ToClass(ref PhpValue me) => new stdClass(me);	// new stdClass(){ $scalar = VALUE }
            public override string ToStringQuiet(ref PhpValue me) => me.String;
            public override string ToString(ref PhpValue me, Context ctx) => me.String;
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => me.String;
            public override long ToLong(ref PhpValue me) => Convert.StringToLongInteger(me.String);
            public override long ToLongOrThrow(ref PhpValue me) => Convert.ToLongOrThrow(me.String);
            public override double ToDouble(ref PhpValue me) => Convert.StringToDouble(me.String);
            public override bool ToBoolean(ref PhpValue me) => Convert.ToBoolean(me.String);
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number) => Convert.ToNumber(me.String, out number);
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) { key = Core.Convert.StringToArrayKey(me.String); return true; }
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => Operators.GetEmptyForeachEnumerator();
            public override int Compare(ref PhpValue me, PhpValue right) => Comparison.Compare(me.String, right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right) => right.IsString(out var sy) && sy == me.String;
            public override object EnsureObject(ref PhpValue me)
            {
                var obj = ToClass(ref me);

                // me is changed if value is empty
                if (string.IsNullOrEmpty(me.String))
                    me = PhpValue.FromClass(obj);

                return obj;
            }
            public override IPhpArray EnsureArray(ref PhpValue me)
            {
                var str = new PhpString(me.String); // upgrade to mutable string
                var arr = str.EnsureWritable();     // ensure its internal blob

                me = PhpValue.Create(str);          // copy back new value

                //
                return arr;
            }
            public override IPhpArray GetArrayAccess(ref PhpValue me) => EnsureArray(ref me);
            public override PhpValue GetArrayItem(ref PhpValue me, PhpValue index, bool quiet)
            {
                var item = Operators.GetItemValue(me.String, index, quiet);

                if (quiet && string.IsNullOrEmpty(item))
                {
                    return PhpValue.Null;
                }

                return (PhpValue)item;
            }
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) { throw new NotSupportedException(); } // TODO: Err
            public override PhpArray ToArray(ref PhpValue me) => PhpArray.New(me);
            public override IPhpCallable AsCallable(ref PhpValue me, RuntimeTypeHandle callerCtx, object callerObj) => PhpCallback.Create(me.String, callerCtx, callerObj);
            public override string DisplayString(ref PhpValue me) => "'" + me.String + "'";
            public override void Output(ref PhpValue me, Context ctx) => ctx.Echo(me.String);
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.Accept(me.String);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class MutableStringTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.MutableString;
            public override object ToClass(ref PhpValue me) => new stdClass(DeepCopy(ref me));	// new stdClass(){ $scalar = VALUE }
            public override string ToStringQuiet(ref PhpValue me) => me.MutableString.ToString();
            public override string ToString(ref PhpValue me, Context ctx) => me.MutableString.ToString(ctx);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => me.MutableString.ToStringOrThrow(ctx);
            public override long ToLong(ref PhpValue me) => me.MutableString.ToLong();
            public override long ToLongOrThrow(ref PhpValue me) => Convert.ToLongOrThrow(me.MutableString.ToString());
            public override double ToDouble(ref PhpValue me) => me.MutableString.ToDouble();
            public override bool ToBoolean(ref PhpValue me) => me.MutableString.ToBoolean();
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number) => me.MutableString.ToNumber(out number);
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) { key = Core.Convert.StringToArrayKey(me.MutableString.ToString()); return true; }
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => Operators.GetEmptyForeachEnumerator();
            public override int Compare(ref PhpValue me, PhpValue right) => Comparison.Compare(me.MutableString.ToString(), right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right) => right.IsString(out var sy) && sy.Length == me.MutableStringBlob.Length && sy == me.MutableStringBlob.ToString();
            public override object EnsureObject(ref PhpValue me)
            {
                //var obj = PhpValue.Create(new stdClass(ctx));
                //if (me.MutableString.IsEmpty)
                //{
                //    // me is changed if value is empty
                //    me = obj;
                //}
                //return obj;
                throw new NotImplementedException();
            }
            public override PhpAlias EnsureAlias(ref PhpValue me)
            {
                // ensure blob is lazily copied
                if (me.MutableStringBlob.IsShared)
                {
                    me._obj.blob = me.MutableStringBlob.ReleaseOne();
                }
                //
                return base.EnsureAlias(ref me);
            }
            public override IPhpArray EnsureArray(ref PhpValue me)
            {
                // ensure blob is lazily copied
                var blob = me.MutableStringBlob;
                if (blob.IsShared)
                {
                    me._obj.blob = blob = blob.ReleaseOne();
                }
                //
                return blob;
            }
            public override IPhpArray GetArrayAccess(ref PhpValue me) => me.MutableStringBlob;
            public override PhpValue GetArrayItem(ref PhpValue me, PhpValue index, bool quiet) => ((IPhpArray)me.MutableStringBlob).GetItemValue(index); // quiet);
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) { throw new NotSupportedException(); } // TODO: Err
            public override PhpValue DeepCopy(ref PhpValue me) => new PhpValue(me.MutableStringBlob.AddRef());
            public override void PassValue(ref PhpValue me) => me = new PhpValue(me.MutableStringBlob.AddRef());    // ~ DeepCopy
            public override PhpArray ToArray(ref PhpValue me) => me.MutableString.ToArray();
            public override IPhpCallable AsCallable(ref PhpValue me, RuntimeTypeHandle callerCtx, object callerObj) => PhpCallback.Create(me.MutableStringBlob.ToString(Encoding.UTF8), callerCtx, callerObj);
            public override string DisplayString(ref PhpValue me) => "'" + me.MutableStringBlob.ToString(Encoding.UTF8) + "'";
            public override void Output(ref PhpValue me, Context ctx) => me.MutableStringBlob.Output(ctx);
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.Accept(me.MutableString);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class ClassTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Object;
            public override bool IsEmpty(ref PhpValue me) => !Convert.ToBoolean(me.Object);
            public override object ToClass(ref PhpValue me) => (me.Object is IPhpConvertible conv) ? conv.ToClass() : me.Object;
            public override string ToStringQuiet(ref PhpValue me) => me.Object.ToString();
            public override string ToString(ref PhpValue me, Context ctx) => ToStringOrThrow(ref me, ctx);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => Convert.ToStringOrThrow(me.Object, ctx);
            public override long ToLong(ref PhpValue me)
            {
                if (me.Object is IPhpConvertible)
                {
                    return ((IPhpConvertible)me.Object).ToLong();
                }
                else
                {
                    PhpException.Throw(PhpError.Notice, string.Format(Resources.ErrResources.object_could_not_be_converted, me.Object.GetType().Name, PhpVariable.TypeNameInt));
                    return 1L;
                }
            }
            public override long ToLongOrThrow(ref PhpValue me)
            {
                if (me.Object is IPhpConvertible)
                {
                    return ((IPhpConvertible)me.Object).ToLong();
                }
                else
                {
                    throw PhpException.TypeErrorException(string.Format(Resources.ErrResources.object_could_not_be_converted, me.Object.GetType().Name, PhpVariable.TypeNameInt));
                }
            }
            public override double ToDouble(ref PhpValue me)
            {
                if (me.Object is IPhpConvertible)
                {
                    return ((IPhpConvertible)me.Object).ToDouble();
                }
                else
                {
                    PhpException.Throw(PhpError.Notice, string.Format(Resources.ErrResources.object_could_not_be_converted, me.Object.GetType().Name, PhpVariable.TypeNameDouble));
                    return 1.0;
                }
            }
            public override bool ToBoolean(ref PhpValue me) => Convert.ToBoolean(me.Object);
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                if (me.Object is IPhpConvertible)
                {
                    return ((IPhpConvertible)me.Object).ToNumber(out number);
                }
                else
                {
                    PhpException.Throw(PhpError.Notice, string.Format(Resources.ErrResources.object_could_not_be_converted, me.Object.GetType().Name, PhpVariable.TypeNameInt));
                    number = PhpNumber.Create(1L);
                    return Convert.NumberInfo.LongInteger;
                }
            }
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) { key = default(IntStringKey); return false; }
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => Operators.GetForeachEnumerator(me.Object, aliasedValues, caller);
            public override int Compare(ref PhpValue me, PhpValue right) => Comparison.Compare(me.Object, right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right)
            {
                return right.Object == me.Object || (right.Object is PhpAlias alias && alias.Value.Object == me.Object);
            }
            public override object EnsureObject(ref PhpValue me) => me.Object;
            public override IPhpArray EnsureArray(ref PhpValue me) => Operators.EnsureArray(me.Object);
            public override IPhpArray GetArrayAccess(ref PhpValue me) => Operators.EnsureArray(me.Object);
            public override PhpValue GetArrayItem(ref PhpValue me, PhpValue index, bool quiet)
            {
                // IPhpArray.GetItemValue
                if (me.Object is IPhpArray arr)
                {
                    return arr.GetItemValue(index); // , quiet);
                }

                // ArrayAccess.offsetGet()
                if (me.Object is ArrayAccess arracces)
                {
                    return arracces.offsetGet(index);
                }

                // IList[]
                if (me.Object is System.Collections.IList list)
                {
                    var key = index.ToIntStringKey();
                    if (key.IsInteger)
                    {
                        if (key.Integer >= 0 && key.Integer < list.Count)
                        {
                            return PhpValue.FromClr(list[index.ToIntStringKey().Integer]);
                        }
                        else if (!quiet)
                        {
                            PhpException.Throw(PhpError.Error, Resources.ErrResources.undefined_offset, key.Integer.ToString());
                        }
                    }
                    else if (!quiet)
                    {
                        PhpException.Throw(PhpError.Warning, Resources.ErrResources.illegal_offset_type);
                    }

                    return PhpValue.Void;
                }

                //
                if (!quiet)
                {
                    PhpException.Throw(PhpError.Error, Resources.ErrResources.object_used_as_array, me.Object.GetPhpTypeInfo().Name);
                }

                //
                return PhpValue.Void;
            }
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet)
            {
                if (me.Object is IPhpArray arr)
                {
                    return Operators.EnsureItemAlias(arr, index, quiet);
                }

                if (!quiet) // NOTE: PHP does not report this error (?)
                {
                    PhpException.Throw(PhpError.Error, Resources.ErrResources.object_used_as_array, me.Object.GetPhpTypeInfo().Name);
                }

                return new PhpAlias(PhpValue.Null);
            }
            public override PhpArray ToArray(ref PhpValue me) => Convert.ClassToArray(me.Object);
            //public override PhpArray ArrayOrThrow(ref PhpValue me)
            //{
            //    // TODO: review
            //    //if (me.Object is IPhpConvertible conv)
            //    //{
            //    //    return conv.ToArray();
            //    //}

            //    return base.ArrayOrThrow(ref me);
            //}
            public override object AsObject(ref PhpValue me) => me.Object;
            public override IPhpCallable AsCallable(ref PhpValue me, RuntimeTypeHandle callerCtx, object callerObj)
            {
                var obj = me.Object;

                if (obj is IPhpCallable callable) return callable;  // classes with __invoke() magic method implements IPhpCallable
                if (obj is Delegate d) return RoutineInfo.CreateUserRoutine(d.GetMethodInfo().Name, d);

                return PhpCallback.CreateInvalid();
            }
            public override string DisplayString(ref PhpValue me)
            {
                var obj = me.Object;
                if (obj is PhpResource resource)
                {
                    return $"resource id='{resource.Id}' type='{resource.TypeName}'";
                }
                else
                {
                    return obj.GetPhpTypeInfo().Name + "#" + obj.GetHashCode().ToString("X");
                }
            }
            public override void Output(ref PhpValue me, Context ctx) => ctx.Echo(Convert.ToStringOrThrow(me.Object, ctx));
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.AcceptObject(me.Object);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class ArrayTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.PhpArray;
            public override bool IsEmpty(ref PhpValue me) => me.Array.IsEmpty();
            public override object ToClass(ref PhpValue me) => me.Array.ToObject();
            public override string ToStringQuiet(ref PhpValue me) => PhpArray.PrintablePhpTypeName;
            public override string ToString(ref PhpValue me, Context ctx) => ((IPhpConvertible)me.Array).ToStringOrThrow(ctx);          // TODO: explicit or implicit?
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => ((IPhpConvertible)me.Array).ToStringOrThrow(ctx);   // TODO: explicit or implicit?
            public override string AsString(ref PhpValue me, Context ctx) => ((IPhpConvertible)me.Array).ToStringOrThrow(ctx);          // TODO: explicit or implicit?
            public override long ToLong(ref PhpValue me) => ((IPhpConvertible)me.Array).ToLong();       // TODO: explicit or implicit?
            public override long ToLongOrThrow(ref PhpValue me) => throw PhpException.TypeErrorException();
            public override double ToDouble(ref PhpValue me) => ((IPhpConvertible)me.Array).ToDouble(); // TODO: explicit or implicit?
            public override bool ToBoolean(ref PhpValue me) => ((IPhpConvertible)me.Array).ToBoolean(); // TODO: explicit or implicit?
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number) => ((IPhpConvertible)me.Array).ToNumber(out number); // TODO: explicit or implicit?
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) { key = default(IntStringKey); return false; }
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => me.Array.GetForeachEnumerator(aliasedValues);
            public override int Compare(ref PhpValue me, PhpValue right) => me.Array.Compare(right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right) => me.Array.StrictCompareEq(right.ArrayOrNull());
            public override object EnsureObject(ref PhpValue me) => ToClass(ref me);    // me is not modified
            public override IPhpArray EnsureArray(ref PhpValue me) => me.Array; // EnsureWritable() called lazily when writing
            public override PhpAlias EnsureAlias(ref PhpValue me)
            {
                // ensure array is lazily copied
                me.Array.EnsureWritable();
                //
                return base.EnsureAlias(ref me);
            }
            public override IPhpArray GetArrayAccess(ref PhpValue me) => me.Array;
            public override PhpValue GetArrayItem(ref PhpValue me, PhpValue index, bool quiet) => me.Array.GetItemValue(index); // , quiet);
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => me.Array.EnsureItemAlias(index, quiet);
            public override PhpValue DeepCopy(ref PhpValue me) => new PhpValue(me.Array.DeepCopy());
            public override void PassValue(ref PhpValue me) => me = new PhpValue(me.Array.DeepCopy());
            public override PhpArray ToArray(ref PhpValue me)
            {
                me.Array.EnsureWritable();
                return me.Array;
            }
            public override PhpArray ArrayOrThrow(ref PhpValue me) => me.Array;
            public override IPhpCallable AsCallable(ref PhpValue me, RuntimeTypeHandle callerCtx, object callerObj)
            {
                if (me.Array.Count == 2)
                {
                    if (me.Array.TryGetValue(0, out var obj) &&
                        me.Array.TryGetValue(1, out var method))
                    {
                        // [ class => object|string, methodname => string ]
                        return PhpCallback.Create(obj, method, callerCtx, callerObj);
                    }
                }

                // invalid
                return base.AsCallable(ref me, callerCtx, callerObj);
            }
            public override string DisplayString(ref PhpValue me) => "array(length = " + me.Array.Count.ToString() + ")";
            public override void Output(ref PhpValue me, Context ctx) => ctx.Echo((string)me.Array);
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.Accept(me.Array);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class AliasTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Alias;
            public override bool IsNull(ref PhpValue me) => me.Alias.Value.IsNull;
            public override bool IsEmpty(ref PhpValue me) => me.Alias.Value.IsEmpty;
            public override object ToClass(ref PhpValue me) => me.Alias.ToClass();
            public override string ToStringQuiet(ref PhpValue me) => me.Alias.Value.ToString();
            public override string ToString(ref PhpValue me, Context ctx) => me.Alias.ToString(ctx);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => me.Alias.ToStringOrThrow(ctx);
            public override long ToLong(ref PhpValue me) => me.Alias.ToLong();
            public override long ToLongOrThrow(ref PhpValue me) => me.Alias.Value.ToLongOrThrow();
            public override double ToDouble(ref PhpValue me) => me.Alias.ToDouble();
            public override bool ToBoolean(ref PhpValue me) => me.Alias.ToBoolean();
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number) => me.Alias.ToNumber(out number);
            public override bool TryToIntStringKey(ref PhpValue me, out IntStringKey key) => me.Alias.Value.TryToIntStringKey(out key);
            public override IPhpEnumerator GetForeachEnumerator(ref PhpValue me, bool aliasedValues, RuntimeTypeHandle caller) => me.Alias.Value.GetForeachEnumerator(aliasedValues, caller);
            public override int Compare(ref PhpValue me, PhpValue right) => me.Alias.Value.Compare(right);
            public override bool StrictEquals(ref PhpValue me, PhpValue right) => me.Alias.Value.StrictEquals(right);
            public override object EnsureObject(ref PhpValue me) => me.Alias.Value.EnsureObject();
            public override IPhpArray EnsureArray(ref PhpValue me) => me.Alias.Value.EnsureArray();
            public override IPhpArray GetArrayAccess(ref PhpValue me) => me.Alias.Value.GetArrayAccess();
            public override PhpAlias EnsureAlias(ref PhpValue me) => me.Alias.AddRef();
            public override PhpValue GetArrayItem(ref PhpValue me, PhpValue index, bool quiet) => me.Alias.Value.GetArrayItem(index, quiet);
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => me.Alias.Value.EnsureItemAlias(index, quiet);
            public override void PassValue(ref PhpValue me) => me = me.Alias.Value.DeepCopy();
            public override PhpValue DeepCopy(ref PhpValue me) => me.Alias.DeepCopy();
            public override PhpArray ToArray(ref PhpValue me) => me.Alias.Value.ToArray();
            public override PhpArray ArrayOrThrow(ref PhpValue me) => me.Alias.Value.ToArrayOrThrow();
            public override object AsObject(ref PhpValue me) => me.Alias.Value.AsObject();
            public override IPhpCallable AsCallable(ref PhpValue me, RuntimeTypeHandle callerCtx, object callerObj) => me.Alias.Value.AsCallable(callerCtx, callerObj);
            public override string DisplayString(ref PhpValue me) => "&" + me.Alias.Value.DisplayString;
            public override void Output(ref PhpValue me, Context ctx) => me.Alias.Value.Output(ctx);
            public override void Accept(ref PhpValue me, PhpVariableVisitor visitor) => visitor.Accept(me.Alias);
        }
    }
}
