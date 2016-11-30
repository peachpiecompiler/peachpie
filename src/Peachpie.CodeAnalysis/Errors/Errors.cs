using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Errors
{
    internal static class Errors
    {
        private static Dictionary<int, ErrorType> errorTypes = new Dictionary<int, ErrorType>();

        public static readonly ErrorType ERR_MetadataFileNotFound = RegisterError(ErrorCode.ERR_MetadataFileNotFound);

        private static ErrorType RegisterError(ErrorCode code)
        {
            var errorType = new ErrorType(code);
            errorTypes.Add((int)code, errorType);

            return errorType;
        }

        public static ErrorType GetError(ErrorCode code)
        {
            return errorTypes[(int)code];
        }

        /// <remarks>
        /// Parser errors are included in this lookup.
        /// </remarks>
        public static ErrorType GetAnyError(int errorCode)
        {
            if (ParserErrors.IsParserError(errorCode))
            {
                return ParserErrors.GetError(errorCode);
            }
            else
            {
                return errorTypes[errorCode];
            }
        }
    }
}
