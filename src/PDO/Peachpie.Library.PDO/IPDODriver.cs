using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// Interface of a PDO driver
    /// </summary>
    public interface IPDODriver
    {
        /// <summary>
        /// Gets the driver name (used in DSN)
        /// </summary>
        string Name { get; }
       
        /// <summary>
        /// Gets the client version.
        /// </summary>
        /// <value>
        /// The client version.
        /// </value>
        string ClientVersion { get; }

        /// <summary>
        /// Gets the driver specific attribute value
        /// </summary>
        /// <param name="pdo">The pdo.</param>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        PhpValue GetAttribute(PDO pdo, int attribute);

        /// <summary>
        /// Gets the methods added to the PDO instance when this driver is used.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, ExtensionMethodDelegate> GetPDObjectExtensionMethods();

        /// <summary>
        /// Opens a new database connection.
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        DbConnection OpenConnection(string dsn, string user, string password, PhpArray options);

        /// <summary>
        /// Gets the last insert identifier.
        /// </summary>
        /// <param name="pDO">The p do.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        string GetLastInsertId(PDO pDO, string name);
       
        /// <summary>
        /// Opens a DataReader.
        /// </summary>
        /// <param name="pdo">The pdo.</param>
        /// <param name="cmd">The command.</param>
        /// <param name="cursor">The cursor configuration.</param>
        /// <returns></returns>
        DbDataReader OpenReader(PDO pdo, DbCommand cmd, PDO.PDO_CURSOR cursor);

        /// <summary>
        /// Tries to set a driver specific attribute value.
        /// </summary>
        /// <param name="attributes">The current attributes collection.</param>
        /// <param name="attribute">The attribute to set.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if value is valid, or false if value can't be set.</returns>
        bool TrySetAttribute(Dictionary<PDO.PDO_ATTR, object> attributes, PDO.PDO_ATTR attribute, PhpValue value);

        /// <summary>
        /// Quotes a string for use in a query.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="param">The parameter.</param>
        /// <returns></returns>
        string Quote(string str, PDO.PARAM param);
        
        /// <summary>
        /// Prepare a PDO statement
        /// </summary>
        /// <param name="pdo">The pdo.</param>
        /// <param name="statement">The statement.</param>
        /// <param name="driver_options">The driver options.</param>
        /// <returns></returns>
        PDOStatement PrepareStatement(PDO pdo, string statement, PhpArray driver_options);
    }
}
