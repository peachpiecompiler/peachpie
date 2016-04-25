using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
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
