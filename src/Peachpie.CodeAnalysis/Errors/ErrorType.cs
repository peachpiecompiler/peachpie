using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Errors
{
    internal class ErrorType
    {
        private readonly string messageId;
        private readonly string formatString;

        public ErrorType(ErrorCode code)
        {
            this.Id = (int)code;
            this.Severity = code.ToString().StartsWith("WRN_") ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error;
            this.messageId = code.ToString();

            Debug.Assert(ErrorStrings.ResourceManager.GetString(this.messageId) != null, $"String '{this.messageId}' could not be found in resources!");
        }

        public ErrorType(int id, DiagnosticSeverity severity, string formatString)
        {
            this.Id = id;
            this.Severity = severity;
            this.formatString = formatString;
        }

        public int Id { get; private set; }

        public DiagnosticSeverity Severity { get; private set; }

        public string FormatString
        {
            get
            {
                // To handle caching and possible explicit creation of this class with the error string message
                if (this.formatString == null)
                {
                    Debug.Assert(this.messageId != null);
                    ErrorStrings.ResourceManager.GetString(this.messageId);
                }

                return this.formatString;
            }
        }

        public PhpDiagnosticInfo CreateDiagnosticInfo(object[] args) => new PhpDiagnosticInfo(this.Id, args);

        public PhpDiagnosticInfo CreateDiagnosticInfo(bool isWarningAsError, object[] args) =>
            new PhpDiagnosticInfo(isWarningAsError, this.Id, args);
    }
}