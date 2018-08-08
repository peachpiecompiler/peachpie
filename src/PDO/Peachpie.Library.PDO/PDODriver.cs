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
                return this.DbFactory.GetType().Assembly.GetName().Version.ToString();
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
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.DbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        /// <summary>
        /// Builds the connection string.
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        protected abstract string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options);

        /// <inheritDoc />
        public virtual DbConnection OpenConnection(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            var connection = this.DbFactory.CreateConnection();
            connection.ConnectionString = this.BuildConnectionString(dsn, user, password, options);
            connection.Open();
            return connection;
        }

        /// <inheritDoc />
        public virtual ExtensionMethodDelegate TryGetExtensionMethod(string name) => null;

        /// <inheritDoc />
        public abstract string GetLastInsertId(PDO pdo, string name);

        /// <inheritDoc />
        public virtual bool TrySetAttribute(Dictionary<PDO.PDO_ATTR, PhpValue> attributes, PDO.PDO_ATTR attribute, PhpValue value)
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

        /// <inheritDoc />
        public virtual PDOStatement PrepareStatement(Context ctx, PDO pdo, string statement, PhpArray driver_options)
        {
            PDOStatement stmt = new PDOStatement(ctx, pdo, statement, driver_options);
            return stmt;
        }

        /// <inheritDoc />
        public virtual DbDataReader OpenReader(PDO pdo, DbCommand cmd, PDO.PDO_CURSOR cursor)
        {
            return cmd.ExecuteReader();
        }
    }
}
