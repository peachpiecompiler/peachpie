using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDO driver base class
    /// </summary>
    /// <seealso cref="IPDODriver" />
    [PhpHidden]
    public abstract class PDODriver : IPDODriver
    {
        /// <inheritDoc />
        public string Name { get; }

        /// <inheritDoc />
        public virtual string ClientVersion
        {
            get
            {
                return this.DbFactory.GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
            }
        }

        /// <inheritDoc />
        public DbProviderFactory DbFactory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDODriver"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dbFactory">The database factory object.</param>
        /// <exception cref="System.ArgumentNullException">
        /// name
        /// or
        /// dbFactory
        /// </exception>
        public PDODriver(string name, DbProviderFactory dbFactory)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (dbFactory == null)
                throw new ArgumentNullException(nameof(dbFactory));

            this.Name = name;
            this.DbFactory = dbFactory;
        }

        /// <summary>
        /// Builds the connection string.
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        protected abstract string BuildConnectionString(string dsn, string user, string password, PhpArray options);

        /// <inheritDoc />
        public virtual DbConnection OpenConnection(string dsn, string user, string password, PhpArray options)
        {
            string connectionString = this.BuildConnectionString(dsn, user, password, options);
            var connection = this.DbFactory.CreateConnection();
            connection.ConnectionString = connectionString;
            connection.Open();
            return connection;
        }

        /// <inheritDoc />
        public virtual Dictionary<string, ExtensionMethodDelegate> GetPDObjectExtensionMethods()
        {
            return new Dictionary<string, ExtensionMethodDelegate>();
        }

        /// <inheritDoc />
        public abstract string GetLastInsertId(PDO pdo, string name);

        /// <inheritDoc />
        public virtual bool TrySetAttribute(Dictionary<PDO.PDO_ATTR, object> attributes, PDO.PDO_ATTR attribute, PhpValue value)
        {
            return false;
        }

        /// <inheritDoc />
        public virtual string Quote(string str, PDO.PARAM param)
        {
            return null;
        }

        /// <inheritDoc />
        public virtual PhpValue GetAttribute(PDO pdo, int attribute)
        {
            return PhpValue.Null;
        }

        /// <summary>
        /// Registers all referenced PDO drivers.
        /// </summary>
        public static void RegisterAllDrivers()
        {
            foreach (var driver in Context.CompositionContext.GetExports<IPDODriver>())
            {
                PDOEngine.RegisterDriver(driver);
            }
        }

        /// <inheritDoc />
        public virtual PDOStatement PrepareStatement(PDO pdo, string statement, PhpArray driver_options)
        {
            PDOStatement stmt = new PDOStatement(pdo, statement, driver_options);
            return stmt;
        }

        /// <inheritDoc />
        public virtual DbDataReader OpenReader(PDO pdo, DbCommand cmd, PDO.PDO_CURSOR cursor)
        {
            return cmd.ExecuteReader();
        }
    }
}
