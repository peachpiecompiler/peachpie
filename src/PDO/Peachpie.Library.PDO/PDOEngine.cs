using Pchp.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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
        /// List of known assembly names exporting implementations of <see cref="PDODriver"/> interface.
        /// CONSIDER: TODO: configuration ? We have the 'Context' so we can read the config there ...
        /// </summary>
        static string[] s_knownAssemblies => new[]
        {
            "Peachpie.Library.PDO.Firebird",
            "Peachpie.Library.PDO.IBM",
            "Peachpie.Library.PDO.MySQL",
            "Peachpie.Library.PDO.PgSQL",
            "Peachpie.Library.PDO.Sqlite",
            "Peachpie.Library.PDO.SqlSrv",
        };

        static ConcurrentDictionary<string, PDODriver> s_drivers = new ConcurrentDictionary<string, PDODriver>(StringComparer.OrdinalIgnoreCase);

        static int s_driversCollected;

        /// <summary>
        /// Gets set of loaded PDO drivers.
        /// </summary>
        static ConcurrentDictionary<string, PDODriver> GetDrivers()
        {
            if (s_driversCollected == 0)
            {
                CollectPdoDrivers();
                Interlocked.Increment(ref s_driversCollected);
            }

            return s_drivers;
        }

        static void CollectPdoDrivers()
        {
            var drivertypes = new List<Type>();

            foreach (var assname in s_knownAssemblies)
            {
                try
                {
                    var ass = Assembly.Load(new AssemblyName(assname));

                    drivertypes.AddRange(ass
                        .GetTypes()
                        .Where(t => !t.IsInterface && !t.IsValueType && !t.IsAbstract && typeof(PDODriver).IsAssignableFrom(t)));
                }
                catch
                {
                    // ignore
                }
            }

            // instantiate drivers:
            foreach (var t in drivertypes)
            {
                RegisterDriver((PDODriver)Activator.CreateInstance(t));
            }
        }

        internal static IEnumerable<string> GetDriverNames() => GetDrivers().Keys;

        internal static PDODriver TryGetDriver(string driverName)
        {
            return GetDrivers().TryGetValue(driverName, out var driver) || TryGetUriDriver(driverName, out driver) ? driver : null;
        }

        static bool TryGetUriDriver(string driverName, out PDODriver driver)
        {
            if (driverName == "uri")
            {
                throw new NotImplementedException("PDO uri DSN not implemented");

                //// Uri mode
                //if (Uri.TryCreate(connstring.ToString(), UriKind.Absolute, out var uri))
                //{
                //    if (uri.Scheme.Equals("file", StringComparison.Ordinal))
                //    {
                //        //return
                //    }
                //    else
                //    {
                //        throw new PDOException("PDO DSN as URI does not support other schemes than 'file'");
                //    }
                //}
                //else
                //{
                //    throw new PDOException("Invalid uri in DSN");
                //}
            }

            driver = null;
            return false;
        }

        /// <summary>
        /// Register a custom <see cref="PDODriver"/>.
        /// </summary>
        /// <param name="driver">Driver instance to be added. Cannot be <c>null</c>.</param>
        /// <returns>If driver was successfuly registered. Otherwise there was a driver with the same name already.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="driver"/> was <c>null</c> reference.</exception>
        public static bool RegisterDriver(PDODriver driver)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            return s_drivers.TryAdd(driver.Name, driver);
        }
    }
}
