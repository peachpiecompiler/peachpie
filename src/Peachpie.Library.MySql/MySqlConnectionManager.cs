using Pchp.Core;
using Pchp.Library.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.MySql
{
    internal class MySqlConnectionManager : ConnectionManager<MySqlConnectionResource>
    {
        /// <summary>
        /// Gets the manager singleton within runtime context.
        /// </summary>
        public static MySqlConnectionManager GetInstance(Context ctx) => ctx.GetStatic<MySqlConnectionManager>();

        protected override MySqlConnectionResource CreateConnection(string connectionString) => new MySqlConnectionResource(this, connectionString);
    }
}
