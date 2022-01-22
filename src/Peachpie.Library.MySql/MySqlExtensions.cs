using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Threading;
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
        /// Casts the <paramref name="object"/> to <typeparamref name="TResult"/>.
        /// Otherwise it reflects the <paramref name="object"/>, looking for a <paramref name="getterMethodName"/> and trying to invoke that to get the underlying reference.
        /// </summary>
        /// <typeparam name="TIn">Abstract type.</typeparam>
        /// <typeparam name="TResult">Expected actual type of <paramref name="object"/>.</typeparam>
        /// <param name="object">Reference to object.</param>
        /// <param name="lazyCache">Lazily created dictionary remembering method used ot obtain the underlying value.</param>
        /// <param name="getterMethodName">Method name used to obtain the underlying value.</param>
        /// <returns>Value of type <typeparamref name="TResult"/>.</returns>
        /// <remarks>Used to get an underlying value of wrapping classes like the ones provided by MiniProfiler.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="object"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="getterMethodName"/> is not defined on <paramref name="object"/>.</exception>
        /// <exception cref="NullReferenceException"><paramref name="object"/>' getter method returned null or an unexpected value.</exception>
        static TResult/*!*/GetUnderlyingValue<TIn, TResult>(TIn @object, ref ConcurrentDictionary<Type, MethodInfo> lazyCache, string getterMethodName) where TResult : class
        {
            // we have TResult in most cases:
            if (@object is TResult result)
            {
                return result;
            }

            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            // enure cache
            if (lazyCache == null)
            {
                Interlocked.CompareExchange(ref lazyCache, new ConcurrentDictionary<Type, MethodInfo>(), null);
            }

            // resolve {getterMethodName} method
            var method = lazyCache.GetOrAdd(
                @object.GetType(),
                type => type.GetMethod(getterMethodName, BindingFlags.Instance | BindingFlags.Public))
                ?? throw new InvalidOperationException($"'{getterMethodName}' method could not be resolved for {@object.GetType().Name}.");

            // checks
            var value = method.Invoke(@object, null)
                ?? throw new NullReferenceException($"{getterMethodName}() returned null.");

            return value as TResult
                ?? throw new NullReferenceException($"{@object.GetType().Name}.{getterMethodName}() returned an unexpected value of type {value.GetType().Name}. Expecting '{typeof(TResult).Name}'.");
        }

        /// <summary>
        /// Casts or unwraps given <see cref="IDbCommand"/> to <see cref="MySqlCommand"/>.
        /// </summary>
        /// <returns><see cref="IDbCommand"/> might be wrapped into another class (usually DB profiler class like MiniProfiler).</returns>
        public static MySqlCommand AsMySqlCommand(this IDbCommand command) => GetUnderlyingValue<IDbCommand, MySqlCommand>(command, ref s_iternalCommandMethod, "get_InternalCommand");

        /// <summary>
        /// Casts or unwraps given <see cref="IDbCommand"/> to <see cref="MySqlCommand"/>.
        /// </summary>
        /// <returns><see cref="IDbCommand"/> might be wrapped into another class (usually DB profiler class like MiniProfiler).</returns>
        public static MySqlDataReader AsMySqlDataReader(this IDataReader reader) => GetUnderlyingValue<IDataReader, MySqlDataReader>(reader, ref s_wrappedReaderMethod, "get_WrappedReader");

        /// <summary>
        /// Casts or unwraps given <see cref="IDbConnection"/> to <see cref="MySqlConnection"/>.
        /// </summary>
        /// <returns><see cref="IDbConnection"/> might be wrapped into another class (usually DB profiler class like MiniProfiler).</returns>
        public static MySqlConnection AsMySqlConnection(this IDbConnection connection) => GetUnderlyingValue<IDbConnection, MySqlConnection>(connection, ref s_wrappedConnectionMethod, "get_WrappedConnection");

        static ConcurrentDictionary<Type, MethodInfo>
            s_iternalCommandMethod,
            s_wrappedReaderMethod,
            s_wrappedConnectionMethod;

        /// <summary>
        /// Returns the last insert ID from the MySqlCommand, eventually using the underlying MySqlCommand of another IDbCommand.
        /// </summary>
        /// <param name="command">Generic <see cref="IDbCommand"/> to work with</param>
        /// <returns>Last insert ID</returns>
        public static long LastInsertedId(IDbCommand command) => command.AsMySqlCommand().LastInsertedId;

        /// <summary>
        /// Returns metadata about the columns in the result set.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyCollection{DbColumn}"/> containing metadata about the result set.</returns>
        public static ReadOnlyCollection<DbColumn> GetColumnSchema(IDataReader reader) => reader.AsMySqlDataReader().GetColumnSchema();
    }
}
