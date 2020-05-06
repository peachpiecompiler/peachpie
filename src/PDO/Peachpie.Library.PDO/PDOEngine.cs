using Pchp.Core;
using System;
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

        /// <summary>
        /// Gets set of loaded PDO drivers.
        /// </summary>
        static Dictionary<string, PDODriver> GetDrivers()
        {
            if (s_lazydrivers == null)
            {
                Interlocked.CompareExchange(ref s_lazydrivers, CollectPdoDrivers(), null);
            }

            return s_lazydrivers;
        }
        static Dictionary<string, PDODriver> s_lazydrivers;

        static Dictionary<string, PDODriver> CollectPdoDrivers()
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

            return drivertypes
                .Select(t => (PDODriver)Activator.CreateInstance(t))
                .ToDictionary(driver => driver.Name, StringComparer.OrdinalIgnoreCase);
        }

        internal static IReadOnlyCollection<string> GetDriverNames() => GetDrivers().Keys;

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
    }
}
