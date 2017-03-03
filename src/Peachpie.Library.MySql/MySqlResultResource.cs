using MySql.Data.MySqlClient;
using MySql.Data.Types;
using Pchp.Core;
using Pchp.Library.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Library.Resources;

namespace Peachpie.Library.MySql
{
    internal sealed class MySqlResultResource : ResultResource
    {
        ///// <summary>
        ///// Custom data associated with single field in row. Created by <see cref="GetCustomData"/> method when data are loaded.
        ///// </summary>
        //public class FieldCustomData
        //{
        //    /// <summary>
        //    /// see Field[i].RealTableName
        //    /// </summary>
        //    public string RealTableName;

        //    /// <summary>
        //    /// see Field[i].corflags
        //    /// </summary>
        //    public ColumnFlags Flags;

        //    /// <summary>
        //    /// see Result.GetColumnSize(i)
        //    /// </summary>
        //    public int ColumnSize;
        //}

        /// <summary>
        /// Creates an instance of a result resource.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="reader">Data reader from which to load results.</param>
        /// <param name="convertTypes">Whether to convert resulting values to PHP types.</param>
        /// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
        public MySqlResultResource(ConnectionResource/*!*/ connection, IDataReader/*!*/ reader, bool convertTypes)
            : base(connection, reader, "MySQL result", convertTypes)
        {
            // no code in here
        }

        internal static MySqlResultResource ValidResult(PhpResource handle)
        {
            var result = handle as MySqlResultResource;
            if (result != null && result.IsValid)
            {
                return result;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.invalid_result_resource);
                return null;
            }
        }

        protected override void FreeManaged()
        {
            var reader = (MySqlDataReader)this.Reader;
            if (reader != null)
            {
                reader.Close(); // non virtual!!
            }

            base.FreeManaged();
        }

        /// <summary>
        /// Gets row values.
        /// </summary>
        /// <param name="dataTypes">Data type names.</param>
        /// <param name="convertTypes">Whether to convert value to PHP types.</param>
        /// <returns>Row data.</returns>
        protected override object[] GetValues(string[] dataTypes, bool convertTypes)
        {
            var my_reader = (MySqlDataReader)Reader;
            var oa = new object[my_reader.FieldCount];

            if (convertTypes)
            {
                Debug.Assert(dataTypes.Length >= oa.Length);
                for (int i = 0; i < oa.Length; i++)
                {
                    oa[i] = ConvertDbValue(dataTypes[i], my_reader.GetValue(i));
                }
            }
            else
            {
                for (int i = 0; i < oa.Length; i++)
                {
                    oa[i] = my_reader.GetValue(i);
                }
            }

            return oa;
        }

        /// <summary>
        /// Collect additional information about current row of Reader.
        /// </summary>
        protected override object GetCustomData()
        {
            //MySqlDataReader my_reader = (MySqlDataReader)Reader;

            //var data = new FieldCustomData[my_reader.FieldCount];

            //var resultset = MySqlDataReaderHelper.ResultSet(my_reader);

            //for (int i = 0; i < my_reader.FieldCount; i++)
            //{
            //    var field = MySqlDataReaderHelper.fields_index(resultset, i);

            //    data[i] = new FieldCustomData()
            //    {
            //        Flags = MySqlDataReaderHelper.colFlags(field), //my_reader.GetFieldFlags(i),
            //        RealTableName = MySqlDataReaderHelper.RealTableName(field), //my_reader.GetRealTableName(i),
            //        ColumnSize = MySqlDataReaderHelper.GetColumnSize(field) //my_reader.GetColumnSize(i)
            //    };
            //}

            //return data;

            return null;
        }

        /// <summary>
        /// Converts a value of a specified MySQL DB type to PHP value.
        /// </summary>
        /// <param name="dataType">MySQL DB data type.</param>
        /// <param name="sqlValue">The value.</param>
        /// <returns>PHP value.</returns>
        private static object ConvertDbValue(string dataType, object sqlValue)
        {
            if (sqlValue == null || sqlValue.GetType() == typeof(string))
                return sqlValue;

            if (sqlValue.GetType() == typeof(double))
                return Pchp.Core.Convert.ToString((double)sqlValue);

            if (sqlValue == System.DBNull.Value)
                return null;

            if (sqlValue.GetType() == typeof(int))
                return ((int)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(uint))
                return ((uint)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(bool))
                return (bool)sqlValue ? "1" : "0";

            if (sqlValue.GetType() == typeof(byte))
                return ((byte)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(sbyte))
                return ((sbyte)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(short))
                return ((short)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(ushort))
                return ((ushort)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(float))
                return Pchp.Core.Convert.ToString((float)sqlValue);

