using System;
using System.Collections.Generic;
using System.Data.Common;
using MySqlConnector;
using Pchp.Core;
using Peachpie.Library.PDO.Utilities;

namespace Peachpie.Library.PDO.MySQL
{
    /// <summary>
    /// PDO driver class for MySQL
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    public sealed class PDOMySQLDriver : PDODriver
    {
        /// <inheritDoc />
        public override string Name => "mysql";

        /// <inheritDoc />
        public override DbProviderFactory DbFactory => MySqlConnectorFactory.Instance;

        /// <inheritDoc />
        protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            var csb = new MySqlConnectionStringBuilder
            {
                
            };

            // parse and validate the datasource string:
            DataSourceString.ParseNameValue(dsn, csb, (_csb, name, value) =>
            {
                // unknown option aliases:
                if (name.Equals("dbname", StringComparison.OrdinalIgnoreCase)) name = "Database";

                //
                _csb[name] = value;
            });

            //
            if (!string.IsNullOrEmpty(user)) csb.UserID = user;
            if (!string.IsNullOrEmpty(password)) csb.Password = password;

            if (options != null && options.Count != 0)
            {
                csb.Pooling = options[PDO.ATTR_PERSISTENT].ToBoolean();
            }

            //
            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override string GetLastInsertId(PDO pdo, string name)
        {
            var command = (MySqlCommand)pdo.GetCurrentCommand();
            var lastid = (command != null) ? command.LastInsertedId : -1;

            return lastid.ToString();
        }

        /// <inheritDoc />
        public override string Quote(string str, PDO.PARAM param)
        {
            return "'" + MySqlHelper.EscapeString(str) + "'";
        }

        /// <inheritDoc />
        public override bool TrySetAttribute(PDO pdo, Dictionary<PDO.PDO_ATTR, PhpValue> attributes, int attribute, PhpValue value)
        {
            return false;
        }

        /// <inheritDoc />
        public override void HandleException(Exception ex, out PDO.ErrorInfo errorInfo)
        {
            if (ex is MySqlException mex)
            {
                errorInfo = PDO.ErrorInfo.Create(mex.SqlState, mex.Number.ToString(), ex.Message);
            }
            else
            {
                base.HandleException(ex, out errorInfo);
            }
        }
    }
}
