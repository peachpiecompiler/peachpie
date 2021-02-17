using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Library;
using static Pchp.Library.StandardPhpOptions;

namespace Peachpie.Library.MsSql
{
    internal class MsSqlConfiguration : IPhpConfiguration
    {
        public IPhpConfiguration Copy() => (MsSqlConfiguration)this.MemberwiseClone();

        public string ExtensionName => "mssql";

        /// <summary>
		/// Request timeout in seconds. Non-positive value means no timeout.
		/// </summary>
		public int Timeout = 60;

        /// <summary>
        /// Connect timeout in seconds. Non-positive value means no timeout.
        /// </summary>
        public int ConnectTimeout = 5;

        /// <summary>
        /// Limit on size of a batch. Non-positive value means no limit.
        /// </summary>
        public int BatchSize = 0;

        /// <summary>
		/// Maximum number of connections per request. Negative value means no limit.
		/// </summary>
		public int MaxConnections = -1;

        /// <summary>
        /// Use NT authentication when connecting to the server.
        /// </summary>
        public bool NTAuthentication = false;

        /// <summary>
		/// Gets or sets a value of a legacy configuration option.
		/// </summary>
		static PhpValue GetSet(Context ctx, IPhpConfigurationService config, string option, PhpValue value, IniAction action)
        {
            var local = config.Get<MsSqlConfiguration>();

            switch (option)
            {
                // local:

                case "mssql.connect_timeout":
                    return StandardPhpOptions.GetSet(ref local.ConnectTimeout, 5, value, action);

                case "mssql.timeout":
                    return StandardPhpOptions.GetSet(ref local.Timeout, 60, value, action);

                case "mssql.batchsize":
                    return StandardPhpOptions.GetSet(ref local.BatchSize, 0, value, action);

                // global:  

                case "mssql.max_links":
                    Debug.Assert(action == IniAction.Get);
                    return StandardPhpOptions.GetSet(ref local.MaxConnections, 0, PhpValue.Null, action);

                case "mssql.secure_connection":
                    Debug.Assert(action == IniAction.Get);
                    return StandardPhpOptions.GetSet(ref local.NTAuthentication, false, PhpValue.Null, action);
            }

            Debug.Fail("Option '" + option + "' is supported but not implemented.");
            return PhpValue.False;
        }

        /// <summary>
		/// Registers legacy ini-options.
		/// </summary>
		internal static void RegisterLegacyOptions()
        {
            const string s = "mssql";
            var d = new GetSetDelegate(GetSet);

            // global:
            Register("mssql.max_links", IniFlags.Supported | IniFlags.Global, d, s);
            Register("mssql.secure_connection", IniFlags.Supported | IniFlags.Global, d, s);
            Register("mssql.allow_persistent", IniFlags.Unsupported | IniFlags.Global, d, s);
            Register("mssql.max_persistent", IniFlags.Unsupported | IniFlags.Global, d, s);

            // local:
            Register("mssql.connect_timeout", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mssql.timeout", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mssql.batchsize", IniFlags.Supported | IniFlags.Local, d, s);
            Register("mssql.min_error_severity", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mssql.min_message_severity", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mssql.compatability_mode", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mssql.textsize", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mssql.textlimit", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mssql.datetimeconvert", IniFlags.Unsupported | IniFlags.Local, d, s);
            Register("mssql.max_procs", IniFlags.Unsupported | IniFlags.Local, d, s);
        }
    }
}
