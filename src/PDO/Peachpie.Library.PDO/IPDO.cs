using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// Interface of PDO class
    /// </summary>
    public interface IPDO
    {
        /// <summary>
        /// Initiates a transaction
        /// </summary>
        /// <exception cref="PDOException">When a transaction has already been started</exception>
        /// <returns>True if transaction started successfully, or false</returns>
        bool beginTransaction();
        /// <summary>
        /// Commits a transaction
        /// </summary>
        /// <returns></returns>
        bool commit();

        /// <summary>
        /// Creates a PDO instance representing a connection to a database
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        void __construct(string dsn, string username = null, string password = null, PhpArray options = null);
        /// <summary>
        /// Fetch the SQLSTATE associated with the last operation on the database handle
        /// </summary>
        /// <returns></returns>
        PhpValue errorCode();
        /// <summary>
        /// Fetch extended error information associated with the last operation on the database handle
        /// </summary>
        /// <returns></returns>
        PhpValue errorInfo();
        /// <summary>
        /// Execute an SQL statement and return the number of affected rows.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <returns></returns>
        PhpValue exec(string statement);
        /// <summary>
        /// Retrieve a database connection attribute
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        PhpValue getAttribute(int attribute);
        /// <summary>
        /// Checks if inside a transaction
        /// </summary>
        /// <returns></returns>
        bool inTransaction();
        /// <summary>
        /// Returns the ID of the last inserted row or sequence value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        string lastInsertId(string name = null);
        /// <summary>
        /// Prepares a statement for execution and returns a statement object.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="driver_options">The driver options.</param>
        /// <returns></returns>
        IPDOStatement prepare(string statement, PhpArray driver_options = null);
        /// <summary>
        /// Executes an SQL statement, returning a result set as a PDOStatement object.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        IPDOStatement query(string statement, params PhpValue[] args);
        /// <summary>
        /// Quotes a string for use in a query.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="parameter_type">Type of the parameter.</param>
        /// <returns></returns>
        string quote(string str, int parameter_type = PDO.PARAM_STR);
        /// <summary>
        /// Rolls back a transaction.
        /// </summary>
        /// <returns></returns>
        bool rollback();
        /// <summary>
        /// Set an attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        bool setAttribute(int attribute, PhpValue value);

    }
}
