using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Additional binary operation types to <see cref="BinaryOperationKind"/>
    /// </summary>
    enum BinaryPhpOperationKind
    {
        First = 1000,

        OperatorConditionalXor,

        Identical,
        NotIdentical,
    }
}
