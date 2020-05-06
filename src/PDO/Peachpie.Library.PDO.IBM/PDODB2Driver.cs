using System.Collections.Generic;
using Pchp.Core;

namespace Peachpie.Library.PDO
{
    using System;
    using System.Data.Common;
    //IBM pure ADO.NET Driver is not available on dotnet core, only on framework
    using IBM.Data.DB2.Core;
    using Peachpie.Library.PDO.Utilities;

    /// <summary>
    /// PDO driver class for IBM DB2
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    public class PDODB2Driver : PDODriver
    {
        /// <inheritDoc />
        public override string Name => "ibm";

        /// <inheritDoc />
        public override DbProviderFactory DbFactory => DB2Factory.Instance;

        /// <inheritDoc />
        protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            //TODO ibm db2 pdo parameters to dotnet connectionstring
            var csb = new DB2ConnectionStringBuilder(dsn.ToString());
            csb.UserID = user;
            csb.Password = password;
            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override ExtensionMethodDelegate TryGetExtensionMethod(string name) => base.TryGetExtensionMethod(name);

        /// <inheritDoc />
        public override string GetLastInsertId(PDO pdo, string name)
        {
            //http://php.net/manual/en/function.db2-last-insert-id.php#98361
            //https://www.ibm.com/support/knowledgecenter/SSEPGG_9.7.0/com.ibm.db2.luw.sql.ref.doc/doc/r0004231.html
            using (var cmd = pdo.CreateCommand("SELECT IDENTITY_VAL_LOCAL() AS LASTID FROM SYSIBM.SYSDUMMY1"))
            {
                object value = cmd.ExecuteScalar();
                return value?.ToString();
            }
        }
    }
}
