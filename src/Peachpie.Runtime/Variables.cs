using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
	/// Represents enumerator which 
	/// </summary>
	public interface IPhpEnumerator : IEnumerator<KeyValuePair<PhpValue, PhpValue>>
    {
        /// <summary>
        /// Moves the enumerator to the last entry of the dictionary.
        /// </summary>
        /// <returns>Whether the enumerator has been sucessfully moved to the last entry.</returns>
        bool MoveLast();

        /// <summary>
        /// Moves the enumerator to the first entry of the dictionary.
        /// </summary>
        /// <returns>Whether the enumerator has been sucessfully moved to the first entry.</returns>
        bool MoveFirst();

        /// <summary>
        /// Moves the enumerator to the previous entry of the dictionary.
        /// </summary>
        /// <returns>Whether the enumerator has been sucessfully moved to the previous entry.</returns>
        bool MovePrevious();

        /// <summary>
        /// Gets whether the enumeration has ended and the enumerator points behind the last element.
        /// </summary>
        bool AtEnd { get; }

        /// <summary>
        /// Gets current unaliased value.
        /// </summary>
        PhpValue CurrentValue { get; }

        /// <summary>
        /// Aliases current entry value.
        /// </summary>
        PhpAlias CurrentValueAliased { get; }

        /// <summary>
        /// Gets current key.
        /// </summary>
        PhpValue CurrentKey { get; }
    }

    /// <summary>
    /// Provides methods which allows implementor to be used in PHP foreach statement as a source of enumeration.
    /// </summary>
    public interface IPhpEnumerable
    {
        /// <summary>
        /// Implementor's intrinsic enumerator which will be advanced during enumeration.
        /// </summary>
        IPhpEnumerator IntrinsicEnumerator { get; }

        /// <summary>
        /// Creates an enumerator used in foreach statement.
        /// </summary>
        /// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
        /// <param name="caller">Type of the class in whose context the caller operates.</param>
        /// <returns>The dictionary enumerator.</returns>
        IPhpEnumerator GetForeachEnumerator(bool aliasedValues, RuntimeTypeHandle caller);
    }

    /// <summary>
    /// The PHP array interface provides operations for an array access.
    /// </summary>
    public interface IPhpArray // TODO: : IPhpEnumerable, IPhpConvertible
    {
        /// <summary>
        /// Gets number of items in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets value at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// </summary>
        PhpValue GetItemValue(IntStringKey key);

        /// <summary>
        /// Sets value at specific index. Value must not be an alias.
        /// </summary>
        void SetItemValue(IntStringKey key, PhpValue value);

        /// <summary>
        /// Writes aliased value at given index.
        /// </summary>
        void SetItemAlias(IntStringKey key, PhpAlias alias);

        /// <summary>
        /// Add a value to the end of array.
        /// Value can be an alias.
        /// </summary>
        void AddValue(PhpValue value);

        /// <summary>
        /// Removes a value matching given key.
        /// In case the value is not found, the method does nothing.
        /// </summary>
        void RemoveKey(IntStringKey key);

        /// <summary>
        /// Ensures the item at given index is alias.
        /// </summary>
        PhpAlias EnsureItemAlias(IntStringKey key);

        /// <summary>
        /// Ensures the item at given index is class object.
        /// </summary>
        object EnsureItemObject(IntStringKey key);

        /// <summary>
        /// Ensures the item at given index is array.
        /// </summary>
        IPhpArray EnsureItemArray(IntStringKey key);
    }

    /// <summary>
    /// Visitor implementation for a variable.
    /// </summary>
    /// <remarks>Used for serialization, printing, dumping.</remarks>
    public class PhpVariableVisitor
    {
        public virtual void Accept(PhpValue obj) => obj.Accept(this);
        public virtual void Accept(bool obj) { }
        public virtual void Accept(long obj) { }
        public virtual void Accept(double obj) { }
        public virtual void Accept(string obj) { }
        public virtual void Accept(PhpString obj) { }
        public virtual void Accept(PhpArray obj)
        {
            var iterator = obj.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                AcceptArrayItem(iterator.Current);
            }
        }
        public virtual void AcceptArrayItem(KeyValuePair<IntStringKey, PhpValue> entry) => Accept(entry.Value);
        public virtual void Accept(PhpAlias obj) => Accept(obj.Value);
        public virtual void AcceptObject(object obj) { }
        public virtual void AcceptNull() { }
    }

    /// <summary>
    /// Provides method for serializing a variable into a stream.
    /// </summary>
    /// <remarks>Used for dumping, printing, export, serialization.</remarks>
    public interface IPhpVariableFormatter
    {
        /// <summary>
        /// Serializes a <paramref name="value"/> into a string (unicode or binary).
        /// </summary>
        /// <param name="value">Value to be serialized.</param>
        PhpString Serialize(PhpValue value);
    }

    public static class PhpVariable
    {
        #region Types

        /// <summary>
        /// PHP name for <see cref="int"/>.
        /// </summary>
        public const string TypeNameInt = "int";
        public const string TypeNameInteger = "integer";

        /// <summary>
        /// PHP name for <see cref="long"/>.
        /// </summary>
        public const string TypeNameLongInteger = "int64";

        /// <summary>
        /// PHP name for <see cref="double"/>.
        /// </summary>
        public const string TypeNameDouble = "double";
        public const string TypeNameFloat = "float";

        /// <summary>
        /// PHP name for <see cref="bool"/>.
        /// </summary>
        public const string TypeNameBool = "bool";
        public const string TypeNameBoolean = "boolean";

        /// <summary>
        /// PHP name for <see cref="string"/>.
        /// </summary>
        public const string TypeNameString = "string";

        /// <summary>
        /// PHP name for <see cref="System.Void"/>.
        /// </summary>
        public const string TypeNameVoid = "void";

        /// <summary>
        /// PHP name for <see cref="System.Object"/>.
        /// </summary>
        public const string TypeNameObject = "mixed";

        /// <summary>
        /// PHP name for <B>null</B>.
        /// </summary>
        public const string TypeNameNull = "NULL";

        /// <summary>
        /// PHP name for <B>true</B> constant.
        /// </summary>
        public const string True = "true";

        /// <summary>
        /// PHP name for <B>true</B> constant.
        /// </summary>
        public const string False = "false";

        /// <summary>
        /// Gets the PHP name of given value.
        /// </summary>
        /// <param name="value">The object which type name to get.</param>
        /// <returns>The PHP name of the type of <paramref name="value"/>.</returns>
        /// <remarks>Returns CLR type name for variables of unknown type.</remarks>
        public static string GetTypeName(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long: return TypeNameInteger;
                case PhpTypeCode.Double: return TypeNameDouble;
                case PhpTypeCode.Boolean: return TypeNameBoolean;
                case PhpTypeCode.String:
                case PhpTypeCode.WritableString: return TypeNameString;
                case PhpTypeCode.Alias: return GetTypeName(value.Alias.Value);
                case PhpTypeCode.PhpArray: return PhpArray.PhpTypeName;
                case PhpTypeCode.Object:
                    if (value.IsNull) return TypeNameNull;
                    return value.Object.GetType().Name;
                case PhpTypeCode.Undefined: return TypeNameVoid;
            }

            throw new ArgumentException();
        }

        #endregion

        /// <summary>
        /// Enumerates deep copy of iterator values.
        /// </summary>
        public static IEnumerator<KeyValuePair<IntStringKey, PhpValue>> EnumerateDeepCopies(IEnumerator<KeyValuePair<IntStringKey, PhpValue>> iterator)
        {
            while (iterator.MoveNext())
            {
                var entry = iterator.Current;
                yield return new KeyValuePair<IntStringKey, PhpValue>(entry.Key, entry.Value.DeepCopy());
            }
        }

        public static bool IsValidCallback(IPhpCallable callable)
        {
            PhpCallback tmp;

            return callable != null && ((tmp = callable as PhpCallback) == null || tmp.IsValid);
        }

        /// <summary>
        /// Finds whether a value is a scalar.
        /// </summary>
        /// <remarks>Scalar variables are those containing an integer, float, string or boolean.</remarks>
        public static bool IsScalar(this PhpValue variable)
        {
            switch (variable.TypeCode)
            {
                case PhpTypeCode.Boolean:
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                case PhpTypeCode.String:
                case PhpTypeCode.WritableString:
                    return true;

                case PhpTypeCode.Alias:
                    return IsScalar(variable.Alias.Value);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the value is <see cref="int"/> or <see cref="long"/>.
        /// </summary>
        public static bool IsInteger(this PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                    return true;

                case PhpTypeCode.Alias:
                    return IsInteger(value.Alias.Value);

                default:
                    return false;
            }
        }

        /// <summary>
        /// In case value contains <see cref="PhpArray"/>,
        /// its instance is returned. Otherwise <c>null</c>.
        /// </summary>
        /// <remarks>Value is dereferenced if necessary.</remarks>
        public static PhpArray ArrayOrNull(this PhpValue value)
        {
            return (value.IsAlias ? value.Alias.Value.Object : value.Object) as PhpArray;
        }
    }
}
