using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library.Database
{
    /// <summary>
	/// Abstract class implementing common functionality of PHP connection resources.
	/// </summary>
    [PhpHidden]
    public abstract class ConnectionResource : PhpResource
    {
        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString => _connectionString;
        readonly string _connectionString;

        /// <summary>
        /// Constructs the connection resource.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="resourceTypeName"></param>
        protected ConnectionResource(string connectionString, string resourceTypeName)
            : base(resourceTypeName)
        {
            Debug.Assert(connectionString != null);

            _connectionString = connectionString;
        }

        /// <summary>
        /// Establishes the connection to the database.
        /// Gets <c>false</c> if connection can't be opened.
        /// </summary>
        public abstract bool Connect();

        /// <summary>
		/// Builds a connection string in form of <c>server=;user id=;password=</c>.
		/// </summary>
		public static string BuildConnectionString(string server, string user, string password, string additionalSettings)
        {
            var result = new StringBuilder(32);

            result.Append("server=");
            result.Append(server);
            result.Append(";user id=");
            result.Append(user);
            result.Append(";password=");
            result.Append(password);

            if (!string.IsNullOrEmpty(additionalSettings))
            {
                result.Append(';');
                result.AppendFormat(additionalSettings);
            }

            return result.ToString();
        }
    }
}
