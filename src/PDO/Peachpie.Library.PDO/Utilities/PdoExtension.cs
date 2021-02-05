using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.PDO.Utilities
{
    /// <summary>
    /// Extension methods to <see cref="PDO"/>.
    /// </summary>
    [PhpHidden]
    public static class PdoExtension
    {
        /// <summary>
        /// Gets last created command.
        /// </summary>
        public static DbCommand GetCurrentCommand(this PDO pdo) => pdo.CurrentCommand;

        /// <summary>
        /// Creates command using provided PDO driver.
        /// </summary>
        public static DbCommand CreateCommand(this PDO pdo, string statement) => pdo.CreateCommand(statement);

        /// <summary>
        /// Closes pending data reader if any.
        /// </summary>
        public static void ClosePendingReader(this PDO pdo) => pdo.ClosePendingReader();

        /// <summary>
        /// Gets underlying <see cref="DbConnection"/> of a <see cref="PDO"/> object.
        /// </summary>
        /// <typeparam name="TConnection"></typeparam>
        /// <param name="pdo">PDO object instance.</param>
        /// <returns>Underlying connection. Can be <c>null</c>.</returns>
        public static TConnection GetCurrentConnection<TConnection>(this PDO pdo) where TConnection : DbConnection => pdo.Connection as TConnection;
    }
}
