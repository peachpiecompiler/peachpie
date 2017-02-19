using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.MySql
{
    class MySqlConfiguration : IPhpConfiguration
    {
        IPhpConfiguration IPhpConfiguration.Copy() => (MySqlConfiguration)this.MemberwiseClone();

        /// <summary>
        /// Request timeout in seconds. Non-positive value means no timeout.
        /// </summary>
        public int ConnectTimeout = 0;

        /// <summary>
        /// <c>Maximum Pool Size</c> value passed to the MySql Connector/Net Connection String.
        /// </summary>
        public int MaxPoolSize = 100;

        /// <summary>
        /// Command timeout, in seconds.
        /// </summary>
        public int DefaultCommandTimeout = -1;

        /// <summary>
        /// Default server (host) name.
        /// </summary>
        public string Server = "localhost";

        /// <summary>
        /// Default port.
        /// </summary>
        public int Port = 3306;

        /// <summary>
        /// Default user name.
        /// </summary>
        public string User = "root";

        /// <summary>
        /// Default password.
        /// </summary>
        public string Password = "";

        /// <summary>
        /// If not <c>null</c> reference, this connection string is used by parameterless <c>mysql_connect()</c> function call as MySql Connector/.NET connection string.
        /// </summary>
        internal string ConnectionString = null;

        /// <summary>
        /// Maximum number of connections per request. Negative value means no limit.
        /// </summary>
        public int MaxConnections = -1;
    }
}
