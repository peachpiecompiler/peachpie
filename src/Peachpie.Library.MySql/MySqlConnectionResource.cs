using MySql.Data.MySqlClient;
using Pchp.Core;
using Pchp.Library.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Diagnostics;

namespace Peachpie.Library.MySql
{
    /// <summary>
    /// 
    /// </summary>
    [PhpHidden]
    sealed class MySqlConnectionResource : ConnectionResource
    {
        readonly MySqlConnectionManager _manager;
        readonly MySqlConnection _connection;

        public MySqlConnectionResource(MySqlConnectionManager manager, string connectionString) : base(connectionString, "mysql connection")
        {
            _manager = manager;
            _connection = new MySqlConnection(this.ConnectionString);
        }

        public override bool Connect()
        {
            if (_connection.State == ConnectionState.Open)
            {
                return true;
            }

            try
            {
                _connection.Open();  // TODO: Async
                _lastException = null;
            }
            catch (System.Exception e)
            {
                _lastException = e;

                throw new NotImplementedException();    // TODO: ERR

                //PhpException.Throw(PhpError.Warning, LibResources.GetString("cannot_open_connection",
                //  GetExceptionMessage(e)));

                //return false;
            }

            return true;
        }

        protected override void FreeManaged()
        {
            base.FreeManaged();

            _manager.RemoveConnection(this);

            try
            {
                if (_connection != null && _connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }

                _lastException = null;
            }
            catch (System.Exception e)
            {
                _lastException = e;
                throw new NotImplementedException(); // TODO: ERR
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("error_closing_connection",
                //  GetExceptionMessage(e)));
            }
        }

        public override void ClosePendingReader()
        {
            var myreader = (MySqlDataReader)_pendingReader;
            if (myreader != null)
            {
                myreader.Close();   // we have to call Close() on MySqlDataReader, it is declared as non-virtual!
                _pendingReader = myreader = null;
            }
        }

        protected override IDbConnection ActiveConnection => _connection;

        protected override ResultResource GetResult(ConnectionResource connection, IDataReader reader, bool convertTypes)
        {
            return new MySqlResultResource(connection, reader, convertTypes);
        }

        protected override IDbCommand CreateCommand(string commandText, CommandType commandType)
        {
            return new MySqlCommand()
            {
                Connection = _connection,
                CommandText = commandText,
                CommandType = commandType
            };
        }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        internal string ServerVersion => _connection.ServerVersion;
    }
}
