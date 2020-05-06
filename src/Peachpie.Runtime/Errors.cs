using Pchp.Core.Reflection;
using Pchp.Core.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
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

    /// <summary>
    /// Provides standard PHP errors.
    /// </summary>
    [DebuggerNonUserCode]
    public static class PhpException
    {
        static string PeachpieLibraryAssembly => "Peachpie.Library";
        static string ErrorClass => "Pchp.Library.Spl.Error";
        static string TypeErrorClass => "Pchp.Library.Spl.TypeError";
        static string ArgumentCountErrorClass => "Pchp.Library.Spl.ArgumentCountError";
        static string AssertionErrorClass => "Pchp.Library.Spl.AssertionError";

        static Type _Error, _TypeError, _AssertionError, _ArgumentCountError;

        static Exception Exception(ref Type _type, string _typename, string message)
        {
            if (_type == null)
            {
                _type = Type.GetType(Assembly.CreateQualifiedName(PeachpieLibraryAssembly, _typename), throwOnError: false) ?? typeof(Exception);
            }

            return (Exception)Activator.CreateInstance(
                _type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding,
                null,
                new[] { message },
                null);
        }

        public static Exception ErrorException(string formatstring, string arg1) => ErrorException(string.Format(formatstring, arg1));

        public static Exception ErrorException(string formatstring, string arg1, string arg2) => ErrorException(string.Format(formatstring, arg1, arg2));

        public static Exception ErrorException(string message) => Exception(ref _Error, ErrorClass, message);

        public static Exception TypeErrorException() => TypeErrorException(string.Empty);

        public static Exception TypeErrorException(string message) => Exception(ref _TypeError, TypeErrorClass, message);

        public static Exception ArgumentCountErrorException(string message) => Exception(ref _ArgumentCountError, ArgumentCountErrorClass, message);

        public static Exception AssertionErrorException(string message) => Exception(ref _AssertionError, AssertionErrorClass, message);

        internal static Exception ClassNotFoundException(string classname) => ErrorException(ErrResources.class_not_found, classname);

        /// <summary>
        /// Triggers the error by passing it to
        /// the user handler first (<see cref="PhpCoreConfiguration.UserErrorHandler"/> and then to
        /// the internal handler (<see cref="Throw(PhpError, string)"/>.
        /// </summary>
        public static void TriggerError(Context ctx, PhpError error, string message)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            if (message == null)
            {
                message = string.Empty;
            }

            // try the user handler
            var config = ctx.Configuration.Core;
            if (config.UserErrorHandler != null && (config.UserErrorTypes & error) != 0)
            {
                var trace = new PhpStackTrace();

                if (!config.UserErrorHandler.Invoke(ctx, (int)error, message, trace.GetFilename(), trace.GetLine(), PhpValue.Null).IsFalse)
                {
                    return;
                }
            }

            // fallback to internal handler
            Throw(error, message);
        }

        public static void Throw(PhpError error, string message)
        {
            Trace.WriteLine(message, $"PHP ({error})");

            if ((error & (PhpError)PhpErrorSets.Fatal) != 0)
            {
                LogEventSource.Log.HandleFatal(message);

                // terminate the script
                throw new InvalidOperationException(message);
            }
            else
            {
                LogEventSource.Log.HandleWarning(message);
            }
        }

        public static void Throw(PhpError error, string formatString, params string[] args) => Throw(error, string.Format(formatString, args));

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
        /// An argument violates a type hint.
        /// </summary>
        /// <param name="argName">The name of the argument.</param>
        /// <param name="typeName">The name of the hinted type.</param>
        public static void InvalidArgumentType(string argName, string typeName)
        {
            Throw(PhpError.Error, ErrResources.invalid_argument_type, argName, typeName);
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
        /// Outputs warning: Illegal offset type.
        /// </summary>
        public static void IllegalOffsetType()
        {
            Throw(PhpError.Warning, ErrResources.illegal_offset_type);
        }

        /// <summary>
        /// Outputs error: Undefined offset ({0}).
        /// </summary>
        public static void UndefinedOffset(IntStringKey key)
        {
            Throw(PhpError.Error, string.Format(ErrResources.undefined_offset, key.ToString()));
        }

        /// <summary>
        /// Argument type mismatch error.
        /// </summary>
        public static void ThrowIfArgumentNull(object value, int arg)
        {
            if (ReferenceEquals(value, null))
            {
                // PHP: TypeError: Argument {arg} passed to {methodname} must be an instance of {expected}, null given

                // throw new TypeError
                throw TypeErrorException(string.Format(ErrResources.argument_null, arg));
            }
        }

        public static void ThrowIfArgumentNotCallable(Context ctx, RuntimeTypeHandle callerCtx, PhpValue value, bool nullAllowed, int arg)
        {
            if (nullAllowed && value.IsNull)
                return;

            var callable = value.AsCallable(callerCtx);
            if (callable == null || (callable is PhpCallback phpcallback && !phpcallback.IsValidBound(ctx)))
            {
                throw TypeErrorException(string.Format(ErrResources.argument_not_callable, arg, PhpVariable.GetTypeName(value)));
            }
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
        /// Emitted to a library function call which has invalid actual argument count.
        /// </summary>
        public static void InvalidArgumentCount(string typeName, string methodName)
        {
            if (methodName != null)
            {
                if (typeName != null)
                {
                    Throw(PhpError.Warning, string.Format(ErrResources.invalid_argument_count_for_method, typeName, methodName));
                }
                else
                {
                    Throw(PhpError.Warning, string.Format(ErrResources.invalid_argument_count_for_function, methodName));
                }
            }
            else
            {
                Throw(PhpError.Warning, ErrResources.invalid_argument_count);
            }
        }

        /// <summary>
        /// Emitted to the foreach statement if the variable to be enumerated doesn't implement 
        /// the <see cref="IPhpEnumerable"/> interface.
        /// </summary>
        public static void InvalidForeachArgument()
        {
            Throw(PhpError.Warning, ErrResources.invalid_foreach_argument);
        }

        /// <summary>
        /// Emitted to the function call if an argument cannot be implicitly casted.
        /// </summary>
        /// <param name="argument">The argument name which is casted.</param>
        /// <param name="targetType">The type to which is casted.</param>
        /// <param name="functionName">The name of the function called.</param>
        public static void InvalidImplicitCast(string argument, string targetType, string functionName)
        {
            Throw(PhpError.Warning, ErrResources.invalid_implicit_cast, argument, targetType, functionName);
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
        /// Called function is not supported.
        /// </summary>
        /// <param name="function">Not supported function name.</param>
        public static void FunctionDeprecated(string/*!*/function)
        {
            Debug.Assert(!string.IsNullOrEmpty(function));

            Throw(PhpError.E_DEPRECATED, ErrResources.function_is_deprecated, function);
        }

        /// <summary>
        /// Call to a member function <paramref name="methodName"/>() on a non-object.
        /// </summary>
        /// <param name="methodName">The method name.</param>
        public static void MethodOnNonObject(string methodName)
        {
            throw ErrorException(string.Format(ErrResources.method_called_on_non_object, methodName));
        }

        /// <summary>new Error("Call to undefined function {<paramref name="funcName"/>}()")</summary>
        /// <param name="funcName">The function name.</param>
        public static void UndefinedFunctionCalled(string funcName)
        {
            throw ErrorException(string.Format(ErrResources.undefined_function_called, funcName));
        }

        /// <summary>new Error("Call to undefined function {<paramref name="funcName"/>}()")</summary>
        /// <param name="typeName">Class name.</param>
        /// <param name="funcName">The function name.</param>
        public static void UndefinedMethodCalled(string typeName, string funcName)
        {
            throw ErrorException(string.Format(ErrResources.undefined_method_called, typeName, funcName));
        }

        /// <summary>
        /// NOTICE: Notice: Undefined property {0}::${1}.
        /// </summary>
        internal static void UndefinedProperty(string className, string propertyName)
        {
            // NOTICE: Notice: Undefined property {0}::${1}
            Throw(PhpError.Notice, ErrResources.undefined_property_accessed, className, propertyName);
        }

        /// <summary>
        /// Reports an error when a variable should be PHP object but it is not.
        /// </summary>
        /// <param name="reference">Whether a reference modifier (=&amp;) is used.</param>
        /// <param name="var">The variable which was misused.</param>
        /// <exception cref="PhpException"><paramref name="var"/> is <see cref="PhpArray"/> (Warning).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is scalar type (Warning).</exception>
        /// <exception cref="PhpException"><paramref name="var"/> is a string (Warning).</exception>
        public static void VariableMisusedAsObject(PhpValue var, bool reference)
        {
            if (var.IsEmpty)
            {
                Throw(PhpError.Notice, ErrResources.empty_used_as_object);
            }
            else if (var.IsArray)
            {
                Throw(PhpError.Warning, ErrResources.array_used_as_object);
            }
            else if (var.TypeCode == PhpTypeCode.String || var.TypeCode == PhpTypeCode.MutableString)
            {
                Throw(PhpError.Warning, reference ? ErrResources.string_item_used_as_reference : ErrResources.string_used_as_object);
            }
            else if (var.IsAlias)
            {
                VariableMisusedAsObject(var.Alias.Value, reference);
            }
            else
            {
                Throw(PhpError.Warning, ErrResources.scalar_used_as_object, PhpVariable.GetTypeName(var));
            }
        }

        /// <summary>
        /// Recoverable fatal error: Object of class X could not be converted to string.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        public static void ObjectToStringNotSupported(object instance)
        {
            var classname = (instance != null)
                ? instance.GetPhpTypeInfo().Name
                : PhpVariable.TypeNameNull;

            ObjectConvertError(classname, PhpVariable.TypeNameString);
        }

        /// <summary>
        /// Recoverable fatal error: Object of class {0} could not be converted to {1}.
        /// </summary>
        public static void ObjectConvertError(string classname, string targettype)
        {
            // TODO: Recoverable fatal error
            Throw(PhpError.Error, ErrResources.object_could_not_be_converted, classname, targettype);
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

        internal static void ThrowSelfOutOfClass()
        {
            Throw(PhpError.Error, ErrResources.self_used_out_of_class);
            throw new ArgumentException(ErrResources.self_used_out_of_class);
        }

        /// <summary>
        /// Internal warning when new element cannot be added to array because there are no more free keys.
        /// </summary>
        internal static void NextArrayKeyUnavailable()
        {
            // Warning: Cannot add element to the array as the next element is already occupied
            Throw(PhpError.Warning, ErrResources.next_array_key_unavailable);
        }
    }

    #endregion

    #region LogEventSource

    /// <summary>
    /// Provides logging of errors into system events log.
    /// </summary>
    [EventSource(Name = "PeachPie")]
    public sealed class LogEventSource : EventSource
    {
        /// <summary>
        /// Public singleton to be used.
        /// Cannot be <c>null</c>.
        /// </summary>
        public static readonly LogEventSource/*!*/Log = new LogEventSource();

        /// <summary>
        /// Logs user's error message.
        /// </summary>
        [Event(1, Message = "error_log: {0}", Level = EventLevel.Error)]
        public void ErrorLog(string message)
        {
            if (IsEnabled()) WriteEvent(1, message);
        }

        /// <summary>
        /// Logs non-fatal error.
        /// </summary>
        [Event(2, Message = "Warning: {0}", Level = EventLevel.Warning)]
        public void HandleWarning(string message)
        {
            if (IsEnabled()) WriteEvent(2, message);
        }

        /// <summary>
        /// Logs fatal error.
        /// </summary>
        [Event(3, Message = "Error: {0}", Level = EventLevel.Error)]
        public void HandleFatal(string message)
        {
            if (IsEnabled()) WriteEvent(3, message);
        }
    }

    #endregion
}
