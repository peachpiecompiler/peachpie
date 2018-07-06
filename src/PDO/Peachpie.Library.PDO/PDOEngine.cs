using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDOEngine methods
    /// </summary>
    [PhpHidden]
    public static class PDOEngine
    {
        /// <summary>
        /// Gets set of loaded PDO drivers.
        /// </summary>
        static Dictionary<string, IPDODriver> GetDrivers()
        {
            if (s_lazydrivers == null)
            {
                Interlocked.CompareExchange(ref s_lazydrivers, CollectPdoDrivers(), null);
            }

            return s_lazydrivers;
        }
        static Dictionary<string, IPDODriver> s_lazydrivers;

        static Dictionary<string, IPDODriver> CollectPdoDrivers()
        {
            return Context
                .CompositionContext
                .GetExports<IPDODriver>()
                .ToDictionary(driver => driver.Name, StringComparer.OrdinalIgnoreCase);
        }

        internal static IReadOnlyCollection<string> GetDriverNames() => GetDrivers().Keys;

        internal static IPDODriver TryGetDriver(string driverName)
        {
            GetDrivers().TryGetValue(driverName, out var driver);
            return driver;
        }
    }
}
