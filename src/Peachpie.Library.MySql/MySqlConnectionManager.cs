using MySqlConnector;
using Pchp.Core;
using Pchp.Library.Database;
using System;
using System.Collections.Generic;
using System.Data;
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

        /// <summary>
        /// Creates a connection resource using an existing <see cref="IDbConnection"/> instance.
        /// </summary>
        internal MySqlConnectionResource CreateConnection(IDbConnection dbconnection)
        {
            var connection = new MySqlConnectionResource(this, dbconnection ?? throw new ArgumentNullException(nameof(dbconnection)));

            AddConnection(connection);

            return connection;
        }

        public virtual void ReportException(Exception exception, string exceptionMessage)
        {
            // MySql outputs the error to php error handler
            PhpException.Throw(PhpError.Warning, exceptionMessage);
        }
    }
}
