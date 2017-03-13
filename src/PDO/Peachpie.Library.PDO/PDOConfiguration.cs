using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDO configuration
    /// </summary>
    /// <seealso cref="Pchp.Core.IPhpConfiguration" />
    public class PDOConfiguration : IPhpConfiguration
    {
        /// <inheritDoc />
        public IPhpConfiguration Copy() => (PDOConfiguration)this.MemberwiseClone();

        /// <inheritDoc />
        public string ExtensionName => "pdo";

        //public NameValueCollection Alias { get; set; } = new NameValueCollection();

    }
}
