using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library
{
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
        class ErrorContext
        {
            /// <summary>
            /// Stores user error handlers which has been rewritten by a new one.
            /// </summary>
            public Stack<ErrorHandlerRecord> OldUserErrorHandlers = null;

            /// <summary>
            /// Stores user exception handlers which has been rewritten by a new one.
            /// </summary>
            public Stack<ErrorHandlerRecord> OldUserExceptionHandlers = null;

            /// <summary>
            /// Errors to be reported to user.
            /// Ignored when <see cref="Context.ErrorReportingDisabled"/> is <c>true</c>.
            /// </summary>
            public PhpError ReportErrors = (PhpError)PhpErrorSets.All;
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
        public static int error_reporting(Context ctx, int level)
        {
            if ((level & (int)PhpErrorSets.All) == 0 && level != 0)
            {
                //PhpException.InvalidArgument("level");
                throw new ArgumentException(nameof(level));
            }

            var errctx = GetErrorContext(ctx);
            var result = (int)errctx.ReportErrors;
            errctx.ReportErrors = (PhpError)level & (PhpError)PhpErrorSets.All;
            return result;
        }

        /// <summary>
        /// Internal record in the error handler stack.
        /// </summary>
        [DebuggerDisplay("{ErrorHandler,nq}, {ErrorTypes}")]
        class ErrorHandlerRecord
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

            var errctx = GetErrorContext(ctx);

            //var old_handler = ctx.UserErrorHandler;
            //var old_handlers = errctx.OldUserErrorHandlers;

            //// previous handler was defined by user => store it into the stack:
            //if (old_handler != null)
            //{
            //    if (old_handlers == null)
            //    {
            //        old_handlers = new Stack(5);
            //        RequestContext.RequestEnd += new Action(ClearOldUserHandlers);
            //    }
            //    old_handlers.Push(new ErrorHandlerRecord(old_handler, old_errors));
            //}

            //// sets the current handler:
            //Configuration.Local.ErrorControl.UserHandler = newHandler;
            //Configuration.Local.ErrorControl.UserHandlerErrors = (PhpError)errorTypes;

            //// returns the previous handler:
            //return (old_handler != null) ? old_handler.ToPhpRepresentation() : null;
            return PhpValue.Null;
        }

        /// <summary>
        /// Restores the previous user error handler if there was any.
        /// </summary>
        public static bool restore_error_handler(Context ctx)
        {
            var errctx = GetErrorContext(ctx);

            //// if some user handlers has been stored in the stack then restore the top-most, otherwise set to null:
            //if (OldUserErrorHandlers != null && OldUserErrorHandlers.Count > 0)
            //{
            //    ErrorHandlerRecord record = (ErrorHandlerRecord)OldUserErrorHandlers.Pop();

            //    Configuration.Local.ErrorControl.UserHandler = record.ErrorHandler;
            //    Configuration.Local.ErrorControl.UserHandlerErrors = record.ErrorTypes;
            //}
            //else
            //{
            //    Configuration.Local.ErrorControl.UserHandler = null;
            //    Configuration.Local.ErrorControl.UserHandlerErrors = (PhpError)PhpErrorSet.None;
            //}

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
            if (newHandler is PhpCallback && !((PhpCallback)newHandler).IsValid) return PhpValue.Null;

            var errctx = GetErrorContext(ctx);

            //PhpCallback old_handler = Configuration.Local.ErrorControl.UserExceptionHandler;

            //// previous handler was defined by user => store it into the stack:
            //if (old_handler != null)
            //{
            //    if (OldUserExceptionHandlers == null)
            //    {
            //        OldUserExceptionHandlers = new Stack(5);
            //        RequestContext.RequestEnd += new Action(ClearOldUserHandlers);
            //    }
            //    OldUserExceptionHandlers.Push(old_handler);
            //}

            //// sets the current handler:
            //Configuration.Local.ErrorControl.UserExceptionHandler = newHandler;

            //// returns the previous handler:
            //return (old_handler != null) ? old_handler.ToPhpRepresentation() : null;
            return PhpValue.Null;
        }

        /// <summary>
        /// Restores the previous user error handler if there was any.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        public static bool restore_exception_handler(Context ctx)
        {
            //if (OldUserExceptionHandlers != null && OldUserExceptionHandlers.Count > 0)
            //    Configuration.Local.ErrorControl.UserExceptionHandler = (PhpCallback)OldUserExceptionHandlers.Pop();
            //else
            //    Configuration.Local.ErrorControl.UserExceptionHandler = null;

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
        public static bool trigger_error(Context ctx, string error_msg, int error_type = E_USER_NOTICE)
        {
            // not implemented

            return true;
        }

        /// <summary>
        /// Alias of <see cref="trigger_error"/>.
        /// </summary>
        public static bool user_error(Context ctx, string error_msg, int error_type = E_USER_NOTICE) => trigger_error(ctx, error_msg, error_type);

        public const int DEBUG_BACKTRACE_PROVIDE_OBJECT = 1;
        public const int DEBUG_BACKTRACE_IGNORE_ARGS = 2;

        /// <summary>
        /// Generates a backtrace.
        /// </summary>
        public static PhpArray debug_backtrace(int options = 0, int limit = 0)
        {
            // not implemented

            return PhpArray.NewEmpty();
        }

        /// <summary>
        /// Prints a backtrace.
        /// </summary>
        public static void debug_print_backtrace(Context ctx, int options = 0, int limit = 0)
        {
            // not implemented
        }

        /// <summary>
        /// Send an error message to the defined error handling routines.
        /// </summary>
        public static bool error_log(string message, int message_type = 0, string destination = null, string extra_headers = null)
        {
            // not implemented

            return false;
        }
    }
}
