using Pchp.Core.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    #region PhpError, PhpErrorSets

    /// <summary>
    /// Set of error types.
    /// </summary>
    [Flags]
    public enum PhpError
    {
        /// <summary>Error.</summary>
        E_ERROR = 1,
        /// <summary>Warning.</summary>
        E_WARNING = 2,
        /// <summary>Parse error.</summary>
        E_PARSE = 4,
        /// <summary>Notice.</summary>
        E_NOTICE = 8,
        /// <summary>Core error.</summary>
        E_CORE_ERROR = 16,
        /// <summary>Core warning.</summary>
        E_CORE_WARNING = 32,
        /// <summary>Compile error.</summary>
        E_COMPILE_ERROR = 64,
        /// <summary>Compile warning.</summary>
        E_COMPILE_WARNING = 128,
        /// <summary>User error.</summary>
        E_USER_ERROR = 256,
        /// <summary>User warning.</summary>
        E_USER_WARNING = 521,
        /// <summary>User notice.</summary>
        E_USER_NOTICE = 1024,
        /// <summary>Strict error.</summary>
        E_STRICT = 2048,
        /// <summary>E_RECOVERABLE_ERROR error.</summary>
        E_RECOVERABLE_ERROR = 4096,
        /// <summary>Deprecated error.</summary>
        E_DEPRECATED = 8192,
        /// <summary>Deprecated error.</summary>
        E_USER_DEPRECATED = 16384,

        /// <summary>All errors but strict.</summary>
        E_ALL = PhpErrorSets.AllButStrict,

        Warning = E_WARNING,
        Error = E_ERROR,
        Notice = E_NOTICE,
    }

    /// <summary>
    /// Sets of error types.
    /// </summary>
    [Flags]
    public enum PhpErrorSets
    {
        /// <summary>Empty error set.</summary>
        None = 0,

        /// <summary>Standard errors used by Core and Class Library.</summary>
        Standard = PhpError.E_ERROR | PhpError.E_WARNING | PhpError.E_NOTICE | PhpError.E_DEPRECATED,

        /// <summary>User triggered errors.</summary>
        User = PhpError.E_USER_ERROR | PhpError.E_USER_WARNING | PhpError.E_USER_NOTICE | PhpError.E_USER_DEPRECATED,

        /// <summary>Core system errors.</summary>
        System = PhpError.E_PARSE | PhpError.E_CORE_ERROR | PhpError.E_CORE_WARNING | PhpError.E_COMPILE_ERROR | PhpError.E_COMPILE_WARNING | PhpError.E_RECOVERABLE_ERROR,

        /// <summary>All possible errors except for the strict ones.</summary>
        AllButStrict = Standard | User | System,

        /// <summary>All possible errors. 30719 in PHP 5.3</summary>
        All = AllButStrict | PhpError.E_STRICT,

        /// <summary>Errors which can be handled by the user defined routine.</summary>
        Handleable = (User | Standard) & ~PhpError.E_ERROR,

        /// <summary>Errors which causes termination of a running script.</summary>
        Fatal = PhpError.E_ERROR | PhpError.E_COMPILE_ERROR | PhpError.E_CORE_ERROR | PhpError.E_USER_ERROR,
    }

    #endregion

    #region PhpException

    public static class PhpException
    {
        public static void Throw(PhpError error, string formatString, params string[] args)
        {
            Debug.Assert((error & (PhpError)PhpErrorSets.Fatal) == 0, string.Format(formatString, args));   // assert in debug mode to handle exception

            // TODO: get current Context from execution context
            // TODO: throw error according to configuration
        }

        /// <summary>
        /// Invalid argument error.
        /// </summary>
        /// <param name="argument">The name of the argument being invalid.</param>
        public static void InvalidArgument(string argument)
        {
            Throw(PhpError.Warning, ErrResources.invalid_argument, argument);
        }

        /// <summary>
        /// Invalid argument error with a description of a reason. 
        /// </summary>
        /// <param name="argument">The name of the argument being invalid.</param>
        /// <param name="message">The message - what is wrong with the argument. Must contain "{0}" which is replaced by argument's name.
        /// </param>
        public static void InvalidArgument(string argument, string message)
        {
            Debug.Assert(message.Contains("{0}"));
            Throw(PhpError.Warning, ErrResources.invalid_argument_with_message + message, argument);
        }

        /// <summary>
        /// Argument null error. Thrown when argument can't be null but it is.
        /// </summary>
        /// <param name="argument">The name of the argument.</param>
        public static void ArgumentNull(string argument)
        {
            Throw(PhpError.Warning, ErrResources.argument_null, argument);
        }

        /// <summary>
        /// The value of an argument is not invalid but unsupported.
        /// </summary>
        /// <param name="argument">The argument which value is unsupported.</param>
        /// <param name="value">The value which is unsupported.</param>
        public static void ArgumentValueNotSupported(string argument, object value)
        {
            Throw(PhpError.Warning, ErrResources.argument_value_not_supported, value?.ToString() ?? PhpVariable.TypeNameNull, argument);
        }

        /// <summary>
        /// Called function is not supported.
        /// </summary>
        /// <param name="function">Not supported function name.</param>
        public static void FunctionNotSupported(string/*!*/function)
        {
            Debug.Assert(!string.IsNullOrEmpty(function));

            Throw(PhpError.Warning, ErrResources.notsupported_function_called, function);
        }

        /// <summary>
        /// Converts exception message (ending by dot) to error message (not ending by a dot).
        /// </summary>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <returns>The error message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exceptionMessage"/> is a <B>null</B> reference.</exception>
        public static string ToErrorMessage(string exceptionMessage)
        {
            if (exceptionMessage == null) throw new ArgumentNullException("exceptionMessage");
            return exceptionMessage.TrimEnd(new char[] { '.' });
        }
    }

    #endregion
}
