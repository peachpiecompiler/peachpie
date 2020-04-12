using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    [PhpExtension("bcmath")]
    public static class BCMath
    {
        sealed class BCMathOptions
        {
            public const int DefaultScale = 0;

            /// <summary>
            /// <c>bcscale()</c> value.
            /// </summary>
            public int Scale { get; set; } = DefaultScale;
        }

        static int GetCurrentScale(Context ctx)
        {
            return ctx.TryGetStatic<BCMathOptions>(out var options)
                ? options.Scale
                : BCMathOptions.DefaultScale;
        }

        static void SetCurrentScale(Context ctx, int scale)
        {
            ctx.GetStatic<BCMathOptions>().Scale = Math.Max(scale, 0);
        }

        //function bcadd
        //function bcsub
        //function bcmul
        //function bcdiv
        //function bcmod
        //function bcpow
        //function bcsqrt
        //function bcscale
        //function bccomp
        //function bcpowmod
    }
}
