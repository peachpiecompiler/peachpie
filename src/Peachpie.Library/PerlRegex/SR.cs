using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.PerlRegex
{
    /// <summary>
    /// Temporary resources for .NET Regex implementation.
    /// </summary>
    internal class SR
    {
        public const string UnrecognizedGrouping = "";
        public const string UnterminatedComment = "";
        public const string OnlyAllowedOnce = "";
        public const string BeginIndexNotNegative = "";
        public const string LengthNotNegative = "";
        public const string MalformedReference = "{0}";
        public const string UndefinedReference = "{0}";
        public const string AlternationCantHaveComment = "";
        public const string AlternationCantCapture = "";
        public const string IllegalEndEscape = "";
        public const string MalformedNameRef = "";
        public const string UndefinedBackref = "{0}";
        public const string UndefinedSubpattern = "{0}";
        public const string CaptureGroupOutOfRange = "";
        public const string MissingControl = "";
        public const string UnrecognizedControl = "";
        public const string IncompleteSlashP = "";
        public const string MalformedSlashP = "";
        public const string IllegalCondition = "";
        public const string TooManyAlternates = "";
        public const string MakeException = "{0}{1}";
        public const string UnterminatedBracket = "";
        public const string InvalidGroupName = "";
        public const string CapnumNotZero = "";
        public const string SubtractionMustBeLast = "";
        public const string NestedQuantify = "{0}";
        public const string ReversedCharRange = "";
        public const string BadClassInCharRange = "{0}";
        public const string NotEnoughParens = "";
        public const string IllegalRange = "";
        public const string InternalError = "";
        public const string QuantifyAfterNothing = "";
        public const string TooManyParens = "";
        public const string UndefinedNameRef = "{0}";
        public const string UnrecognizedEscape = "{0}";
        public const string TooFewHex = "";
        public const string NotSupported_ReadOnlyCollection = "";
        public const string Arg_ArrayPlusOffTooSmall = "";
        public const string EnumNotStarted = "";
        public const string UnknownProperty = "{0}";
        public const string UnexpectedOpcode = "{0}";
        public const string UnimplementedState = "";
        public const string NoResultOnFailed = "";
        public const string CountTooSmall = "";
        public const string RegexMatchTimeoutException_Occurred = "";
        public const string ReplacementError = "";

        public static string Format(string str, params object[] ps) => string.Format(str, ps);
    }
}
