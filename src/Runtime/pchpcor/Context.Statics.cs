using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// If implemented on a request-static holder,
    /// context performs its one-time initialization.
    /// </summary>
    public interface IStaticInit
    {
        /// <summary>
        /// One-time initialization routine called by context when instance is created.
        /// </summary>
        void Init(Context ctx);
    }

    partial class Context
    {
        // .. move GetStatic here
    }
}
