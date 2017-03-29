using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Peachpie.Library.PDO
{
    //IBM pure ADO.NET Driver is not available on dotnet core, only on framework
#if NETSTANDARD1_6
using IBM.Data.DB2.Core;
 
    /// <summary>
    /// PDO driver class for IBM DB2
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    [System.Composition.Export(typeof(IPDODriver))]
    public class PDODB2Driver : PDODriver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PDODB2Driver"/> class.
        /// </summary>
        public PDODB2Driver() : base("ibm", DB2Factory.Instance)
        {
        }


        /// <inheritDoc />
        protected override string BuildConnectionString(string dsn, string user, string password, PhpArray options)
        {
            //TODO ibm db2 pdo parameters to dotnet connectionstring
            var csb = new DB2ConnectionStringBuilder(dsn);
            csb.UserID = user;
            csb.Password = password;
            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override Dictionary<string, ExtensionMethodDelegate> GetPDObjectExtensionMethods()
        {
            return new Dictionary<string, ExtensionMethodDelegate>();
        }

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
#endif
}
