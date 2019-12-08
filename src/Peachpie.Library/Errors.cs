using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library
{
    [PhpExtension("Core")]
    public static class Errors
    {
        #region Constants

        /// <summary>Error.</summary>
        public const int E_ERROR = (int)PhpError.E_ERROR;
        /// <summary>Warning.</summary>
        public const int E_WARNING = (int)PhpError.E_WARNING;
        /// <summary>Parse error.</summary>
        public const int E_PARSE = (int)PhpError.E_PARSE;
        /// <summary>Notice.</summary>
        public const int E_NOTICE = (int)PhpError.E_NOTICE;
        /// <summary>Core error.</summary>
        public const int E_CORE_ERROR = (int)PhpError.E_CORE_ERROR;
        /// <summary>Core warning.</summary>
        public const int E_CORE_WARNING = (int)PhpError.E_CORE_WARNING;
        /// <summary>Compile error.</summary>
        public const int E_COMPILE_ERROR = (int)PhpError.E_COMPILE_ERROR;
        /// <summary>Compile warning.</summary>
        public const int E_COMPILE_WARNING = (int)PhpError.E_COMPILE_WARNING;
        /// <summary>User error.</summary>
        public const int E_USER_ERROR = (int)PhpError.E_USER_ERROR;
        /// <summary>User warning.</summary>
        public const int E_USER_WARNING = (int)PhpError.E_USER_WARNING;
        /// <summary>User notice.</summary>
        public const int E_USER_NOTICE = (int)PhpError.E_USER_NOTICE;
        /// <summary>Strict error.</summary>
        public const int E_STRICT = (int)PhpError.E_STRICT;
        /// <summary>E_RECOVERABLE_ERROR error.</summary>
        public const int E_RECOVERABLE_ERROR = (int)PhpError.E_RECOVERABLE_ERROR;
        /// <summary>Deprecated error.</summary>
        public const int E_DEPRECATED = (int)PhpError.E_DEPRECATED;
        /// <summary>Deprecated error.</summary>
        public const int E_USER_DEPRECATED = (int)PhpError.E_USER_DEPRECATED;

        /// <summary>All errors but strict.</summary>
        public const int E_ALL = (int)PhpErrorSets.AllButStrict;

        #endregion

        #region ErrorContext

        /// <summary>
        /// Current context errors configuration.
        /// </summary>
        sealed class ErrorContext
        {
            /// <summary>
            /// Stores user error handlers which has been rewritten by a new one.
            /// </summary>
            Stack<ErrorHandlerRecord> _oldUserErrorHandlers = null;

            /// <summary>
            /// Stores user exception handlers which has been rewritten by a new one.
            /// </summary>
            Stack<ErrorHandlerRecord> _oldUserExceptionHandlers = null;

            static void StoreHandler(ref Stack<ErrorHandlerRecord> oldhandlers, ErrorHandlerRecord record)
            {
                Debug.Assert(record != null);

                if (oldhandlers == null)
                {
                    oldhandlers = new Stack<ErrorHandlerRecord>();
                }

                oldhandlers.Push(record);
            }

            static ErrorHandlerRecord RestoreHandler(ref Stack<ErrorHandlerRecord> oldhandlers)
            {
                return (oldhandlers != null && oldhandlers.Count != 0)
                    ? oldhandlers.Pop()
                    : null;
            }

            public void StoreErrorHandler(ErrorHandlerRecord record) => StoreHandler(ref _oldUserErrorHandlers, record);

            public ErrorHandlerRecord RestoreErrorHandler() => RestoreHandler(ref _oldUserErrorHandlers);

            public void StoreExceptionHandler(ErrorHandlerRecord record) => StoreHandler(ref _oldUserExceptionHandlers, record);

            public ErrorHandlerRecord RestoreExceptionHandler() => RestoreHandler(ref _oldUserExceptionHandlers);

            /// <summary>
            /// Errors to be reported to user.
            /// Ignored when <see cref="Context.ErrorReportingDisabled"/> is <c>true</c>.
            /// </summary>
            public PhpError ReportErrors { get; set; } = (PhpError)PhpErrorSets.All;
        }

        static ErrorContext GetErrorContext(Context ctx) => ctx.GetStatic<ErrorContext>();

        #endregion

        #region error_reporting, set_error_handler, restore_error_handler, set_exception_handler, restore_exception_handler

        /// <summary>
        /// Retrieves the current error reporting level.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <returns>
        /// The bitmask of error types which are reported. Returns 0 if error reporting is disabled
        /// by means of @ operator.
        /// </returns>
        public static int error_reporting(Context ctx)
        {
            return ctx.ErrorReportingDisabled ? 0 : (int)GetErrorContext(ctx).ReportErrors;
        }

        /// <summary>
        /// Sets a new level of error reporting.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="level">The new level.</param>
        /// <returns>The original level.</returns>
        public static int error_reporting(Context ctx, PhpError level)
        {
            if ((level & (PhpError)PhpErrorSets.All) == 0 && level != 0)
            {
                //PhpException.InvalidArgument("level");
                throw new ArgumentException(nameof(level));
            }

            var errctx = GetErrorContext(ctx);
            var result = (int)errctx.ReportErrors;
            errctx.ReportErrors = level & (PhpError)PhpErrorSets.All;
            return result;
        }

        /// <summary>
        /// Internal record in the error handler stack.
        /// </summary>
        [DebuggerDisplay("{ErrorHandler,nq}, {ErrorTypes}")]
        sealed class ErrorHandlerRecord
        {
            /// <summary>
            /// Error handler callback.
            /// </summary>
            public readonly IPhpCallable ErrorHandler;

            /// <summary>
            /// Error types to be handled.
            /// </summary>
            public readonly PhpError ErrorTypes;

            /// <summary>
            /// Public constructor of the class.
            /// </summary>
            /// <param name="handler">Error handler callback.</param>
            /// <param name="errors">Error types to be handled.</param>
            public ErrorHandlerRecord(IPhpCallable handler, PhpError errors)
            {
                Debug.Assert(handler != null);
                ErrorHandler = handler;
                ErrorTypes = errors;
            }
        }

        /// <summary>
        /// Sets user defined handler to handle errors.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="newHandler">The user callback called to handle an error.</param>
        /// <param name="errorTypes">Error types to be handled by the handler.</param>
        /// <returns>
        /// The PHP representation of previous user handler, <B>null</B> if there is no user one, or 
        /// <B>false</B> if <paramref name="newHandler"/> is invalid or empty.
        /// </returns>
        /// <remarks>
        /// Stores old user handlers on the stack so that it is possible to 
        /// go back to arbitrary previous user handler.
        /// </remarks>
        public static PhpValue set_error_handler(Context ctx, IPhpCallable newHandler, PhpErrorSets errorTypes = PhpErrorSets.Handleable)
        {
            if (newHandler == null) return PhpValue.Null;
            if (newHandler is PhpCallback && !((PhpCallback)newHandler).IsValid) return PhpValue.Null;

            var config = ctx.Configuration.Core;

            var oldhandler = config.UserErrorHandler;
            var oldtypes = config.UserErrorTypes;

            if (oldhandler != null)
            {
                GetErrorContext(ctx).StoreErrorHandler(new ErrorHandlerRecord(oldhandler, oldtypes));
            }

            config.UserErrorHandler = newHandler;
            config.UserErrorTypes = (PhpError)errorTypes;

            // returns the previous handler:
            return (oldhandler != null)
                ? oldhandler.ToPhpValue()
                : PhpValue.Null;
        }

        /// <summary>
        /// Restores the previous user error handler if there was any.
        /// </summary>
        public static bool restore_error_handler(Context ctx)
        {
            var record = GetErrorContext(ctx).RestoreErrorHandler();
            var config = ctx.Configuration.Core;

            if (record != null)
            {
                config.UserErrorHandler = record.ErrorHandler;
                config.UserErrorTypes = record.ErrorTypes;
            }
            else
            {
                config.UserErrorHandler = null;
                config.UserErrorTypes = 0;
            }

            return true;
        }

        /// <summary>
        /// Sets user defined handler to handle exceptions.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="newHandler">The user callback called to handle an exceptions.</param>
        /// <returns>
        /// The PHP representation of previous user handler, <B>null</B> if there is no user one, or 
        /// <B>false</B> if <paramref name="newHandler"/> is invalid or empty.
        /// </returns>
        /// <remarks>
        /// Stores old user handlers on the stack so that it is possible to 
        /// go back to arbitrary previous user handler.
        /// </remarks>
        public static PhpValue set_exception_handler(Context ctx, IPhpCallable newHandler)
        {
            if (newHandler == null) return PhpValue.Null;
            if (newHandler is PhpCallback callback && !callback.IsValid) return PhpValue.Null;

            var config = ctx.Configuration.Core;

            var old_handler = config.UserExceptionHandler;
            config.UserExceptionHandler = newHandler;

            // previous handler was defined by user => store it into the stack:
            if (old_handler != null)
            {
                GetErrorContext(ctx).StoreExceptionHandler(new ErrorHandlerRecord(old_handler, (PhpError)PhpErrorSets.All));

                // returns the previous handler:
                return old_handler.ToPhpValue();
            }
            else
            {
                return PhpValue.Null;
            }
        }

        /// <summary>
        /// Restores the previous user error handler if there was any.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <returns>This function always returns <c>TRUE</c>.</returns>
        public static bool restore_exception_handler(Context ctx)
        {
            ctx.Configuration.Core.UserExceptionHandler = GetErrorContext(ctx)
                .RestoreExceptionHandler()?
                .ErrorHandler;

            return true;
        }

        #endregion

        /// <summary>
        /// Generates a user-level error/warning/notice message.
        /// </summary>
        /// <remarks>
        /// Used to trigger a user error condition, it can be used in conjunction with the built-in error handler,
        /// or with a user defined function that has been set as the new error handler (set_error_handler()).
        /// This function is useful when you need to generate a particular response to an exception at runtime.
        /// </remarks>
        public static bool trigger_error(Context ctx, string error_msg, PhpError error_type = PhpError.E_USER_NOTICE)
        {
            if ((error_type & (PhpError)PhpErrorSets.User) == 0)
            {
                return false;
            }

            PhpException.TriggerError(ctx, error_type, error_msg);

            //
            return true;
        }

        /// <summary>
        /// Alias of <see cref="trigger_error"/>.
        /// </summary>
        public static bool user_error(Context ctx, string error_msg, PhpError error_type = PhpError.E_USER_NOTICE) => trigger_error(ctx, error_msg, error_type);

        public const int DEBUG_BACKTRACE_PROVIDE_OBJECT = 1;
        public const int DEBUG_BACKTRACE_IGNORE_ARGS = 2;

        /// <summary>
        /// Generates a backtrace.
        /// </summary>
        public static PhpArray debug_backtrace(int options = 0, int limit = 0)
        {
            // TODO: debug_backtrace: options
            return (new Core.Reflection.PhpStackTrace()).GetBacktrace(skip: 1, limit: (limit <= 0) ? int.MaxValue : limit);
        }

        /// <summary>
        /// Prints a backtrace.
        /// </summary>
        public static void debug_print_backtrace(Context ctx, int options = 0, int limit = 0)
        {
            // TODO: debug_backtrace: options, limit
            ctx.Echo((new Core.Reflection.PhpStackTrace()).GetStackTraceString(skip: 1));
        }

        /// <summary>
		/// An action performed by the <see cref="error_log"/> method.
		/// </summary>
		public enum ErrorLogType
        {
            /// <summary>A message to be logged is appended to log file or sent to system log.</summary>
            Default = 0,

            /// <summary>A message is sent by an e-mail.</summary>
            SendByEmail = 1,

            /// <summary>Not supported.</summary>
            ToDebuggingConnection = 2,

            /// <summary>A message is appended to a specified file.</summary>
            AppendToFile = 3,

            /// <summary>A message is sent directly to the SAPI logging handler.</summary>
            SAPI = 4,
        }

        /// <summary>
        /// Send an error message to the defined error handling routines.
        /// </summary>
        public static bool error_log(string message, ErrorLogType message_type = ErrorLogType.Default, string destination = null, string extra_headers = null)
        {
            // send to attached trace listener (attached debugger for instance):
            Trace.WriteLine(message, "PHP");

            // pass the message:
            switch (message_type)
            {
                case ErrorLogType.Default:
                    LogEventSource.Log.ErrorLog(message);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets information about the last error that occurred.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <returns>Array with error information or <c>null</c> if there is no error.</returns>
        public static PhpArray error_get_last(Context ctx)
        {
            // TODO: error_get_last
            return null;
        }

        /// <summary>
        /// Clear the most recent error.
        /// </summary>
        public static void error_clear_last(Context ctx)
        {
            // TODO
        }
    }
}
