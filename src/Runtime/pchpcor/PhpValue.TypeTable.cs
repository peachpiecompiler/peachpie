using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial struct PhpValue
    {
        abstract class TypeTable
        {
            public static readonly NullTable NullTable = new NullTable();
            public static TypeTable VoidTable => NullTable;
            public static readonly LongTable LongTable = new LongTable();
            public static readonly DoubleTable DoubleTable = new DoubleTable();
            public static readonly BoolTable BoolTable = new BoolTable();
            public static readonly StringTable StringTable = new StringTable();
            public static readonly TypeTable WritableStringTable = new WritableStringTable();
            public static readonly ClassTable ClassTable = new ClassTable();
            public static readonly AliasTable AliasTable = new AliasTable();

            public abstract PhpTypeCode Type { get; }
			public abstract bool IsNull { get; }
            public abstract object ToClass(ref PhpValue me, Context ctx);
            public abstract string ToString(ref PhpValue me, Context ctx);
            public abstract string ToStringOrThrow(ref PhpValue me, Context ctx);
            public abstract long ToLong(ref PhpValue me);
            public abstract double ToDouble(ref PhpValue me);
            public abstract bool ToBoolean(ref PhpValue me);
            public abstract Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number);
            public abstract string DisplayString(ref PhpValue me);
        }

		sealed class NullTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Object;
            public override bool IsNull => true;
            public override object ToClass(ref PhpValue me, Context ctx) { throw new NotImplementedException(); }	// new stdClass()
            public override string ToString(ref PhpValue me, Context ctx) => string.Empty;
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => string.Empty;
            public override long ToLong(ref PhpValue me) => 0;
            public override double ToDouble(ref PhpValue me) => 0.0;
            public override bool ToBoolean(ref PhpValue me) => false;
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                number = PhpNumber.Create(0L);
                return Convert.NumberInfo.Unconvertible;
            }
            public override string DisplayString(ref PhpValue me) => "NULL";
        }

        sealed class LongTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Long;
            public override bool IsNull => false;
            public override object ToClass(ref PhpValue me, Context ctx) { throw new NotImplementedException(); }	// new stdClass(){ $scalar = VALUE }
            public override string ToString(ref PhpValue me, Context ctx) => me.Long.ToString();
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => me.Long.ToString();
            public override long ToLong(ref PhpValue me) => me.Long;
            public override double ToDouble(ref PhpValue me) => (double)me.Long;
            public override bool ToBoolean(ref PhpValue me) => me.Long != 0;
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                number = PhpNumber.Create(me.Long);
                return Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger;
            }
            public override string DisplayString(ref PhpValue me) => me.Long.ToString();
        }

        sealed class DoubleTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Double;
            public override bool IsNull => false;
            public override object ToClass(ref PhpValue me, Context ctx) { throw new NotImplementedException(); }	// new stdClass(){ $scalar = VALUE }
            public override string ToString(ref PhpValue me, Context ctx) => Convert.ToString(me.Double, ctx);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => Convert.ToString(me.Double, ctx);
            public override long ToLong(ref PhpValue me) => (long)me.Double;
            public override double ToDouble(ref PhpValue me) => me.Double;
            public override bool ToBoolean(ref PhpValue me) => me.Double != 0;
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                number = PhpNumber.Create(me.Double);
                return Convert.NumberInfo.IsNumber | Convert.NumberInfo.Double;
            }
            public override string DisplayString(ref PhpValue me) => me.Double.ToString();
        }

        sealed class BoolTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Boolean;
            public override bool IsNull => false;
            public override object ToClass(ref PhpValue me, Context ctx) { throw new NotImplementedException(); }	// new stdClass(){ $scalar = VALUE }
            public override string ToString(ref PhpValue me, Context ctx) => Convert.ToString(me.Boolean);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => Convert.ToString(me.Boolean);
            public override long ToLong(ref PhpValue me) => me.Boolean ? 1L : 0L;
            public override double ToDouble(ref PhpValue me) => me.Boolean ? 1.0 : 0.0;
            public override bool ToBoolean(ref PhpValue me) => me.Boolean;
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                number = PhpNumber.Create(me.Boolean ? 1L : 0L);
                return Convert.NumberInfo.IsNumber | Convert.NumberInfo.LongInteger;
            }
            public override string DisplayString(ref PhpValue me) => me.Boolean ? "TRUE" : "FALSE";
        }

        sealed class StringTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.String;
            public override bool IsNull => false;
            public override object ToClass(ref PhpValue me, Context ctx) { throw new NotImplementedException(); }	// new stdClass(){ $scalar = VALUE }
            public override string ToString(ref PhpValue me, Context ctx) => me.String;
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => me.String;
            public override long ToLong(ref PhpValue me) => Convert.StringToLongInteger(me.String);
            public override double ToDouble(ref PhpValue me) => Convert.StringToDouble(me.String);
            public override bool ToBoolean(ref PhpValue me) => Convert.ToBoolean(me.String);
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number) => Convert.ToNumber(me.String, out number);
            public override string DisplayString(ref PhpValue me) => $"'{me.String}'";
        }

        sealed class WritableStringTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.WritableString;
            public override bool IsNull => false;
            public override object ToClass(ref PhpValue me, Context ctx) { throw new NotImplementedException(); }	// new stdClass(){ $scalar = VALUE }
            public override string ToString(ref PhpValue me, Context ctx) => me.WritableString.ToString(ctx);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => me.WritableString.ToStringOrThrow(ctx);
            public override long ToLong(ref PhpValue me) => me.WritableString.ToLong();
            public override double ToDouble(ref PhpValue me) => me.WritableString.ToDouble();
            public override bool ToBoolean(ref PhpValue me) => me.WritableString.ToBoolean();
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number) => me.WritableString.ToNumber(out number);
            public override string DisplayString(ref PhpValue me) => $"'{me.WritableString.ToString()}'";
        }

        sealed class ClassTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Object;
            public override bool IsNull => false;
            public override object ToClass(ref PhpValue me, Context ctx) => me.Object;
            public override string ToString(ref PhpValue me, Context ctx)
            {
                if (me.Object is IPhpConvertible) return ((IPhpConvertible)me.Object).ToString(ctx);
                throw new NotImplementedException();
            }
            public override string ToStringOrThrow(ref PhpValue me, Context ctx)
            {
                if (me.Object is IPhpConvertible) return ((IPhpConvertible)me.Object).ToStringOrThrow(ctx);
                throw new NotImplementedException();
            }
            public override long ToLong(ref PhpValue me)
            {
                if (me.Object is IPhpConvertible) return ((IPhpConvertible)me.Object).ToLong();
                throw new NotImplementedException();
            }
            public override double ToDouble(ref PhpValue me)
            {
                if (me.Object is IPhpConvertible) return ((IPhpConvertible)me.Object).ToDouble();
                throw new NotImplementedException();
            }
            public override bool ToBoolean(ref PhpValue me)
            {
                if (me.Object is IPhpConvertible) return ((IPhpConvertible)me.Object).ToBoolean();
                throw new NotImplementedException();
            }
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number)
            {
                if (me.Object is IPhpConvertible) return ((IPhpConvertible)me.Object).ToNumber(out number);
                throw new NotImplementedException();
            }
            public override string DisplayString(ref PhpValue me) => me.Object.GetType().FullName + "#" + me.Object.GetHashCode().ToString("X");
        }

        sealed class AliasTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Alias;
            public override bool IsNull => false;
            public override object ToClass(ref PhpValue me, Context ctx) => me.Alias.ToClass(ctx);
            public override string ToString(ref PhpValue me, Context ctx) => me.Alias.ToString(ctx);
            public override string ToStringOrThrow(ref PhpValue me, Context ctx) => me.Alias.ToStringOrThrow(ctx);
            public override long ToLong(ref PhpValue me) => me.Alias.ToLong();
            public override double ToDouble(ref PhpValue me) => me.Alias.ToDouble();
            public override bool ToBoolean(ref PhpValue me) => me.Alias.ToBoolean();
            public override Convert.NumberInfo ToNumber(ref PhpValue me, out PhpNumber number) => me.Alias.ToNumber(out number);
            public override string DisplayString(ref PhpValue me) => "&" + me.Alias.Value.DisplayString;
        }
    }
}
