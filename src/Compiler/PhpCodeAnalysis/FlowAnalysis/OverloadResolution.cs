using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    partial class OverloadResolution
    {
        /// <summary>
        /// Prefer instance methods.
        /// </summary>
        public void WithInstanceType(TypeRefContext ctx, TypeRefMask tmask)
        {
            if (!tmask.IsAnyType)
            {
                _isFinal |= (!tmask.IncludesSubclasses);    // instance type does not include subclasses -> call may not be virtual
            }
        }

        public void WithParametersType(TypeRefContext ctx, TypeRefMask[] ptypes)
        {
            WithParametersCount(ptypes.Length);

            // TODO: filter candidates which parameters are convertible from provided types
            // prefer single candidate matching types perfectly

            // var expected = s.GetExpectedParamType(ctx, index);
            // { tmask is convertible to expected} ?
            // any candidate with perfect match ?
        }
    }
}
