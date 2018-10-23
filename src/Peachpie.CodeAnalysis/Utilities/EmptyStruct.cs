using System;
using System.Collections.Generic;
using System.Text;
using Pchp.CodeAnalysis.Semantics;

namespace Peachpie.CodeAnalysis.Utilities
{
    /// <summary>
    /// Empty structure to be used in generic classes requiring return or argument type (such as <see cref="PhpOperationVisitor{TResult}"/>).
    /// </summary>
    public struct VoidStruct
    {
    }
}
