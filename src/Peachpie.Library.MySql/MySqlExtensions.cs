using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using MySqlConnector;
using Pchp.Core;
using Pchp.Library.Database;

namespace Peachpie.Library.MySql
{
    /// <summary>
    /// Helper MySql methods.
    /// </summary>
    public static class MySqlExtensions
    {
        /// <summary>
        /// Whether the column is typed as unsigned integer.
        /// </summary>
        public static bool IsUnsigned(this MySqlDbColumn col)
        {
            switch (col.ProviderType)
            {
                case MySqlDbType.UByte:
                case MySqlDbType.UInt16:
                case MySqlDbType.UInt24:
                case MySqlDbType.UInt32:
                case MySqlDbType.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// True for any blob type.
        /// </summary>
        public static bool IsBlob(this MySqlDbColumn col)
        {
            switch (col.ProviderType)
            {
                case MySqlDbType.Blob:
                case MySqlDbType.LongBlob:
                case MySqlDbType.MediumBlob:
                case MySqlDbType.TinyBlob:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// True for any numeric type.
        /// </summary>
        public static bool IsNumeric(this MySqlDbColumn col)
        {
            switch (col.ProviderType)
            {
                case MySqlDbType.Int16:
                case MySqlDbType.Int24:
                case MySqlDbType.Int32:
                case MySqlDbType.Int64:
                case MySqlDbType.Double:
                case MySqlDbType.Year:
                case MySqlDbType.Timestamp:
                case MySqlDbType.Decimal:
                case MySqlDbType.Float:
                case MySqlDbType.UByte:
                case MySqlDbType.UInt16:
                case MySqlDbType.UInt24:
                case MySqlDbType.UInt32:
                case MySqlDbType.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns <paramref name="link"/> as MySql connection or last opened MySql connection for given <paramref name="ctx"/>.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="link">Optional, an existing MySql connection resource.</param>
        /// <returns>MySql connection resource or <c>null</c> if there is no connection.</returns>
        public static ConnectionResource ValidConnection(Context ctx, PhpResource link = null)
        {
            var resource = link ?? MySqlConnectionManager.GetInstance(ctx).GetLastConnection();
            return resource as MySqlConnectionResource;
        }

        /// <summary>
        /// Returns the last insert ID from an IDbCommand by unwrapping it to the internal MySqlCommand
        /// </summary>
        /// <param name="command">Generic IDbCommand to work with</param>
        /// <returns>Last insert ID</returns>
        public static long LastInsertedId(
            IDbCommand command)
        {
            // If we have a MySqlCommand, just use it
            if (command is MySqlCommand mySqlCommand) return mySqlCommand.LastInsertedId;

            // If we did not get one back, try to unwrap it as likely it's wrapped by a profiler like MiniProfiler
            if (_innerCommandMethod == null)
                _innerCommandMethod = command.GetType().GetMethod("get_InternalCommand", BindingFlags.Instance | BindingFlags.Public);
            mySqlCommand = _innerCommandMethod?.Invoke(command, null) as MySqlCommand;
            if (mySqlCommand == null) throw new NullReferenceException("Could not get internal command for wrapped command!");
            return mySqlCommand.LastInsertedId;
        }
        private static MethodInfo _innerCommandMethod;

        /// <summary>
        /// Returns metadata about the columns in the result set.
        /// </summary>
        /// <returns>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{DbColumn}"/> containing metadata about the result set.</returns>
        public static ReadOnlyCollection<DbColumn> GetColumnSchema(
            IDataReader reader)
        {
            // If we have a MySqlCommand, just use it
            if (reader is MySqlDataReader mySqlDataReader) return mySqlDataReader.GetColumnSchema();

            // If we did not get one back, try to unwrap it as likely it's wrapped by a profiler like MiniProfiler
            if (_innerDataReaderMethod == null)
                _innerDataReaderMethod = reader.GetType().GetMethod("get_WrappedReader", BindingFlags.Instance | BindingFlags.Public);
            mySqlDataReader = _innerDataReaderMethod?.Invoke(reader, null) as MySqlDataReader;
            if (mySqlDataReader == null) throw new NullReferenceException("Could not get MySqlDataReader for wrapped reader!");
            return mySqlDataReader.GetColumnSchema();
        }
        private static MethodInfo _innerDataReaderMethod;
    }
}
