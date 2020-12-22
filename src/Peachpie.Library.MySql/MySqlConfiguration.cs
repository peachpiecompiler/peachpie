using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using static Pchp.Library.StandardPhpOptions;
using Pchp.Library;

namespace Peachpie.Library.MySql
{
    sealed class MySqlConfiguration : IPhpConfiguration
    {
        IPhpConfiguration IPhpConfiguration.Copy() => (MySqlConfiguration)this.MemberwiseClone();

        public string ExtensionName => "mysql";

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

        /// <summary>
        /// Gets, sets, or restores a value of a legacy configuration option.
        /// </summary>
        static PhpValue GetSet(Context ctx, IPhpConfigurationService config, string option, PhpValue value, IniAction action)
        {
            var local = config.Get<MySqlConfiguration>();

            switch (option)
            {
                // local:

                case "mysql.connect_timeout": return StandardPhpOptions.GetSet(ref local.ConnectTimeout, 0, value, action);

                case "mysqli.default_port":
                case "mysql.default_port": return StandardPhpOptions.GetSet(ref local.Port, 3306, value, action);

                case "mysqli.default_host":
                case "mysql.default_host": return StandardPhpOptions.GetSet(ref local.Server, null, value, action);

                case "mysqli.default_user":
                case "mysql.default_user": return StandardPhpOptions.GetSet(ref local.User, "root", value, action);

                case "mysqli.default_pw":
                case "mysql.default_password": return StandardPhpOptions.GetSet(ref local.Password, "", value, action);

                case "mysql.default_command_timeout": return StandardPhpOptions.GetSet(ref local.DefaultCommandTimeout, -1, value, action);

                case "mysql.connection_string": return StandardPhpOptions.GetSet(ref local.ConnectionString, null, value, action);

                // global:

                case "mysql.max_links":
                    Debug.Assert(action == IniAction.Get);
                    return StandardPhpOptions.GetSet(ref local.MaxConnections, -1, value, action);

                case "mysql.max_pool_size":
                    return StandardPhpOptions.GetSet(ref local.MaxPoolSize, 100, value, action);
            }

            Debug.Fail("Option '" + option + "' is supported but not implemented.");
            return PhpValue.False;
        }

        /// <summary>
        /// Registers legacy ini-options.
        /// </summary>
        internal static void RegisterLegacyOptions()
        {
            const string s = "mysql";
            var d = new GetSetDelegate(GetSet);

            // local:
            Register("mysql.trace_mode", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mysqli.default_port", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysql.default_port", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysqli.default_socket", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mysql.default_socket", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mysqli.default_host", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysql.default_host", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysqli.default_user", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysql.default_user", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysqli.default_pw", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysql.default_password", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysql.connect_timeout", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysql.default_command_timeout", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mysql.connection_string", IniFlags.Supported | IniFlags.Local, d, s);

            // global:
            Register("mysql.allow_persistent", IniFlags.Unsupported | IniFlags.Global, d, s);
            Register("mysql.max_persistent", IniFlags.Unsupported | IniFlags.Global, d, s);
            Register("mysql.max_links", IniFlags.Supported | IniFlags.Global, d, s);
            Register("mysql.max_pool_size", IniFlags.Supported | IniFlags.Global, d, s);
        }
    }
}
