using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Peachpie.AspNetCore.Web
{
    static class HttpContextHelpers
    {
        /// <summary>
        /// Parses samesite option value.
        /// </summary>
        /// <param name="samesite">One of: None|Lax|Strict. Case insensitive.</param>
        /// <param name="mode">Parsed value.</param>
        /// <returns>Whether the value was parsed.</returns>
        public static bool TryParseSameSite(string samesite, out SameSiteMode mode)
        {
            if (samesite != null)
            {
                // return s_options.TryGetValue(samesite, out mode); // overhead

                // return Enum.TryParse<SameSiteMode>(samesite, true, out mode); // accepts numbers as well

                if (samesite.Equals(nameof(SameSiteMode.None), StringComparison.OrdinalIgnoreCase))
                {
                    mode = SameSiteMode.None;
                    return true;
                }

                if (samesite.Equals(nameof(SameSiteMode.Lax), StringComparison.OrdinalIgnoreCase))
                {
                    mode = SameSiteMode.Lax;
                    return true;
                }

                if (samesite.Equals(nameof(SameSiteMode.Strict), StringComparison.OrdinalIgnoreCase))
                {
                    mode = SameSiteMode.Strict;
                    return true;
                }
            }

            // default
            mode = SameSiteMode.Lax;
            return false;
        }
    }
}
