using System;
using System.Collections.Generic;
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

    public class PhpVariable
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
    }
}
