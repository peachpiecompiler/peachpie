using System;
using System.Collections.Generic;
using System.Text;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceCustomAttribute
    {
        /// <summary>
        /// Associated  <see cref="TypeRefContext"/> instance.
        /// </summary>
        internal TypeRefContext TypeCtx { get; }
    }
}
