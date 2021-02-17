using System;
using System.Collections.Generic;
using System.Data;
using Pchp.Core;
using System.Data.Common;
using System.Diagnostics;
using Pchp.Library.Database;

namespace Peachpie.Library.PDO
{
    public partial class PDO
    {
        /// <summary>Implementation of our connection resource to be used by PDO.</summary>
        internal sealed class PdoConnectionResource : ConnectionResource
        {
            public PDO PDO { get; }

            /// <summary>Current runtime context.</summary>
            public Context Context => PDO._ctx;

            public PdoConnectionResource(PDO pdo, DbConnection connection)
                : base(connection.ConnectionString, nameof(PdoConnectionResource))
            {
                this.PDO = pdo;
                this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            }

            /// <summary>Actual database connection.</summary>
            public DbConnection Connection { get; }

            protected override IDbConnection ActiveConnection => Connection;

            public DbCommand LastCommand { get; private set; }

            public DbCommand CreateCommand(string commandText)
            {
                var dbCommand = Connection.CreateCommand();
                dbCommand.CommandText = commandText;
                dbCommand.Transaction = PDO.CurrentTransaction;
                dbCommand.CommandTimeout = (PDO.TryGetAttribute(PDO_ATTR.ATTR_TIMEOUT, out var timeout) ? (int)timeout : 30) * 1000;

                LastCommand = dbCommand;

                return dbCommand;
            }

            protected override IDbCommand CreateCommand(string commandText, CommandType commandType) => CreateCommand(commandText);

            protected override ResultResource GetResult(IDataReader reader, bool convertTypes)
            {
                return new PdoResultResource(this, reader, convertTypes);
            }

            internal ResultResource ExecuteCommand(IDbCommand command, bool convertTypes, IList<IDataParameter> parameters, bool skipResults)
            {
                return ExecuteCommandProtected(command, convertTypes, parameters, skipResults);
            }
        }

        /// <summary>Result loaded with data supporting iteration.</summary>
        sealed class PdoResultResource : ResultResource
        {
            #region Properties

            /// <summary>
            /// Underlying connection resource.
            /// </summary>
            new PdoConnectionResource Connection => (PdoConnectionResource)base.Connection;

            /// <summary>Current context.</summary>
            Context Context => Connection.Context;

            /// <summary>Reference to containing <see cref="PDO"/> instance.</summary>
            PDO PDO => Connection.PDO;

            /// <summary>Reference to underlying PDO driver.</summary>
            PDODriver Driver => PDO.Driver;

            #endregion

            public PdoResultResource(PdoConnectionResource connection, IDataReader reader, bool convertTypes)
                : base(connection, reader, nameof(PdoResultResource), convertTypes)
            {
            }

            protected override object[] GetValues(string[] dataTypes, bool convertTypes)
            {
                var my_reader = Reader;
                var oa = new object[my_reader.FieldCount];

                if (convertTypes)
                {
                    bool stringify = PDO.Stringify;

                    for (int i = 0; i < oa.Length; i++)
                    {
                        oa[i] = ConvertDbValue(dataTypes[i], my_reader.GetValue(i), stringify);
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

            protected override string MapFieldTypeName(string typeName)
            {
                return typeName;
            }

            /// <summary>
            /// Converts a value of a specified MySQL DB type to PHP value.
            /// </summary>
            /// <param name="dataType">MySQL DB data type.</param>
            /// <param name="sqlValue">The value.</param>
            /// <param name="stringify">Whether to convert the value to nullable string.
            /// Byte arrays are not converted to be later properly turned into PhpString.</param>
            /// <returns>PHP value.</returns>
            private static object ConvertDbValue(string dataType, object sqlValue, bool stringify)
            {
                //if (sqlValue == null || sqlValue.GetType() == typeof(string))
                //    return sqlValue;

                if (sqlValue == DBNull.Value)
                    return null;

                //if (sqlValue.GetType() == typeof(int))
                //    return ((int)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(uint))
                //    return ((uint)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(byte))
                //    return ((byte)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(sbyte))
                //    return ((sbyte)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(short))
                //    return ((short)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(ushort))
                //    return ((ushort)sqlValue).ToString();

                if (sqlValue is System.DateTime datetime)
                    return ConvertDateTime(dataType, datetime);

                //if (sqlValue.GetType() == typeof(long))
                //    return ((long)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(ulong))
                //    return ((ulong)sqlValue).ToString();

                if (sqlValue.GetType() == typeof(TimeSpan))
                    return ((TimeSpan)sqlValue).ToString();

                if (sqlValue.GetType() == typeof(decimal))
                    return ((decimal)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(byte[]))
                //    return (byte[])sqlValue;

                ////MySqlDateTime sql_date_time = sqlValue as MySqlDateTime;
                //if (sqlValue.GetType() == typeof(MySqlDateTime))
                //{
                //    MySqlDateTime sql_date_time = (MySqlDateTime)sqlValue;
                //    if (sql_date_time.IsValidDateTime)
                //        return ConvertDateTime(dataType, sql_date_time.GetDateTime());

                //    if (dataType == "DATE" || dataType == "NEWDATE")
                //        return "0000-00-00";
                //    else
                //        return "0000-00-00 00:00:00";
                //}

                if (stringify && sqlValue != null)
                {
                    if (sqlValue is bool b)
                        return b ? "1" : "0";

                    if (sqlValue is double d)
                        return Pchp.Core.Convert.ToString(d);

                    if (sqlValue.GetType() == typeof(float))
                        return Pchp.Core.Convert.ToString((float)sqlValue);

                    if (sqlValue.GetType() == typeof(byte[]))
                        return sqlValue;    // keep byte[] array (will be stored to PhpString later)

                    return sqlValue.ToString();
                }

                //
                return sqlValue;
            }

            static string ConvertDateTime(string dataType, System.DateTime value)
            {
                if (dataType == "DATE" || dataType == "NEWDATE")
                    return value.ToString("yyyy-MM-dd");
                else
                    return value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        internal PDODriver Driver { get; private set; }

        private protected PdoConnectionResource _connection;
    }
}
