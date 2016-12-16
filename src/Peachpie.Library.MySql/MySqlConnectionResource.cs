using MySql.Data.MySqlClient;
using Pchp.Core;
using Pchp.Library.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public MySqlConnectionResource(MySqlConnectionManager manager,string connectionString) : base(connectionString, "mysql connection")
        {
            _manager = manager;
            _connection = new MySqlConnection(this.ConnectionString);
        }

        public override bool Connect()
        {
            _connection.Open(); // TODO: Async

            return true;
        }

        protected override void FreeManaged()
        {
            _manager.RemoveConnection(this);
            _connection.Close();

            //
            base.FreeManaged();
        }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        internal string ServerVersion => _connection.ServerVersion;
    }
}
