using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace Peachpie.Library.MySql
{
    internal static class Extensions
    {
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
    }
}
