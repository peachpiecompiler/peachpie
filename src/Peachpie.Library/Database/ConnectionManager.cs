using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Library.Database
{
    /// <summary>
	/// Abstract base class for database connection managers.
	/// </summary>
    public abstract class ConnectionManager<TConnection> : IStaticInit where TConnection : ConnectionResource
    {
        /// <summary>
        /// Associated runtime context.
        /// </summary>
        public Context Context => _ctx;
        Context _ctx;

        /// <summary>
        /// List of connections established by the manager.
        /// </summary>
        private readonly List<TConnection> _connections = new List<TConnection>();

        /// <summary>
		/// Connection factory.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>Connection. Cannot be <c>null</c>.</returns>
        protected abstract TConnection CreateConnection(string connectionString);

        /// <summary>
        /// Establishes a connection if a connection with the same connection string doesn't exist yet.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <param name="newConnection">Whether to create a new connection even if there exists one with same string.</param>
        /// <param name="limit">Maximal number of connections. Negative value means no limit.</param>
        /// <param name="success"><B>true</B> on success, <B>false</B> on failure.</param>
        /// <returns>The connection (opened or not) or a <B>null</B> reference on failure.</returns>
        public TConnection CreateConnection(string connectionString, bool newConnection, int limit, out bool success)
        {
            Debug.Assert(connectionString != null);

            TConnection connection;

            if (!newConnection)
            {
                connection = GetConnectionByString(connectionString);
                if (connection != null)
                {
                    success = true;
                    return connection;
                }
            }

            //if (limit >= 0 && count > limit)
            //{
            //    Interlocked.Decrement(ref AppConnectionCount);

            //    PhpException.Throw(PhpError.Warning, LibResources.GetString("connection_limit_reached", limit));
            //    success = false;
            //    return null;
            //}

            connection = CreateConnection(connectionString);
            Debug.Assert(connection != null);
            if ((success = connection.Connect()) == true)
            {
                _connections.Add(connection);
                _ctx.RegisterDisposable(connection);
            }

            return connection;
        }

        TConnection GetConnectionByString(string connectionString)
        {
            for (int i = 0; i < _connections.Count; i++)
            {
                var connection = _connections[i];
                if (connection.ConnectionString == connectionString)
                {
                    return connection;
                }
            }

            //
            return null;
        }

        /// <summary>
        /// Removes specified connection from the list of active connections.
        /// </summary>
        /// <param name="connection">The connection to be removed.</param>
        public bool RemoveConnection(TConnection connection)
        {
            //
            _ctx.UnregisterDisposable(connection);

            //
            if (_connections.Count != 0)
            {
                if (_connections.Remove(connection))
                {
                    //Interlocked.Decrement(ref AppConnectionCount);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns last opened connection.
        /// </summary>
        public TConnection GetLastConnection()
            => _connections.Count != 0 ? _connections[_connections.Count - 1] : null;

        void IStaticInit.Init(Context ctx)
        {
            _ctx = ctx;
        }
    }
}
