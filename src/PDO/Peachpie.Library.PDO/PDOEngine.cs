using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDOEngine methods
    /// </summary>
    [PhpHidden]
    public static class PDOEngine
    {
        private static readonly Dictionary<string, IPDODriver> s_drivers = new Dictionary<string, IPDODriver>();

        /// <summary>
        /// Registers the driver.
        /// </summary>
        public static void RegisterDriver(IPDODriver driver)
        {
            lock (s_drivers)
            {
                if (!s_drivers.ContainsKey(driver.Name))
                {
                    s_drivers.Add(driver.Name, driver);
                }
            }
        }

        internal static string[] GetDriverNames()
        {
            lock (s_drivers)
            {
                return s_drivers.Keys.ToArray();
            }
        }

        internal static IPDODriver GetDriver(string driverName)
        {
            lock (s_drivers)
            {
                if (!s_drivers.ContainsKey(driverName))
                    return null;
                return s_drivers[driverName];
            }
        }
    }
}
