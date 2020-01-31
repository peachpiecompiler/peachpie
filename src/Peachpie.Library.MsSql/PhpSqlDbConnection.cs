/*

 Copyright (c) 2005-2006 Tomas Matousek and Martin Maly.  
 Copyright (c) 20012-2017 Jakub Misek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Data;
using System.Data.SqlClient;
using Pchp.Core;
using Pchp.Library.Database;
using Pchp.Library.Resources;

namespace Peachpie.Library.MsSql
{
    internal sealed class SqlConnectionManager : ConnectionManager<PhpSqlDbConnection>
    {
        /// <summary>
        /// Last failed connect attempt error message.
        /// </summary>
        public string FailConnectErrorMessage = "";

        /// <summary>
        /// Gets the manager singleton within runtime context.
        /// </summary>
        public static SqlConnectionManager GetInstance(Context ctx) => ctx.GetStatic<SqlConnectionManager>();

        protected override PhpSqlDbConnection CreateConnection(string/*!*/ connectionString)
        {
            return new PhpSqlDbConnection(this, connectionString);
        }
    }

    /// <summary>
    /// SQL connection resource.
    /// </summary>
    internal sealed class PhpSqlDbConnection : ConnectionResource
    {
        readonly SqlConnection _connection;

        readonly SqlConnectionManager _manager;

        internal SqlConnection SqlConnection => _connection;

        /// <summary>
        /// Gets underlaying connection.
        /// </summary>
        protected override IDbConnection ActiveConnection => _connection;

        ///// <summary>
        ///// Gets reference to script context. Cannot be <c>null</c>.
        ///// </summary>
        //readonly Context/*!*/_ctx;

        /// <summary>
        /// Creates a new connection resource.
        /// </summary>
        /// <param name="manager">Containing connection manager.</param>
        /// <param name="connectionString">Connection string.</param>
        public PhpSqlDbConnection(SqlConnectionManager manager, string/*!*/ connectionString)
            : base(connectionString, "mssql connection")
        {
            //if (ctx == null)
            //    throw new ArgumentNullException(nameof(ctx));

            //_ctx = ctx;
            _manager = manager;
            _connection = new SqlConnection(connectionString);
            // TODO: Connection.InfoMessage += new SqlInfoMessageEventHandler(InfoMessage);
        }

        protected override void FreeManaged()
        {
            base.FreeManaged();

            _manager.RemoveConnection(this);
        }

        /// <summary>
        /// Gets a query result resource.
        /// </summary>
        /// <param name="reader">Data reader to be used for result resource population.</param>
        /// <param name="convertTypes">Whether to convert data types to PHP ones.</param>
        /// <returns>Result resource holding all resulting data of the query.</returns>
        protected override ResultResource/*!*/GetResult(IDataReader/*!*/ reader, bool convertTypes)
        {
            return new PhpSqlDbResult(this, reader, convertTypes);
        }

        /// <summary>
        /// Command factory.
        /// </summary>
        protected override IDbCommand CreateCommand(string commandText, CommandType commandType)
        {
            return new SqlCommand()
            {
                Connection = _connection,
                CommandText = commandText,
                CommandType = commandType,
                CommandTimeout = 0, // TODO: configuration.Timeout
            };
        }
    }
}
