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
        readonly MySqlConnection _connection;

        public MySqlConnectionResource(string connectionString) : base(connectionString, "mysql connection")
        {
            _connection = new MySqlConnection(this.ConnectionString);
        }

        public override bool Connect()
        {
            _connection.Open(); // TODO: Async

            return true;
        }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        internal string ServerVersion => _connection.ServerVersion;
    }
}
