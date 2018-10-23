using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// An interface of symbols with a result value (field, routine, property).
    /// </summary>
    public interface IPhpValue
    {
        /// <summary>
        /// Optional. Gets the initializer.
        /// </summary>
        BoundExpression Initializer { get; }
    }
}
