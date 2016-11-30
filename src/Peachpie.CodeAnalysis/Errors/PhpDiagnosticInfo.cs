using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Errors
{
    internal class PhpDiagnosticInfo : DiagnosticInfo
    {
        internal PhpDiagnosticInfo(int errorCode, object[] args)
            : base(Pchp.CodeAnalysis.Errors.MessageProvider.Instance, errorCode, args)
        {
        }

        internal PhpDiagnosticInfo(bool isWarningAsError, int errorCode, object[] args)
            : base(Pchp.CodeAnalysis.Errors.MessageProvider.Instance, isWarningAsError, errorCode, args)
        {
        }
    }
}
