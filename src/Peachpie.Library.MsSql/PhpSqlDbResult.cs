using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Library.Database;
using Pchp.Library.Resources;

namespace Peachpie.Library.MsSql
{
    /// <summary>
	/// Represents a result of a SQL command.
	/// </summary>
	internal sealed class PhpSqlDbResult : ResultResource
    {
        /// <summary>
        /// Limit on size of a batch. Non-positive values means no limit.
        /// </summary>
        public int BatchSize { get; set; } = 0;

        /// <summary>
        /// Creates an instance of a result resource.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="reader">Data reader from which to load results.</param>
        /// <param name="convertTypes">Whether to convert resulting values to PHP types.</param>
        /// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
        public PhpSqlDbResult(ConnectionResource/*!*/ connection, IDataReader/*!*/ reader, bool convertTypes)
            : base(connection, reader, "mssql result", convertTypes)
        {
            // no code in here
        }

        internal static PhpSqlDbResult ValidResult(PhpResource handle)
        {
            var result = handle as PhpSqlDbResult;
            if (result != null && result.IsValid) return result;

            PhpException.Throw(PhpError.Warning, Resources.invalid_result_resource);
            return null;
        }

        /// <summary>
        /// Gets an array of column names.
        /// </summary>
        /// <returns>
        /// Array of column names. If a column doesn't have a name (it is calculated), 
        /// it is assigned "computed{number}" name.
        /// </returns>
        protected override string[] GetNames(int fieldsCount)
        {
            var names = base.GetNames(fieldsCount);

            int j = 0;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrEmpty(names[i]))
                {
                    names[i] = (j > 0) ? "computed" + j : "computed";
                    j++;
                }
            }

            return names;
        }

        /// <summary>
        /// Gets row values.
        /// </summary>
        /// <param name="dataTypes">Column type names.</param>
        /// <param name="convertTypes">Whether to convert value to PHP types.</param>
        /// <returns>Row data.</returns>
        protected override object[] GetValues(string[] dataTypes, bool convertTypes)
        {
            var sql_reader = (SqlDataReader)this.Reader;

            object[] oa = new object[sql_reader.FieldCount];

            if (convertTypes)
            {
                for (int i = 0; i < sql_reader.FieldCount; i++)
                    oa[i] = ConvertDbValue(sql_reader.GetSqlValue(i));
            }
            else
            {
                for (int i = 0; i < sql_reader.FieldCount; i++)
                    oa[i] = sql_reader.GetSqlValue(i);
            }

            return oa;
        }

        /// <summary>
        /// Converts a value from database to PHP value.
        /// </summary>
        /// <param name="dbValue">Database value.</param>
        /// <returns>PHP value.</returns>
        public static object ConvertDbValue(object dbValue)
        {
            if (dbValue is SqlInt32)
            {
                if (dbValue.Equals(SqlInt32.Null)) return null;
                else return ((SqlInt32)dbValue).Value;
            }
            if (dbValue is SqlInt16)
            {
                if (dbValue.Equals(SqlInt16.Null)) return null;
                else return System.Convert.ToInt32(((SqlInt16)dbValue).Value);
            }
            if (dbValue is SqlBoolean)
            {
                if (dbValue.Equals(SqlBoolean.Null)) return null;
                else return ((SqlBoolean)dbValue).Value ? 1 : 0;
            }
            if (dbValue is SqlString)
            {
                if (dbValue.Equals(SqlString.Null)) return null;
                else return ((SqlString)dbValue).Value;
            }

            // TODO: check the format of conversion. Is it culture dependent?
            if (dbValue is SqlDateTime)
            {
                if (dbValue.Equals(SqlDateTime.Null)) return null;
                else return ((SqlDateTime)dbValue).Value.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (dbValue is SqlDouble)
            {
                if (dbValue.Equals(SqlDouble.Null)) return null;
                else return ((SqlDouble)dbValue).Value;
            }

            if (dbValue is SqlInt64)
            {
                if (dbValue.Equals(SqlInt64.Null)) return null;
                else return ((SqlInt64)dbValue).Value.ToString();
            }

            if (dbValue is SqlBinary)
            {
                if (dbValue.Equals(SqlBinary.Null)) return null;
                else return new PhpString(((SqlBinary)dbValue).Value);
            }

            if (dbValue is SqlDecimal)
            {
                if (dbValue.Equals(SqlDecimal.Null)) return null;
                else return ((SqlDecimal)dbValue).Value.ToString();
            }

            // TODO: beware of overflow
            if (dbValue is SqlMoney)
            {
                if (dbValue.Equals(SqlMoney.Null)) return null;
                else return System.Convert.ToDouble(((SqlMoney)dbValue).Value);
            }

            if (dbValue is SqlSingle)
            {
                if (dbValue.Equals(SqlSingle.Null)) return null;
                else return System.Convert.ToDouble(((SqlSingle)dbValue).Value);
            }

            if (dbValue is SqlByte)
            {
                if (dbValue.Equals(SqlByte.Null)) return null;
                else return System.Convert.ToInt32(((SqlByte)dbValue).Value);
            }

            if (dbValue is SqlGuid)
            {
                if (dbValue.Equals(SqlGuid.Null)) return null;
                else return new PhpString(((SqlGuid)dbValue).ToByteArray());
            }

            Debug.Fail(null);
            return dbValue.ToString();
        }

        /// <summary>
        /// Maps database type name to the one displayed by PHP.
        /// </summary>
        /// <param name="typeName">Database name.</param>
        /// <returns>PHP name.</returns>
        protected override string MapFieldTypeName(string typeName)
        {
            switch (typeName)
            {
                case "bit": return "bit";

                case "int":
                case "smallint":
                case "tinyint": return "int";

                case "bigint":
                case "numeric": return "numeric";

                case "money":
                case "smallmoney": return "money";

                case "decimal":
                case "float":
                case "real": return "real";

                case "datetime":
                case "smalldatetime": return "datetime";

                case "char":
                case "varchar":
                case "sql_variant": return "char";

                case "text": return "text";

                case "timestamp":
                case "uniqueidentifier":
                case "binary":
                case "varbinary": return "blob";

                case "image": return "image";

                // Unicode types (dotnet specific):

                case "nvarchar":
                case "nchar": return "nchar";

                case "ntext": return "ntext";

                default: return "unknown";
            }
        }

        /// <summary>
        /// Determines whether a type of a specified PHP name is a numeric type.
        /// </summary>
        /// <param name="phpName">PHP type name.</param>
        /// <returns>Whether the type is numeric ("int", "numeric", or "real").</returns>
        public bool IsNumericType(string phpName)
        {
            switch (phpName)
            {
                case "int":
                case "numeric":
                case "real":
                    return true;

                default:
                    return false;
            }
        }
    }
}
