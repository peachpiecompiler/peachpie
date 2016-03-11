using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceRoutineSymbol
    {
        /// <summary>
        /// Gets place referring to <c>Pchp.Core.Context</c> object.
        /// </summary>
        internal virtual IPlace GetContextPlace()
        {
            Debug.Assert(_params[0] is SpecialParameterSymbol && _params[0].Name == SpecialParameterSymbol.ContextName);
            return new ParamPlace(_params[0]);  // <ctx> 
        }
    }

    partial class SourceMethodSymbol
    {
        internal override IPlace GetContextPlace()
        {
            // TODO: <this>.<ctx> in instance methods
            return base.GetContextPlace();
        }
    }
}