using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// Delegate of extension methods added to PDO
    /// </summary>
    /// <param name="pdo">The pdo instance.</param>
    /// <param name="args">The arguments when method is called.</param>
    /// <returns>Method return value</returns>
    public delegate PhpValue ExtensionMethodDelegate(PDO pdo, PhpArray args);
}
