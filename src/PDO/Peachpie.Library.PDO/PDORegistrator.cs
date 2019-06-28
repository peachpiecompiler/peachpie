using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Peachpie.Library.PDO
{
    internal sealed class PDORegistrator
    {
        public PDORegistrator()
        {
            Context.RegisterConfiguration(new PDOConfiguration());
        }
    }
}
