using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDO static functions
    /// </summary>
    [PhpExtension(PDOConfiguration.PdoExtensionName)]
    public static class PDOStatic
    {
        /// <summary>
        /// Get the known PDO drivers
        /// </summary>
        /// <returns></returns>
        public static PhpArray pdo_drivers() => new PhpArray(PDOEngine.GetDriverNames());
    }
}
