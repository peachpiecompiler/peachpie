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
    sealed class MySqlConnection : PhpResource
    {
        public MySqlConnection(string connectionString) : base("mysql connection")
        {
        }
    }
}
