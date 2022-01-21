using System;
using System.Data;
using System.Reflection;
using MySqlConnector;

namespace Peachpie.Library.PDO.MySQL
{
    /// <summary>
    /// Helper MySql methods.
    /// </summary>
    public static class MySqlExtensions
    {
        /// <summary>
        /// Returns the last insert ID from an IDbCommand by unwrapping it to the internal MySqlCommand
        /// </summary>
        /// <param name="command">Generic IDbCommand to work with</param>
        /// <returns>Last insert ID</returns>
        public static long LastInsertedId(
            IDbCommand command)
        {
            // If we have a MySqlCommand, just use it
            if (command is MySqlCommand mySqlCommand) return mySqlCommand.LastInsertedId;

            // If we did not get one back, try to unwrap it as likely it's wrapped by a profiler like MiniProfiler
            if (_innerCommandMethod == null)
                _innerCommandMethod = command.GetType().GetMethod("get_InternalCommand", BindingFlags.Instance | BindingFlags.Public);
            mySqlCommand = _innerCommandMethod?.Invoke(command, null) as MySqlCommand;
            if (mySqlCommand == null) throw new NullReferenceException("Could not get internal command for wrapped command!");
            return mySqlCommand.LastInsertedId;
        }
        private static MethodInfo _innerCommandMethod;
    }
}
