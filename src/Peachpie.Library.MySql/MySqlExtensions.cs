using System;
using System.Collections.Generic;
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
    }
}
