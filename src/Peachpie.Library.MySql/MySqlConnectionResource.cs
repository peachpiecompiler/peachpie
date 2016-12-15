using MySql.Data.MySqlClient;
using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.MySql
{
    /// <summary>
    /// 
    /// </summary>
    sealed class MySqlConnectionResource : PhpResource
    {
        readonly MySqlConnection _connection;

        public MySqlConnectionResource(string connectionString) : base("mysql connection")
        {
            _connection = new MySqlConnection(connectionString);
            _connection.Open(); // TODO: Async
        }
    }
}
