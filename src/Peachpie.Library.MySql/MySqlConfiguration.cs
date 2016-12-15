using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.MySql
{
    class MySqlConfiguration : IPhpConfiguration
    {
        IPhpConfiguration IPhpConfiguration.Copy() => (MySqlConfiguration)this.MemberwiseClone();

        public int ConnectTimeout { get; set; } = 0;

        public int MaxPoolSize { get; set; } = 100;

        public int DefaultCommandTimeout { get; set; } = -1;

        public string Server { get; set; } = "localhost";

        public int Port { get; set; } = 3306;

        public string User { get; set; } = "root";

        public string Password { get; set; } = "";

        internal string ConnectionString { get; set; } = null;
    }
}