            if (sqlValue.GetType() == typeof(System.DateTime))
                return ConvertDateTime(dataType, (System.DateTime)sqlValue);

            if (sqlValue.GetType() == typeof(long))
                return ((long)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(ulong))
                return ((ulong)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(TimeSpan))
                return ((TimeSpan)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(decimal))
                return ((decimal)sqlValue).ToString();

            if (sqlValue.GetType() == typeof(byte[]))
                return (byte[])sqlValue;

            //MySqlDateTime sql_date_time = sqlValue as MySqlDateTime;
            if (sqlValue.GetType() == typeof(MySqlDateTime))
            {
                MySqlDateTime sql_date_time = (MySqlDateTime)sqlValue;
                if (sql_date_time.IsValidDateTime)
                    return ConvertDateTime(dataType, sql_date_time.GetDateTime());

                if (dataType == "DATE" || dataType == "NEWDATE")
                    return "0000-00-00";
                else
                    return "0000-00-00 00:00:00";
            }

            Debug.Fail("Unexpected DB field type " + sqlValue.GetType() + ".");
            return sqlValue.ToString();
        }

        private static string ConvertDateTime(string dataType, System.DateTime value)
        {
            if (dataType == "DATE" || dataType == "NEWDATE")
                return value.ToString("yyyy-MM-dd");
            else
                return value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        ///// <summary>
        ///// Gets flags of the current field.
        ///// </summary>
        ///// <returns>The field length.</returns>
        //public ColumnFlags GetFieldFlags()
        //{
        //    return GetFieldFlags(CurrentFieldIndex);
        //}

        ///// <summary>
        ///// Gets flags of a specified field.
        ///// </summary>
        ///// <param name="fieldIndex">An index of the field.</param>
        ///// <returns>The field length or 0.</returns>
        //public ColumnFlags GetFieldFlags(int fieldIndex)
        //{
        //    if (!CheckFieldIndex(fieldIndex)) return 0;
        //    return MySqlDataReaderHelper.colFlags(MySqlDataReaderHelper.fields_index(MySqlDataReaderHelper.ResultSet((MySqlDataReader)Reader), fieldIndex));  // ((MySqlDataReader)Reader).GetFieldFlags(fieldIndex);
        //}

        ///// <summary>
        ///// field RealTableName
        ///// </summary>
        ///// <param name="fieldIndex"></param>
        ///// <returns></returns>
        //public string GetRealTableName(int fieldIndex)
        //{
        //    if (!CheckFieldIndex(fieldIndex)) return null;

        //    return MySqlDataReaderHelper.RealTableName(MySqlDataReaderHelper.fields_index(MySqlDataReaderHelper.ResultSet((MySqlDataReader)Reader), fieldIndex));  //((MySqlDataReader)Reader).GetRealTableName(fieldIndex);
        //}

        /// <summary>
        /// Maps MySQL .NET Connector's type name to the one displayed by PHP.
        /// </summary>
        /// <param name="typeName">MySQL .NET Connector's name.</param>
        /// <returns>PHP name.</returns>
        protected override string MapFieldTypeName(string typeName)
        {
            switch (typeName)
            {
                case "VARCHAR":
                    return "string";

                case "INT":
                case "BIGINT":
                case "MEDIUMINT":
                case "SMALLINT":
                case "TINYINT":
                    return "int";

                case "FLOAT":
                case "DOUBLE":
                case "DECIMAL":
                    return "real";

                case "YEAR":
                    return "year";

                case "DATE":
                case "NEWDATE":
                    return "date";

                case "TIMESTAMP":
                    return "timestamp";

                case "DATETIME":
                    return "datetime";

                case "TIME":
                    return "time";

                case "SET":
                    return "set";

                case "ENUM":
                    return "enum";

                case "TINY_BLOB":
                case "MEDIUM_BLOB":
                case "LONG_BLOB":
                case "BLOB":
                    return "blob";

                // not in PHP:
                case "BIT":
                    return "bit";

                case null:
                case "NULL":
                    return "NULL";

                default:
                    return "unknown";
            }
        }

        readonly static HashSet<string> _numericTypes = new HashSet<string>(StringComparer.Ordinal) { "int", "real", "year", "timestamp" };

        /// <summary>
        /// Determines whether a type of a specified PHP name is a numeric type.
        /// </summary>
        /// <param name="phpName">PHP type name.</param>
        /// <returns>Whether the type is numeric ("int", "real", or "year").</returns>
        public bool IsNumericType(string phpName) => _numericTypes.Contains(phpName);
    }
}
