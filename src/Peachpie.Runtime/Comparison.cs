using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core
{
    /// <summary>
    /// Defines comparison methods which are used to compare PHP.NET types.
    /// </summary>
    public interface IPhpComparable
    {
        /// <summary>
        /// Compares the current instance with another object using default comparer.
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>Negative, Positive or Zero.</returns>
        /// <exception cref="ArgumentException">Incomparable objects have been compared.</exception>
		int Compare(PhpValue obj);
    }

    /// <summary>
    /// PHP comparison semantic.
    /// </summary>
    public static class Comparison
    {
        static Exception InvalidTypeCodeException(string op, string left, string right)
            => new InvalidOperationException($"{op}({left}, {right})");

        public static bool Clt(long lx, double dy) => (double)lx < dy;
        public static bool Cgt(long lx, double dy) => (double)lx > dy;
        public static bool Ceq(long lx, double dy) => (double)lx == dy;

        public static bool Ceq(long lx, bool by) => (lx != 0) == by;
        public static bool Ceq(long lx, string sy) => Equals(sy, lx);
        public static bool Ceq(double dx, string sy) => Equals(sy, dx);
        public static bool Ceq(string sx, long ly) => Equals(sx, ly);
        public static bool Ceq(string sx, double dy) => Equals(sx, dy);
        public static bool Ceq(string sx, bool by) => Convert.ToBoolean(sx) == by;
        public static bool Ceq(string sx, string sy)
        {
            var info_x = Convert.StringToNumber(sx, out var lx, out var dx);

            // an operand is not entirely convertable to numbers => string comparison is performed:
            if ((info_x & Convert.NumberInfo.IsNumber) == 0)
            {
                return sx == sy;
            }

            var info_y = Convert.StringToNumber(sy, out var ly, out var dy);

            // an operand is not entirely convertable to numbers => string comparison is performed:
            if ((info_y & Convert.NumberInfo.IsNumber) == 0)
            {
                return sx == sy;
            }

            // numeric comparison
            return (((info_x | info_y) & Convert.NumberInfo.Double) != 0)
                ? (dx == dy)   // at least one operand has been converted to double:
                : (lx == ly);  // compare integers:
        }

        public static bool Ceq(string sx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Boolean: return Convert.ToBoolean(sx) == y.Boolean;
                case PhpTypeCode.Long: return Compare(sx, y.Long) == 0;
                case PhpTypeCode.Double: return Compare(sx, y.Double) == 0;
                case PhpTypeCode.PhpArray: return false;
                case PhpTypeCode.String: return Ceq(sx, y.String);
                case PhpTypeCode.MutableString: return Ceq(sx, y.MutableString.ToString());
                case PhpTypeCode.Object: return CompareStringToObject(sx, y.Object) == 0;
                case PhpTypeCode.Alias: return Ceq(sx, y.Alias.Value);
                case PhpTypeCode.Null: return sx.Length == 0;
            }

            throw InvalidTypeCodeException("ceq", PhpVariable.TypeNameString, PhpVariable.GetDebugType(y));
        }

        public static bool CeqNull(PhpValue x)
        {
            switch (x.TypeCode)
            {
                case PhpTypeCode.Null:
                    return true;

                case PhpTypeCode.String:
                    return x.String.Length == 0;

                case PhpTypeCode.MutableString:
                    return x.MutableString.IsEmpty;

                case PhpTypeCode.Alias:
                    return CeqNull(x.Alias.Value);

                default:
                    return x.IsEmpty;
            }
        }

        public static int Compare(long lx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Null: return (lx == 0) ? 0 : 1;
                case PhpTypeCode.Boolean: return Compare(lx != 0, y.Boolean);
                case PhpTypeCode.Long: return Compare(lx, y.Long);
                case PhpTypeCode.Double: return Compare((double)lx, y.Double);
                case PhpTypeCode.PhpArray: return -1;
                case PhpTypeCode.String: return -Compare(y.String, lx);
                case PhpTypeCode.MutableString: return -Compare(y.MutableString.ToString(), lx);
                case PhpTypeCode.Object:
                    Debug.Assert(y.Object != null);

                    // we cannot use ToNumber(object) because it treats some builtin classes as numbers
                    if (y.Object is decimal d)
                    {
                        return Compare((double)lx, (double)d);
                    }

                    // Notice: Object of class {0} could not be converted to int
                    PhpException.Throw(PhpError.Notice, string.Format(Resources.ErrResources.object_could_not_be_converted, PhpVariable.GetDebugType(y), PhpVariable.TypeNameInt));
                    return Compare(lx, 1L); // object is treated as '1'
                case PhpTypeCode.Alias: return Compare(lx, y.Alias.Value);
            }

            throw InvalidTypeCodeException("compare", PhpVariable.TypeNameInt, PhpVariable.GetDebugType(y));
        }

        public static int Compare(double dx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Null: return (dx == 0.0) ? 0 : 1;
                case PhpTypeCode.Boolean: return Compare(dx != 0.0, y.Boolean);
                case PhpTypeCode.Long: return Compare(dx, (double)y.Long);
                case PhpTypeCode.Double: return Compare(dx, y.Double);
                case PhpTypeCode.PhpArray: return -1;
                case PhpTypeCode.String: return -Compare(y.String, dx);
                case PhpTypeCode.MutableString: return -Compare(y.MutableString.ToString(), dx);
                case PhpTypeCode.Object:
                    // we cannot use ToNumber(object) because it treats some builtin classes as numbers
                    if (y.Object is decimal yd)
                    {
                        return Compare(dx, (double)yd);
                    }
                    // Notice: Object of class {0} could not be converted to int
                    PhpException.Throw(PhpError.Notice, string.Format(Resources.ErrResources.object_could_not_be_converted, PhpVariable.GetDebugType(y), PhpVariable.TypeNameDouble));
                    return Compare(dx, 1.0); // object is treated as '1'
                case PhpTypeCode.Alias: return Compare(dx, y.Alias.Value);
            }

            throw InvalidTypeCodeException("compare", PhpVariable.TypeNameDouble, PhpVariable.GetDebugType(y));
        }

        public static int Compare(bool bx, PhpValue y) => Compare(bx, y.ToBoolean());

        public static int Compare(bool bx, bool by) => (bx ? 2 : 1) - (by ? 2 : 1);

        public static int Compare(string sx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Null: return (sx.Length == 0) ? 0 : 1;
                case PhpTypeCode.Boolean: return Compare(Convert.ToBoolean(sx), y.Boolean);
                case PhpTypeCode.Long: return Compare(sx, y.Long);
                case PhpTypeCode.Double: return Compare(sx, y.Double);
                case PhpTypeCode.PhpArray: return -1;   // - 1 * (array.CompareTo(string))
                case PhpTypeCode.String: return Compare(sx, y.String);
                case PhpTypeCode.MutableString: return Compare(sx, y.MutableString.ToString());
                case PhpTypeCode.Object: return CompareStringToObject(sx, y.Object);
                case PhpTypeCode.Alias: return Compare(sx, y.Alias.Value);
            }

            throw InvalidTypeCodeException("compare", PhpVariable.TypeNameString, PhpVariable.GetDebugType(y));
        }

        public static int Compare(PhpString.Blob sx, PhpValue y)
        {
            switch (y.TypeCode)
            {
                case PhpTypeCode.Null: return (sx.Length == 0) ? 0 : 1;
                case PhpTypeCode.Boolean: return Compare(sx.ToBoolean(), y.Boolean);
                case PhpTypeCode.Long: return Compare(sx.ToString(), y.Long);
                case PhpTypeCode.Double: return Compare(sx.ToString(), y.Double);
                case PhpTypeCode.PhpArray: return -1;   // - 1 * (array.CompareTo(string))
                case PhpTypeCode.String: return Compare(sx.ToString(), y.String);
                case PhpTypeCode.MutableString: return Compare(sx, y.MutableStringBlob);
                case PhpTypeCode.Object: return CompareStringToObject(sx.ToString(), y.Object);
                case PhpTypeCode.Alias: return Compare(sx, y.Alias.Value);
            }

            throw InvalidTypeCodeException("compare", PhpVariable.TypeNameString, PhpVariable.GetDebugType(y));
        }

        static int CompareStringToObject(string sx, object y)
        {
            Debug.Assert(y != null);

            if (y.GetPhpTypeInfo().RuntimeMethods[TypeMethods.MagicMethods.__tostring] != null)
            {
                // __toString is eventually called from ToString
                return Compare(sx, y.ToString());
            }
            else if (y is decimal yd)
            {
                return Compare(sx, (double)yd);
            }
            else
            {
                // If not convertible to string (it must contain the __toString method), the object is always greater
                return -1;
            }
        }

        public static int Compare(object x, PhpValue y)
        {
            Debug.Assert(x != null);

            if (x.Equals(y.Object)) return 0;
            if (x is IPhpComparable) return ((IPhpComparable)x).Compare(y);
            if (y.Object is IPhpComparable) return -((IPhpComparable)y.Object).Compare(PhpValue.FromClass(x));
            if (x is decimal xd) return Compare((double)xd, y);

            switch (y.TypeCode)
            {
                case PhpTypeCode.Null:
                    return 1;

                case PhpTypeCode.Boolean:
                    return y.Boolean ? 0 : 1;

                case PhpTypeCode.Long:
                    // Notice: Object of class {0} could not be converted to int
                    PhpException.Throw(PhpError.Notice, string.Format(Resources.ErrResources.object_could_not_be_converted, PhpVariable.GetDebugType(y), PhpVariable.TypeNameInt));
                    return Compare(1L, y.Long);

                case PhpTypeCode.Double:
                    // Notice: Object of class {0} could not be converted to float
                    PhpException.Throw(PhpError.Notice, string.Format(Resources.ErrResources.object_could_not_be_converted, PhpVariable.GetDebugType(y), PhpVariable.TypeNameFloat));
                    return Compare(1L, y.Long);

                case PhpTypeCode.String:
                    return -CompareStringToObject(y.String, x);

                case PhpTypeCode.Object:
                    Debug.Assert(y.Object != null);
                    var result = CompareObjects(x, y.Object, PhpComparer.Default, out var incomparable);
                    if (incomparable)
                    {
                        PhpException.Throw(PhpError.Warning,
                            Resources.ErrResources.incomparable_objects_compared_exception,
                            x.GetPhpTypeInfo().Name,
                            y.Object.GetPhpTypeInfo().Name);
                        return 1;
                    }
                    return result;

                case PhpTypeCode.Alias:
                    return Compare(x, y.Alias.Value);

                default:
                    PhpException.Throw(PhpError.Notice,
                        string.Format(Resources.ErrResources.incomparable_objects_compared_exception,
                        x.GetPhpTypeInfo().Name,
                        PhpVariable.GetDebugType(y)));

                    return 0;
            }
        }

        /// <summary>
		/// Compares two class instances.
		/// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
		/// <param name="comparer">The comparer.</param>
		/// <param name="incomparable">
        /// Whether objects are incomparable while no difference is found before both objects enter an infinite recursion, returns zero.
        /// </param>
		static int CompareObjects(object x, object y, PhpComparer comparer, out bool incomparable)
        {
            HashSet<object> visited = null;
            return CompareObjects(x, y, comparer, out incomparable, ref visited);
        }

        static int CompareObjects(object x, object y, PhpComparer comparer, out bool incomparable, ref HashSet<object> visited)
        {
            Debug.Assert(x != null && y != null);

            incomparable = false;

            // check for same instance
            if (ReferenceEquals(x, y)) return 0;

            // check for different types
            var type_x = x.GetType();
            var type_y = y.GetType();
            if (type_x != type_y)
            {
                if (type_x.IsSubclassOf(type_y)) return -1;
                if (type_y.IsSubclassOf(type_x)) return 1;

                incomparable = true;
                return 1; // they really are incomparable
            }

            // same type
            var phpt = type_x.GetPhpTypeInfo();

            // TODO: check PhpReference

            // compare properties:
            // the comparison operation stops and returns at the first unequal property found

            //if (visited == null)
            //{
            //    visited = new HashSet<object>();
            //}

            //if (visited.Add(x) == false && visited.Add(y) == false)
            //{
            //    // both objects already visited, break the recursion
            //    return 0;
            //}

            // TODO: infinite recursion prevention

            foreach (var p in phpt.GetDeclaredProperties())
            {
                if (p.IsStatic)
                {
                    continue;
                }

                var val_x = p.GetValue(null, x);
                var val_y = p.GetValue(null, y);

                var result = comparer.Compare(val_x, val_y);
                if (result != 0) return result;
            }

            // compare runtime properties:
            var arr_x = phpt.GetRuntimeFields(x);
            var arr_y = phpt.GetRuntimeFields(y);

            var count_x = arr_x != null ? arr_x.Count : 0;
            var count_y = arr_y != null ? arr_y.Count : 0;

            if (count_x != 0)
            {
                if (count_y == 0) return count_x;

                var enumerator = arr_x.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    // TODO: if (!arr_y.TryGetValue(...)) ...
                    var result = comparer.Compare(enumerator.CurrentValue, arr_y[enumerator.CurrentKey]);
                    if (result != 0) return result;
                }
            }

            return count_x - count_y;
        }

        public static int CompareNull(PhpValue y) => y.TypeCode switch
        {
            PhpTypeCode.Null => 0,
            PhpTypeCode.Boolean => y.Boolean ? -1 : 0,
            PhpTypeCode.Long => y.Long == 0 ? 0 : -1,
            PhpTypeCode.Double => y.Double == 0 ? 0 : -1,
            PhpTypeCode.PhpArray => -y.Array.Count,
            PhpTypeCode.String => y.String.Length == 0 ? 0 : -1,
            PhpTypeCode.MutableString => y.MutableString.Length == 0 ? 0 : -1,
            PhpTypeCode.Object => -1, // CONSIDER: y.Object is decimal yd ? CompareNull((double)yd)
            PhpTypeCode.Alias => CompareNull(y.Alias.Value),
            _ => throw InvalidTypeCodeException("compare", PhpVariable.TypeNameNull, PhpVariable.GetDebugType(y))
        };

        public static int Compare(PhpNumber x, PhpValue y) => x.IsLong ? Compare(x.Long, y) : Compare(x.Double, y);

        public static int Compare(PhpArray x, PhpValue y) => x.Compare(y);

        public static int Compare(PhpValue x, PhpValue y) => x.TypeCode switch
        {
            PhpTypeCode.Null => CompareNull(y),
            PhpTypeCode.Boolean => Compare(x.Boolean, y),
            PhpTypeCode.Long => Compare(x.Long, y),
            PhpTypeCode.Double => Compare(x.Double, y),
            PhpTypeCode.PhpArray => Compare(x.Array, y),
            PhpTypeCode.String => Compare(x.String, y),
            PhpTypeCode.MutableString => Compare(x.MutableStringBlob, y),
            PhpTypeCode.Object => Compare(x.Object, y),
            PhpTypeCode.Alias => Compare(x.Alias.Value, y),
            _ => throw InvalidTypeCodeException("compare", PhpVariable.GetDebugType(x), PhpVariable.GetDebugType(y)),
        };

        public static int Compare(PhpValue x, long ly) => -Compare(ly, x);

        /// <summary>
		/// Compares two long integer values.
		/// </summary>
		/// <returns>(+1,0,-1)</returns>
        public static int Compare(long x, long y) => (x > y) ? +1 : (x < y ? -1 : 0);

        /// <summary>
		/// Compares two double values.
		/// </summary>
		/// <returns>(+1,0,-1)</returns>
		/// <remarks>We cannot used <see cref="Math.Sign(double)"/> on <c>x - y</c> since the result can be NaN.</remarks>
        public static int Compare(double x, double y) => (x > y) ? +1 : (x < y ? -1 : 0);

        /// <summary>
		/// Compares string in a manner of PHP. 
		/// </summary>
		/// <remarks>Note that this comparison is not transitive (e.g. {"2","10","10a"} leads to a contradiction).</remarks>
        public static int Compare(string sx, string sy)
        {
            var info_x = Convert.StringToNumber(sx, out var lx, out var dx);
            if ((info_x & Convert.NumberInfo.IsNumber) != 0)
            {
                var info_y = Convert.StringToNumber(sy, out var ly, out var dy);
                if ((info_y & Convert.NumberInfo.IsNumber) != 0)
                {
                    // numeric comparison
                    return (((info_x | info_y) & Convert.NumberInfo.Double) != 0)
                        ? Compare(dx, dy)   // at least one operand has been converted to double:
                        : Compare(lx, ly);  // compare integers:
                }
            }

            // an operand is not entirely convertable to numbers => string comparison is performed:
            return string.CompareOrdinal(sx, sy);
        }

        /// <summary>
		/// Compares string in a manner of PHP, same as <see cref="Compare(string, string)"/>.
		/// </summary>
		public static int Compare(PhpString.Blob sx, PhpString.Blob sy)
        {
            return Compare(sx.ToString(), sy.ToString()); // TODO: convert non-Unicode strings properly and efficiently // https://github.com/peachpiecompiler/peachpie/issues/775
        }

        /// <summary>
		/// Compares a <see cref="string"/> with <see cref="long"/>.
		/// </summary>
        public static int Compare(string/*!*/sx, long ly)
        {
            Debug.Assert(sx != null);

            return ((Convert.StringToNumber(sx, out var lx, out var dx) & Convert.NumberInfo.Double) != 0)
                ? Compare(dx, (double)ly)
                : Compare(lx, ly);
        }

        /// <summary>
        /// Compares a <see cref="string"/> with <see cref="double"/>.
        /// </summary>
        public static int Compare(string/*!*/sx, double dy)
        {
            Debug.Assert(sx != null);

            Convert.StringToNumber(sx, out _, out var dx);

            return Compare(dx, dy);
        }

        /// <summary>
		/// Compares two objects for equality in a manner of the PHP regular comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="ly">The second object.</param>
		/// <returns>Whether the values of operands are the same.</returns>
        public static bool Equals(string/*!*/x, long ly)
        {
            Debug.Assert(x != null);

            return ((Convert.StringToNumber(x, out var lx, out var dx) & Convert.NumberInfo.Double) != 0)
                ? dx == ly
                : lx == ly;
        }

        /// <summary>
		/// Compares two objects for equality in a manner of the PHP regular comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="dy">The second object.</param>
		/// <returns>Whether the values of operands are the same.</returns>
        public static bool Equals(string/*!*/ x, double dy)
        {
            Debug.Assert(x != null);

            Convert.StringToNumber(x, out _, out var dx);

            return dx == dy;
        }
    }

    /// <summary>
    /// PHP strict comparison semantic.
    /// </summary>
    public static class StrictComparison
    {
        public static bool Ceq(bool bx, PhpValue y) => y.IsBoolean(out var by) && bx == by;
        public static bool Ceq(long lx, PhpValue y) => y.IsLong(out var ly) && lx == ly;
        public static bool Ceq(long lx, PhpNumber y) => y.IsLong && lx == y.Long;
        public static bool Ceq(double dx, PhpValue y) => y.IsDouble(out var dy) && dx == dy;
        public static bool Ceq(double dx, PhpNumber y) => y.IsDouble && dx == y.Double;
        public static bool Ceq(string sx, PhpValue y)
        {
            y.IsString(out var sy);
            return sx == sy;
        }

        public static bool Ceq(PhpValue x, bool by) => x.IsBoolean(out var bx) && bx == by;
        public static bool Ceq(PhpValue x, string sy)
        {
            x.IsString(out var sx);
            return sx == sy;
        }

        public static bool Ceq(PhpValue x, PhpValue y) => x.StrictEquals(y);

        public static bool CeqNull(PhpValue x) => x.IsNull || (x.Object is PhpAlias alias && CeqNull(alias.Value));
    }
}
