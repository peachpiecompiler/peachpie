using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    /// <summary>
    /// Helper provider giving an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of object that the provider gets.</typeparam>
    interface IProvider<T>
    {
        /// <summary>
        /// Gets or creates object of type <typeparamref name="T"/>.
        /// </summary>
        T Create();
    }
}
