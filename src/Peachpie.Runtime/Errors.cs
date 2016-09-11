using System;
using System.Collections.Generic;
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
            // TODO: get current Context from execution context
            // TODO: throw error according to configuration
        }
    }

    #endregion
}
