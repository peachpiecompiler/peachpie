using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.AspNetCore.Web
{
    /// <summary>
    /// Request end reason.
    /// </summary>
    internal enum RequestCompletionReason
    {
        None = 0,
        Finished,
        ForceEnd,
        Timeout,
    }
}
