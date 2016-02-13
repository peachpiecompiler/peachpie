using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Pchp.Syntax
{
    #region Enums: WarningGroups, ErrorSeverity

    /// <summary>
    /// Compiler warning groups.
    /// </summary>
    [Flags]
    public enum WarningGroups
    {
        None = 0,

        // DeferredToRuntime group:
        InclusionsMapping = 1,
        DeferredToRuntimeOthers = 2,
        DeferredToRuntime = InclusionsMapping | DeferredToRuntimeOthers,

        // CompilerStrict group:
        AmpModifiers = 4,
        CompilerStrictOthers = 8,
        CompilerStrict = AmpModifiers | CompilerStrictOthers
    }

    public struct ErrorSeverity
    {
        public enum Values
        {
            Warning, Error, FatalError, WarningAsError
        }

        public static readonly ErrorSeverity Warning = new ErrorSeverity(Values.Warning);
        public static readonly ErrorSeverity Error = new ErrorSeverity(Values.Error);
        public static readonly ErrorSeverity FatalError = new ErrorSeverity(Values.FatalError);
        public static readonly ErrorSeverity WarningAsError = new ErrorSeverity(Values.WarningAsError);

        public Values Value { get { return value; } }
        private readonly Values value;

        public bool IsFatal
        {
            get { return value == Values.FatalError; }
        }

        private ErrorSeverity(Values value)
        {
            this.value = value;
        }

        public string ToCmdString()
        {
            return (value == Values.Warning) ? "warning" : "error";
        }

        public static implicit operator int(ErrorSeverity severity)
        {
            return (int)severity.value;
        }
    }

    #endregion

    #region ShortPosition

    /// <summary>
    /// Position of declarations stored in tables. Used for composing error messages.
    /// </summary>
    /// <remarks>
    /// All declarations from included script have the same number.
    /// </remarks>
    public struct ShortPosition
    {
        public int Line;
        public int Column;

        /// <summary>
        /// Constructs new position.
        /// </summary>
        /// <param name="line">Line number.</param>
        /// <param name="column">Column number.</param>
        public ShortPosition(int line, int column)
        {
            this.Line = line;
            this.Column = column;
        }

        /// <summary>
        /// Constructs new position.
        /// </summary>
        /// <param name="position">Position within document.</param>
        public ShortPosition(Text.TextPoint position)
            :this(position.Line, position.Column)
        {
        }

        /// <summary>
        /// Returns string representation of the position - "(_line_,_column_)" or empty string for invalid position.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!IsValid)
                // empty position
                return String.Empty;

            return string.Concat('(', Line.ToString(), ',', Column.ToString(), ')');
        }

        /// <summary>
        /// Sets the position that indicates invalid positon.
        /// </summary>
        public static ShortPosition Invalid = new ShortPosition(-1, -1);

        /// <summary>
        /// Tests whether the position is valid.
        /// </summary>
        /// <returns>True if the position is valid.</returns>
        public bool IsValid
        {
            get { return Line != -1; }
        }
    }

    #endregion

    #region ErrorPosition

    public struct ErrorPosition
    {
        /// <summary>
        /// First line of the error, indexed from 1.
        /// </summary>
        public int FirstLine;
        /// <summary>
        /// First column of the error, indexed from 1.
        /// </summary>
        public int FirstColumn;
        /// <summary>
        /// Last line of the error, indexed from 1.
        /// </summary>
        public int LastLine;
        /// <summary>
        /// Last column of the error, indexed from 1.
        /// </summary>
        public int LastColumn;

        /// <summary>
        /// Initializes new instance of <see cref="ErrorPosition"/>.
        /// </summary>
        /// <param name="firstLine">First line of the error, indexed from 1.</param>
        /// <param name="firstColumn">First column of the error, indexed from 1.</param>
        /// <param name="lastLine">Last line of the error, indexed from 1.</param>
        /// <param name="lastColumn">Last column of the error, indexed from 1.</param>
        public ErrorPosition(int firstLine, int firstColumn, int lastLine, int lastColumn)
        {
            this.FirstLine = firstLine;
            this.FirstColumn = firstColumn;
            this.LastLine = lastLine;
            this.LastColumn = lastColumn;
        }

        /// <summary>
        /// An invalid <see cref="ErrorPosition"/> singleton.
        /// </summary>
        public static ErrorPosition Invalid = new ErrorPosition(-1, -1, -1, -1);

        /// <summary>
        /// Whether 
        /// </summary>
        public bool IsValid
        {
            get { return FirstLine != -1; }
        }
    }

    #endregion

    #region Exceptions

    /// <summary>
    /// Thrown on fatal error.
    /// </summary>
    internal class CompilerException : Exception
    {
        public ErrorInfo ErrorInfo { get { return errorInfo; } }
        private readonly ErrorInfo errorInfo;

        public string[]/*!*/ ErrorParams { get { return errorParams; } }
        private readonly string[]/*!*/ errorParams;

        internal CompilerException()
        {
            errorInfo = FatalErrors.InternalError;
            errorParams = ArrayUtils.EmptyStrings;
        }

        internal CompilerException(ErrorInfo errorInfo, params string[]/*!*/ errorParams)
        {
            Debug.Assert(errorParams != null);

            this.errorInfo = errorInfo;
            this.errorParams = errorParams;
        }
        /// <summary>CTor from <see cref="ErrorInfo"/>, inner exception and parameters</summary>
        /// <param name="errorInfo">Information about error</param>
        /// <param name="innerException">Exception that caused this exception to be thrown</param>
        /// <param name="errorParams">Error parameters</param>
        internal CompilerException(ErrorInfo errorInfo, Exception innerException, params string[]/*!*/ errorParams)
            : base(errorInfo.ToString(), innerException)
        {
            Debug.Assert(errorParams != null);

            this.errorInfo = errorInfo;
            this.errorParams = errorParams;
        }

        internal CompilerException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Thrown if an assembly being reflected has invalid format.
    /// </summary>
    public sealed class ReflectionException : Exception
    {
        internal ReflectionException(string/*!*/ message)
            : base(message)
        {

        }

        public ReflectionException(string/*!*/ message, Exception/*!*/ inner)
            : base(message, inner)
        {

        }
    }

    /// <summary>
    /// Thrown if a source file/directory cannot be read.
    /// </summary>
    public sealed class InvalidSourceException : Exception
    {
        public FullPath Path { get { return path; } }
        private FullPath path;

        internal InvalidSourceException(FullPath path, Exception/*!*/ inner)
            : base(null, inner)
        {
            this.path = path;
        }

        public InvalidSourceException(FullPath path, string message)
            : base(message)
        {
            this.path = path;
        }

        public void Report(ErrorSink/*!*/ errorSink)
        {
            if (errorSink == null)
                throw new ArgumentNullException("errorSink");

            errorSink.Add(FatalErrors.InvalidSource, path, ErrorPosition.Invalid,
                InnerException != null ? InnerException.Message : this.Message);
        }
    }

    #endregion

    #region ErrorInfo

    /// <summary>
    /// Represents an error reported by the compiler.
    /// </summary>
    public struct ErrorInfo
    {
        /// <summary>
        /// Error unique id.
        /// </summary>
        public int Id { get { return id; } }
        private readonly int id;

        /// <summary>
        /// Error group.
        /// </summary>
        public int Group { get { return group; } set { group = value; } }
        private int group;

        /// <summary>
        /// Message resource id.
        /// </summary>
        public string MessageId { get { return messageId; } }
        private readonly string messageId;

        /// <summary>
        /// Error severity.
        /// </summary>
        public ErrorSeverity Severity { get { return severity; } }
        private readonly ErrorSeverity severity;

        public ErrorInfo(int id, string messageId, ErrorSeverity severity)
        {
            this.id = id;
            this.messageId = messageId;
            this.severity = severity;
            this.group = (int)WarningGroups.None;
        }

        public ErrorInfo(int id, string messageId, WarningGroups group)
        {
            this.id = id;
            this.messageId = messageId;
            this.severity = ErrorSeverity.Warning;
            this.group = (int)group;
        }
    }

    #endregion

    #region Error Sinks

    public abstract class ErrorSink
    {
        public WarningGroups DisabledGroups { get { return disabledGroups; } set { disabledGroups = value; } }
        private WarningGroups disabledGroups;

        public int[]/*!*/ DisabledWarnings
        {
            get { return disabledWarnings; }
            set { if (value == null) throw new ArgumentNullException("value"); disabledWarnings = value; }
        }
        private int[]/*!*/ disabledWarnings;

        /// <summary>
        /// Whether to treat warnings as errors.
        /// </summary>
        public bool TreatWarningsAsErrors { get; set; }

        public ErrorSink()
            : this(WarningGroups.None, ArrayUtils.EmptyIntegers)
        {
        }

        public ErrorSink(WarningGroups disabledGroups, int[]/*!*/ disabledWarnings)
        {
            if (disabledWarnings == null)
                throw new ArgumentNullException("disabledWarnings");

            this.disabledGroups = disabledGroups;
            this.disabledWarnings = disabledWarnings;
        }

        #region Error Counter

        public int FatalErrorCount { get { return counts[ErrorSeverity.FatalError]; } }
        public int ErrorCount { get { return counts[ErrorSeverity.Error]; } }
        public int WarningCount { get { return counts[ErrorSeverity.Warning]; } }
        public int WarningAsErrorCount { get { return counts[ErrorSeverity.WarningAsError]; } }

        public bool AnyError
        {
            get
            {
                return counts[ErrorSeverity.Error] + counts[ErrorSeverity.FatalError] + counts[ErrorSeverity.WarningAsError] > 0;
            }
        }

        public bool AnyFatalError
        {
            get { return counts[ErrorSeverity.FatalError] > 0; }
        }

        private int[] counts = { 0, 0, 0, 0 };

        #endregion

        #region Add Overloads

        internal void Add(ErrorInfo info, SourceUnit sourceUnit, Text.Span pos)
        {
            Add(info, CoreResources.GetString(info.MessageId), sourceUnit, pos);
        }

        internal void Add(ErrorInfo info, SourceUnit sourceUnit, Text.Span pos, params string[] args)
        {
            Add(info, CoreResources.GetString(info.MessageId, args), sourceUnit, pos);
        }

        internal void Add(ErrorInfo info, string fullPath, ErrorPosition pos, params string[] args)
        {
            Add(info, CoreResources.GetString(info.MessageId, args), fullPath, pos);
        }
        
        private void Add(ErrorInfo info, string/*!*/ message, SourceUnit sourceUnit, Text.Span pos)
        {
            Debug.Assert(message != null);

            string full_path;
            ErrorPosition mapped_pos;

            // missing source unit means the file name shouldn't be reported (it is not available)
            if (sourceUnit != null && pos.IsValid)
            {
                // get line,column from position
                sourceUnit.LineBreaks.GetLineColumnFromPosition(pos.Start, out mapped_pos.FirstLine, out mapped_pos.FirstColumn);
                sourceUnit.LineBreaks.GetLineColumnFromPosition(pos.End - 1, out mapped_pos.LastLine, out mapped_pos.LastColumn);

                //
                full_path = sourceUnit.FilePath;
            }
            else
            {
                full_path = null;
                mapped_pos = ErrorPosition.Invalid;
            }

            // filter disabled warnings:
            if (info.Id < 0 || (info.Group & (int)disabledGroups) == 0 && Array.IndexOf(disabledWarnings, info.Id) == -1)
            {
                // do not count disabled warnings and related locations et. al.:
                var severity = UpgradeSeverity(info.Severity);
                if (Add(info.Id, message, severity, info.Group, full_path, mapped_pos) && info.Id >= 0)
                    counts[severity]++;
            }
        }

        private void Add(ErrorInfo info, string/*!*/ message, string fullPath, ErrorPosition pos, params string[] args)
        {
            Debug.Assert(message != null);

            var severity = UpgradeSeverity(info.Severity);

            if (Add(info.Id, message, severity, info.Group, fullPath, pos) && info.Id >= 0)
                counts[severity]++;
        }

        #endregion

        #region Specialized Add Methods

        public void AddInternalError(Exception/*!*/ e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            const string BugTrackerUrl = "http://phalanger.codeplex.com/workitem/list/basic";

            StringBuilder message = new StringBuilder();
            for (Exception x = e; x != null; x = x.InnerException)
            {
                message.Append(x.Message);
                message.Append("\r\n");
                message.Append(x.StackTrace);
                message.Append("\r\n");
            }

            Add(FatalErrors.InternalError, null, ErrorPosition.Invalid, BugTrackerUrl, message.ToString());
        }

        //public void AddConfigurationError(ConfigurationErrorsException/*!*/ e)
        //{
        //    if (e == null)
        //        throw new ArgumentNullException("e");

        //    ErrorPosition pos = new ErrorPosition(e.Line, 0, e.Line, 0);
        //    StringBuilder message = new StringBuilder(e.BareMessage);
        //    Exception inner = e.InnerException;
        //    while (inner != null)
        //    {
        //        message.Append(" ");
        //        message.Append(inner.Message);
        //        inner = inner.InnerException;
        //    }
        //    Add(FatalErrors.ConfigurationError, e.Filename, pos, message.ToString());
        //}

        internal bool AddInternal(int id, string message, ErrorSeverity severity, int group, string fullPath, ErrorPosition pos)
        {
            return AddInternal(id, message, severity, group, fullPath, pos, false);
        }

        internal bool AddInternal(int id, string message, ErrorSeverity severity, int group, string fullPath, ErrorPosition pos, bool increaseCount)
        {
            severity = UpgradeSeverity(severity);

            bool result = Add(id, message, severity, group, fullPath, pos);

            if (increaseCount)
                counts[severity]++;

            return result;
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Upgrades <see cref="ErrorSeverity.Warning"/> to <see cref="ErrorSeverity.WarningAsError"/> is <see cref="TreatWarningsAsErrors"/> is enabled.
        /// </summary>
        private ErrorSeverity UpgradeSeverity(ErrorSeverity severity)
        {
            return (severity.Value == ErrorSeverity.Values.Warning && TreatWarningsAsErrors) ? ErrorSeverity.WarningAsError : severity;
        }

        #endregion

        /// <summary>
        /// Returns whether the warning has been reported.
        /// </summary>
        protected abstract bool Add(int id, string message, ErrorSeverity severity, int group, string fullPath, ErrorPosition pos);
    }

    public sealed class TextErrorSink : ErrorSink
    {
        public TextWriter/*!*/ Output { get { return output; } }
        private readonly TextWriter/*!*/ output;

        public TextErrorSink(TextWriter/*!*/ output)
            : this(output, WarningGroups.None, ArrayUtils.EmptyIntegers)
        {
        }

        public TextErrorSink(TextWriter/*!*/ output, WarningGroups disabledGroups, int[]/*!*/ disabledWarnings)
            : base(disabledGroups, disabledWarnings)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            this.output = output;
        }

        protected override bool Add(int id, string message, ErrorSeverity severity, int group, string fullPath, ErrorPosition pos)
        {
            if (fullPath != null)
            {
                Debug.Assert(pos.IsValid);
                output.Write(String.Format("{0}({1},{2}): ", fullPath, pos.FirstLine, pos.FirstColumn));
            }

            if (id >= 0)
                output.WriteLine(String.Format("{0} PHP{1:d4}: {2}", severity.ToCmdString(), id, message));
            else
                output.WriteLine(message);

            return true;
        }
    }

    internal sealed class PassthroughErrorSink : ErrorSink
    {
        private ErrorSink/*!*/ sink;

        public PassthroughErrorSink(ErrorSink/*!*/ sink)
            : base(sink.DisabledGroups, sink.DisabledWarnings)
        {
            this.sink = sink;
        }

        protected override bool Add(int id, string message, ErrorSeverity severity, int group, string fullPath, ErrorPosition pos)
        {
            return sink.AddInternal(id, message, severity, group, fullPath, pos);
        }
    }

    #endregion

    #region Warnings, Errors, Fatal Errors

    internal static class Warnings
    {
        public static readonly ErrorInfo RelatedLocation = new ErrorInfo(-1, "__related_location", ErrorSeverity.Warning);
        public static readonly ErrorInfo None = new ErrorInfo(-2, "", ErrorSeverity.Warning);

        // deferred-to-runtime group:
        public static readonly ErrorInfo InclusionReplacementFailes = new ErrorInfo(1, "inclusion_replacement_failed", WarningGroups.InclusionsMapping);
        public static readonly ErrorInfo InclusionTargetProcessingFailed = new ErrorInfo(3, "incuded_file_name_processing_failed", WarningGroups.DeferredToRuntimeOthers);
        public static readonly ErrorInfo InclusionDeferredToRuntime = new ErrorInfo(4, "inclusion_deferred_to_runtime", WarningGroups.DeferredToRuntimeOthers);
        // TODO: public static readonly ErrorInfo CyclicInclusionDetected = new ErrorInfo(5, "cyclic_inclusion", WarningGroups.DeferredToRuntimeOthers);

        // compiler-strict group:
        public static readonly ErrorInfo ActualParamWithAmpersand = new ErrorInfo(7, "act_param_with_ampersand", WarningGroups.AmpModifiers);
        public static readonly ErrorInfo UnreachableCodeDetected = new ErrorInfo(8, "unreachable_code", WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo TooBigIntegerConversion = new ErrorInfo(9, "too_big_int_conversion", WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo TooManyLocalVariablesInFunction = new ErrorInfo(10, "too_many_local_variables_function", WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo TooManyLocalVariablesInMethod = new ErrorInfo(11, "too_many_local_variables_method", WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo UnoptimizedLocalsInFunction = new ErrorInfo(12, "unoptimized_local_variables_function", WarningGroups.CompilerStrictOthers);

        public static readonly ErrorInfo UnusedLabel = new ErrorInfo(15, "unused_label", WarningGroups.CompilerStrictOthers);

        //

        // warnings in more groups => disabling any of these will disable the warning:
        public static readonly ErrorInfo UnknownClassUsed = new ErrorInfo(20, "unknown_class_used", WarningGroups.DeferredToRuntimeOthers | WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo UnknownClassUsedWithAlias = new ErrorInfo(21, "unknown_class_used_with_alias", WarningGroups.DeferredToRuntimeOthers | WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo UnknownFunctionUsed = new ErrorInfo(22, "unknown_function_used", WarningGroups.DeferredToRuntimeOthers | WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo UnknownFunctionUsedWithAlias = new ErrorInfo(23, "unknown_function_used_with_alias", WarningGroups.DeferredToRuntimeOthers | WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo UnknownConstantUsed = new ErrorInfo(24, "unknown_constant_used", WarningGroups.DeferredToRuntimeOthers | WarningGroups.CompilerStrictOthers);
        public static readonly ErrorInfo UnknownConstantUsedWithAlias = new ErrorInfo(25, "unknown_constant_used_with_alias", WarningGroups.DeferredToRuntimeOthers | WarningGroups.CompilerStrictOthers);

        //

        // others:
        public static readonly ErrorInfo InvalidArgumentCountForMethod = new ErrorInfo(115, "invalid_argument_count_for_method", ErrorSeverity.Warning);
        public static readonly ErrorInfo TooFewFunctionParameters = new ErrorInfo(116, "too_few_function_params", ErrorSeverity.Warning);
        public static readonly ErrorInfo TooFewMethodParameters = new ErrorInfo(117, "too_few_method_params", ErrorSeverity.Warning);
        public static readonly ErrorInfo TooFewCtorParameters = new ErrorInfo(118, "too_few_ctor_params", ErrorSeverity.Warning);
        public static readonly ErrorInfo NoCtorDefined = new ErrorInfo(120, "no_ctor_defined", ErrorSeverity.Warning);
        public static readonly ErrorInfo MultipleSwitchCasesWithSameValue = new ErrorInfo(121, "more_switch_cases_with_same_value", ErrorSeverity.Warning);
        public static readonly ErrorInfo MoreThenOneDefaultInSwitch = new ErrorInfo(122, "more_then_one_default_in_switch", ErrorSeverity.Warning);
        public static readonly ErrorInfo ThisOutOfMethod = new ErrorInfo(123, "this_out_of_method", ErrorSeverity.Warning);
        public static readonly ErrorInfo ThisInWriteContext = new ErrorInfo(124, "this_in_write_context", ErrorSeverity.Warning);
        public static readonly ErrorInfo MandatoryBehindOptionalParam = new ErrorInfo(125, "mandatory_behind_optional_param", ErrorSeverity.Warning);
        public static readonly ErrorInfo InclusionReplacementFailed = new ErrorInfo(126, "inclusion_replacement_failed", WarningGroups.InclusionsMapping);
        public static readonly ErrorInfo ConditionallyRedeclared = new ErrorInfo(127, "conditionally_redeclared", ErrorSeverity.Warning);
        public static readonly ErrorInfo ConditionallyRedeclaredByInclusion = new ErrorInfo(128, "conditionally_redeclared_by_inclusion", ErrorSeverity.Warning);

        public static readonly ErrorInfo PhpTrackVarsNotSupported = new ErrorInfo(129, "php_track_vars_not_supported", ErrorSeverity.Warning);
        public static readonly ErrorInfo UnterminatedComment = new ErrorInfo(130, "unterminated_comment", ErrorSeverity.Warning);
        public static readonly ErrorInfo InvalidEscapeSequenceLength = new ErrorInfo(132, "invalid_escape_sequence_length", ErrorSeverity.Warning);
        public static readonly ErrorInfo InvalidLinePragma = new ErrorInfo(133, "invalid_line_pragma", ErrorSeverity.Warning);


        //

        public static readonly ErrorInfo MultipleStatementsInAssertion = new ErrorInfo(140, "multiple_statements_in_assertion", ErrorSeverity.Warning);

        //

        public static readonly ErrorInfo DivisionByZero = new ErrorInfo(150, "division_by_zero", ErrorSeverity.Warning);
        public static readonly ErrorInfo NotSupportedFunctionCalled = new ErrorInfo(151, "notsupported_function_called", ErrorSeverity.Warning);

        //

        public static readonly ErrorInfo ClassBehaviorMayBeUnexpected = new ErrorInfo(160, "class_behavior_may_be_unexpected", ErrorSeverity.Warning);
        public static readonly ErrorInfo IncompleteClass = new ErrorInfo(161, "incomplete_class", WarningGroups.DeferredToRuntimeOthers);
        public static readonly ErrorInfo ImportDeprecated = new ErrorInfo(162, "import_deprecated", ErrorSeverity.Warning);

        public static readonly ErrorInfo BodyOfDllImportedFunctionIgnored = new ErrorInfo(170, "dll_import_body_ignored", ErrorSeverity.Warning);

        //
        public static readonly ErrorInfo MagicMethodMustBePublicNonStatic = new ErrorInfo(171, "magic_method_must_be_public_nonstatic", ErrorSeverity.Warning);
        public static readonly ErrorInfo CallStatMustBePublicStatic = new ErrorInfo(172, "callstat_must_be_public_static", ErrorSeverity.Warning);

        // strict standards

        public static readonly ErrorInfo DeclarationShouldBeCompatible = new ErrorInfo(180, "declaration_should_be_compatible", ErrorSeverity.Warning);
        public static readonly ErrorInfo AssignNewByRefDeprecated = new ErrorInfo(181, "assign_new_as_ref_is_deprecated", ErrorSeverity.Warning);

    }

    // 1000+
    internal static class Errors
    {
        public static readonly ErrorInfo RelatedLocation = new ErrorInfo(-1, "__related_location", ErrorSeverity.Error);

        public static readonly ErrorInfo ArrayInClassConstant = new ErrorInfo(1000, "array_in_cls_const", ErrorSeverity.Error);
        public static readonly ErrorInfo NonVariablePassedByRef = new ErrorInfo(1001, "nonvar_passed_by_ref", ErrorSeverity.Error);
        public static readonly ErrorInfo FieldInInterface = new ErrorInfo(1002, "field_in_interface", ErrorSeverity.Error);
        public static readonly ErrorInfo InvalidBreakLevelCount = new ErrorInfo(1003, "invalid_break_level_count", ErrorSeverity.Error);

        public static readonly ErrorInfo PropertyDeclaredAbstract = new ErrorInfo(1004, "property_declared_abstract", ErrorSeverity.Error);
        public static readonly ErrorInfo PropertyDeclaredFinal = new ErrorInfo(1005, "property_declared_final", ErrorSeverity.Error);
        public static readonly ErrorInfo PropertyRedeclared = new ErrorInfo(1006, "property_redeclared", ErrorSeverity.Error);

        public static readonly ErrorInfo MethodRedeclared = new ErrorInfo(1007, "method_redeclared", ErrorSeverity.Error);
        public static readonly ErrorInfo InterfaceMethodWithBody = new ErrorInfo(1008, "interface_bodyful_method", ErrorSeverity.Error);
        public static readonly ErrorInfo AbstractMethodWithBody = new ErrorInfo(1009, "abstract_bodyful_method", ErrorSeverity.Error);
        public static readonly ErrorInfo NonAbstractMethodWithoutBody = new ErrorInfo(1010, "nonabstract_bodyless_method", ErrorSeverity.Error);
        public static readonly ErrorInfo CloneCannotTakeArguments = new ErrorInfo(1011, "clone_cannot_take_arguments", ErrorSeverity.Error);
        public static readonly ErrorInfo CloneCannotBeStatic = new ErrorInfo(1012, "clone_cannot_be_static", ErrorSeverity.Error);
        public static readonly ErrorInfo DestructCannotTakeArguments = new ErrorInfo(1013, "destruct_cannot_take_arguments", ErrorSeverity.Error);
        public static readonly ErrorInfo DestructCannotBeStatic = new ErrorInfo(1014, "destruct_cannot_be_static", ErrorSeverity.Error);
        public static readonly ErrorInfo AbstractPrivateMethodDeclared = new ErrorInfo(1015, "abstract_private_method_declared", ErrorSeverity.Error);
        public static readonly ErrorInfo InterfaceMethodNotPublic = new ErrorInfo(1016, "interface_method_non_public", ErrorSeverity.Error);
        public static readonly ErrorInfo ConstantRedeclared = new ErrorInfo(1017, "constant_redeclared", ErrorSeverity.Error);
        public static readonly ErrorInfo AbstractMethodNotImplemented = new ErrorInfo(1018, "abstract_method_not_implemented", ErrorSeverity.Error);
        public static readonly ErrorInfo MethodNotCompatible = new ErrorInfo(1019, "method_not_compatible", ErrorSeverity.Error);   //  TODO: it's fatal error

        public static readonly ErrorInfo NonInterfaceImplemented = new ErrorInfo(1020, "non_interface_implemented", ErrorSeverity.Error);
        public static readonly ErrorInfo NonInterfaceExtended = new ErrorInfo(1021, "non_interface_extended", ErrorSeverity.Error);
        public static readonly ErrorInfo NonClassExtended = new ErrorInfo(1022, "non_class_extended", ErrorSeverity.Error);

        public static readonly ErrorInfo FinalClassExtended = new ErrorInfo(1023, "final_class_extended", ErrorSeverity.Error);

        // 1023

        public static readonly ErrorInfo ConstructCannotBeStatic = new ErrorInfo(1024, "construct_cannot_be_static", ErrorSeverity.Error);
        public static readonly ErrorInfo OverrideFinalMethod = new ErrorInfo(1025, "override_final_method", ErrorSeverity.Error);
        public static readonly ErrorInfo MakeStaticMethodNonStatic = new ErrorInfo(1026, "make_static_method_non_static", ErrorSeverity.Error);
        public static readonly ErrorInfo MakeNonStaticMethodStatic = new ErrorInfo(1027, "make_nonstatic_method_static", ErrorSeverity.Error);
        public static readonly ErrorInfo OverridingNonAbstractMethodByAbstract = new ErrorInfo(1028, "nonabstract_method_overridden_with_abstract", ErrorSeverity.Error);
        public static readonly ErrorInfo OverridingMethodRestrictsVisibility = new ErrorInfo(1029, "overriding_method_restrict_visibility", ErrorSeverity.Error);
        public static readonly ErrorInfo MakeStaticPropertyNonStatic = new ErrorInfo(1030, "make_static_property_nonstatic", ErrorSeverity.Error);
        public static readonly ErrorInfo MakeNonStaticPropertyStatic = new ErrorInfo(1031, "make_nonstatic_property_static", ErrorSeverity.Error);
        public static readonly ErrorInfo OverridingFieldRestrictsVisibility = new ErrorInfo(1032, "overriding_property_restrict_visibility", ErrorSeverity.Error);
        public static readonly ErrorInfo OverridingStaticFieldByStatic = new ErrorInfo(1033, "overriding_static_field_with_static", ErrorSeverity.Error);
        public static readonly ErrorInfo OverridingProtectedStaticWithInitValue = new ErrorInfo(1034, "overriding_protected_static_with_init_value", ErrorSeverity.Error);
        public static readonly ErrorInfo InheritingOnceInheritedConstant = new ErrorInfo(1035, "inheriting_previously_inherited_constant", ErrorSeverity.Error);
        public static readonly ErrorInfo RedeclaringInheritedConstant = new ErrorInfo(1036, "redeclaring_inherited_constant", ErrorSeverity.Error);
        public static readonly ErrorInfo AbstractFinalMethodDeclared = new ErrorInfo(1037, "abstract_final_method_declared", ErrorSeverity.Error);
        public static readonly ErrorInfo LibraryFunctionRedeclared = new ErrorInfo(1038, "library_func_redeclared", ErrorSeverity.Error);
        public static readonly ErrorInfo DuplicateParameterName = new ErrorInfo(1039, "duplicate_parameter_name", ErrorSeverity.Error);
        public static readonly ErrorInfo EmptyIndexInReadContext = new ErrorInfo(1040, "empty_index_in_read_context", ErrorSeverity.Error);


        public static readonly ErrorInfo ConstructNotSupported = new ErrorInfo(1041, "construct_not_supported", ErrorSeverity.Error);
        public static readonly ErrorInfo KeyAlias = new ErrorInfo(1042, "key_alias", ErrorSeverity.Error);
        public static readonly ErrorInfo MultipleVisibilityModifiers = new ErrorInfo(1043, "multiple_visibility_modifiers", ErrorSeverity.Error);
        public static readonly ErrorInfo InvalidInterfaceModifier = new ErrorInfo(1044, "invalid_interface_modifier", ErrorSeverity.Error);

        public static readonly ErrorInfo MethodCannotTakeArguments = new ErrorInfo(1045, "method_cannot_take_arguments", ErrorSeverity.Error);

        //

        public static readonly ErrorInfo PrivateClassInGlobalNamespace = new ErrorInfo(1048, "private_class_in_global_ns", ErrorSeverity.Error);


        public static readonly ErrorInfo InvalidCodePoint = new ErrorInfo(1049, "invalid_code_point", ErrorSeverity.Error);
        public static readonly ErrorInfo InvalidCodePointName = new ErrorInfo(1050, "invalid_code_point_name", ErrorSeverity.Error);
        public static readonly ErrorInfo InclusionInPureUnit = new ErrorInfo(1051, "inclusion_in_pure_unit", ErrorSeverity.Error);
        public static readonly ErrorInfo GlobalCodeInPureUnit = new ErrorInfo(1052, "global_code_in_pure_unit", ErrorSeverity.Error);

        public static readonly ErrorInfo ConflictingTypeAliases = new ErrorInfo(1053, "conflicting_type_aliases", ErrorSeverity.Error);
        public static readonly ErrorInfo ConflictingFunctionAliases = new ErrorInfo(1054, "conflicting_function_aliases", ErrorSeverity.Error);
        public static readonly ErrorInfo ConflictingConstantAliases = new ErrorInfo(1055, "conflicting_constant_aliases", ErrorSeverity.Error);

        //

        public static readonly ErrorInfo ProtectedPropertyAccessed = new ErrorInfo(1058, "protected_property_accessed", ErrorSeverity.Error);
        public static readonly ErrorInfo PrivatePropertyAccessed = new ErrorInfo(1059, "private_property_accessed", ErrorSeverity.Error);
        public static readonly ErrorInfo ProtectedMethodCalled = new ErrorInfo(1060, "protected_method_called", ErrorSeverity.Error);
        public static readonly ErrorInfo PrivateMethodCalled = new ErrorInfo(1061, "private_method_called", ErrorSeverity.Error);
        public static readonly ErrorInfo PrivateCtorCalled = new ErrorInfo(1062, "private_ctor_called", ErrorSeverity.Error);
        public static readonly ErrorInfo ProtectedCtorCalled = new ErrorInfo(1063, "protected_ctor_called", ErrorSeverity.Error);
        public static readonly ErrorInfo ProtectedConstantAccessed = new ErrorInfo(1064, "protected_constant_accessed", ErrorSeverity.Error);
        public static readonly ErrorInfo PrivateConstantAccessed = new ErrorInfo(1065, "private_constant_accessed", ErrorSeverity.Error);

        public static readonly ErrorInfo UnknownMethodCalled = new ErrorInfo(1066, "unknown_method_called", ErrorSeverity.Error);
        public static readonly ErrorInfo AbstractMethodCalled = new ErrorInfo(1067, "abstract_method_called", ErrorSeverity.Error);
        public static readonly ErrorInfo UnknownPropertyAccessed = new ErrorInfo(1068, "undeclared_static_property_accessed", ErrorSeverity.Error);
        public static readonly ErrorInfo UnknownClassConstantAccessed = new ErrorInfo(1069, "undefined_class_constant", ErrorSeverity.Error);
        public static readonly ErrorInfo CircularConstantDefinitionGlobal = new ErrorInfo(1070, "circular_constant_definition_global", ErrorSeverity.Error);
        public static readonly ErrorInfo CircularConstantDefinitionClass = new ErrorInfo(1071, "circular_constant_definition_class", ErrorSeverity.Error);

        //

        public static readonly ErrorInfo MissingEntryPoint = new ErrorInfo(1075, "missing_entry_point", ErrorSeverity.Error);
        public static readonly ErrorInfo EntryPointRedefined = new ErrorInfo(1076, "entry_point_redefined", ErrorSeverity.Error);


        //

        public static readonly ErrorInfo AmbiguousTypeMatch = new ErrorInfo(1088, "ambiguous_type_match", ErrorSeverity.Error);
        public static readonly ErrorInfo AmbiguousFunctionMatch = new ErrorInfo(1089, "ambiguous_function_match", ErrorSeverity.Error);
        public static readonly ErrorInfo AmbiguousConstantMatch = new ErrorInfo(1090, "ambiguous_constant_match", ErrorSeverity.Error);
        public static readonly ErrorInfo CannotUseReservedName = new ErrorInfo(1091, "cannot_use_reserved_name", ErrorSeverity.Error);
        public static readonly ErrorInfo IncompleteClass = new ErrorInfo(1092, "incomplete_class", ErrorSeverity.Error);
        public static readonly ErrorInfo ClassHasNoVisibleCtor = new ErrorInfo(1093, "class_has_no_visible_ctor", ErrorSeverity.Error);
        public static readonly ErrorInfo AbstractClassOrInterfaceInstantiated = new ErrorInfo(1094, "abstract_class_or_interface_instantiated", ErrorSeverity.Error);
        public static readonly ErrorInfo ClosureInstantiated = new ErrorInfo(1095, "instantiation_not_allowed", ErrorSeverity.Error);

        //

        public static readonly ErrorInfo ParentUsedOutOfClass = new ErrorInfo(1110, "parent_used_out_of_class", ErrorSeverity.Error);
        public static readonly ErrorInfo SelfUsedOutOfClass = new ErrorInfo(1111, "self_used_out_of_class", ErrorSeverity.Error);
        public static readonly ErrorInfo ClassHasNoParent = new ErrorInfo(1112, "class_has_no_parent", ErrorSeverity.Error);
        public static readonly ErrorInfo StaticUsedOutOfClass = new ErrorInfo(1113, "static_used_out_of_class", ErrorSeverity.Error);

        //

        public static readonly ErrorInfo UnknownCustomAttribute = new ErrorInfo(1120, "unknown_custom_attribute", ErrorSeverity.Error);
        public static readonly ErrorInfo NotCustomAttributeClass = new ErrorInfo(1121, "not_custom_attribute_class", ErrorSeverity.Error);
        public static readonly ErrorInfo InvalidAttributeExpression = new ErrorInfo(1122, "invalid_attribute_expression", ErrorSeverity.Error);
        public static readonly ErrorInfo InvalidAttributeUsage = new ErrorInfo(1123, "invalid_attribute_usage", ErrorSeverity.Error);
        public static readonly ErrorInfo InvalidAttributeTargetSelector = new ErrorInfo(1124, "invalid_attribute_target_selector", ErrorSeverity.Error);
        public static readonly ErrorInfo DuplicateAttributeUsage = new ErrorInfo(1125, "duplicate_attribute_usage", ErrorSeverity.Error);
        public static readonly ErrorInfo OutAttributeOnByValueParam = new ErrorInfo(1126, "out_attribute_on_byval_param", ErrorSeverity.Error);
        public static readonly ErrorInfo ExportAttributeInNonPureUnit = new ErrorInfo(1127, "export_attribute_in_non_pure", ErrorSeverity.Error);


        //

        public static readonly ErrorInfo MissingPartialModifier = new ErrorInfo(1148, "missing_partial_modifier", ErrorSeverity.Error);
        public static readonly ErrorInfo PartialConditionalDeclaration = new ErrorInfo(1149, "partial_conditional_declaration", ErrorSeverity.Error);
        public static readonly ErrorInfo PartialTransientDeclaration = new ErrorInfo(1150, "partial_transient_declaration", ErrorSeverity.Error);
        public static readonly ErrorInfo PartialImpureDeclaration = new ErrorInfo(1151, "partial_impure_declaration", ErrorSeverity.Error);
        public static readonly ErrorInfo IncompatiblePartialDeclarations = new ErrorInfo(1152, "incompatible_partial_declarations", ErrorSeverity.Error);
        public static readonly ErrorInfo ConflictingPartialVisibility = new ErrorInfo(1153, "conflicting_partial_visibility", ErrorSeverity.Error);
        public static readonly ErrorInfo PartialDeclarationsDifferInBase = new ErrorInfo(1154, "partial_declarations_differ_in_base", ErrorSeverity.Error);
        public static readonly ErrorInfo PartialDeclarationsDifferInTypeParameterCount = new ErrorInfo(1155, "partial_declarations_differ_in_type_parameter_count", ErrorSeverity.Error);
        public static readonly ErrorInfo PartialDeclarationsDifferInTypeParameter = new ErrorInfo(1156, "partial_declarations_differ_in_type_parameter", ErrorSeverity.Error);

        //

        public static readonly ErrorInfo GenericParameterMustBeType = new ErrorInfo(1170, "generic_parameter_must_be_type", ErrorSeverity.Error);
        public static readonly ErrorInfo DuplicateGenericParameter = new ErrorInfo(1171, "duplicate_generic_parameter", ErrorSeverity.Error);
        public static readonly ErrorInfo GenericParameterCollidesWithDeclarer = new ErrorInfo(1172, "generic_parameter_collides_with_declarer", ErrorSeverity.Error);
        public static readonly ErrorInfo CannotDeriveFromTypeParameter = new ErrorInfo(1173, "cannot_derive_from_type_parameter", ErrorSeverity.Error);
        public static readonly ErrorInfo GenericCallToLibraryFunction = new ErrorInfo(1074, "generic_call_to_library_function", ErrorSeverity.Error);
        public static readonly ErrorInfo ConstructorWithGenericParameters = new ErrorInfo(1075, "generic_parameters_disallowed_on_ctor", ErrorSeverity.Error);
        public static readonly ErrorInfo GenericAlreadyInUse = new ErrorInfo(1076, "generic_in_use", ErrorSeverity.Error);

        public static readonly ErrorInfo TooManyTypeArgumentsInTypeUse = new ErrorInfo(1080, "too_many_type_arguments_in_type_use", ErrorSeverity.Error);
        public static readonly ErrorInfo NonGenericTypeUsedWithTypeArgs = new ErrorInfo(1081, "non_generic_type_used_with_type_arguments", ErrorSeverity.Error);
        public static readonly ErrorInfo MissingTypeArgumentInTypeUse = new ErrorInfo(1082, "missing_type_argument_in_type_use", ErrorSeverity.Error);
        public static readonly ErrorInfo IncompatibleTypeParameterConstraintsInTypeUse = new ErrorInfo(1083, "incompatible_type_parameter_constraints_type", ErrorSeverity.Error);
        public static readonly ErrorInfo IncompatibleTypeParameterConstraintsInMethodUse = new ErrorInfo(1084, "incompatible_type_parameter_constraints_method", ErrorSeverity.Error);

        //

        public static readonly ErrorInfo InvalidArgumentCount = new ErrorInfo(1210, "invalid_argument_count", ErrorSeverity.Error);
        public static readonly ErrorInfo AbstractPropertyNotImplemented = new ErrorInfo(1211, "abstract_property_not_implemented", ErrorSeverity.Error);
        public static readonly ErrorInfo InvalidArgumentCountForFunction = new ErrorInfo(1212, "invalid_argument_count_for_function", ErrorSeverity.Error);


        //

        public static readonly ErrorInfo InvalidIdentifier = new ErrorInfo(1230, "invalid_identifier", ErrorSeverity.Error);

        public static readonly ErrorInfo LabelRedeclared = new ErrorInfo(1231, "label_redeclared", ErrorSeverity.Error);
        public static readonly ErrorInfo UndefinedLabel = new ErrorInfo(1232, "undefined_label", ErrorSeverity.Error);

        //

        public static readonly ErrorInfo ExpectingParentCtorInvocation = new ErrorInfo(1240, "expecting_parent_ctor_invocation", ErrorSeverity.Error);
        public static readonly ErrorInfo UnexpectedParentCtorInvocation = new ErrorInfo(1241, "unexpected_parent_ctor_invocation", ErrorSeverity.Error);
        public static readonly ErrorInfo MissingCtorInClrSubclass = new ErrorInfo(1242, "missing_ctor_in_clr_subclass", ErrorSeverity.Error);

        public static readonly ErrorInfo MissingImportedEntity = new ErrorInfo(1250, "missing_imported_entity", ErrorSeverity.Error);
        public static readonly ErrorInfo NamespaceKeywordUsedOutsideOfNamespace = new ErrorInfo(1251, "namespace_keyword_outside_namespace", ErrorSeverity.Error);
        public static readonly ErrorInfo ImportOnlyInPureMode = new ErrorInfo(1252, "import_only_in_pure", ErrorSeverity.Error);

        public static readonly ErrorInfo DllImportMethodMustBeStatic = new ErrorInfo(1260, "dll_import_must_be_static", ErrorSeverity.Error);
        public static readonly ErrorInfo DllImportMethodCannotBeAbstract = new ErrorInfo(1261, "dll_import_cannot_be_abstract", ErrorSeverity.Error);
    }

    // 2000+
    internal static class FatalErrors
    {
        public static readonly ErrorInfo RelatedLocation = new ErrorInfo(-1, "__related_location", ErrorSeverity.Error);

        public static readonly ErrorInfo TypeRedeclared = new ErrorInfo(2000, "type_redeclared", ErrorSeverity.FatalError);
        public static readonly ErrorInfo FunctionRedeclared = new ErrorInfo(2001, "function_redeclared", ErrorSeverity.FatalError);
        public static readonly ErrorInfo ConstantRedeclared = new ErrorInfo(2002, "constant_redeclared", ErrorSeverity.FatalError);

        public static readonly ErrorInfo InvalidCommandLineArgument = new ErrorInfo(2003, "invalid_command_line_argument", ErrorSeverity.FatalError);
        public static readonly ErrorInfo InvalidCommandLineArgumentNoName = new ErrorInfo(2003, "invalid_command_line_argument_noname", ErrorSeverity.FatalError);
        public static readonly ErrorInfo ConfigurationError = new ErrorInfo(2004, "configuration_error", ErrorSeverity.FatalError);
        public static readonly ErrorInfo InvalidSource = new ErrorInfo(2005, "invalid_source", ErrorSeverity.FatalError);
        public static readonly ErrorInfo ErrorCreatingFile = new ErrorInfo(2006, "error_creating_file", ErrorSeverity.FatalError);
        public static readonly ErrorInfo InternalError = new ErrorInfo(2007, "internal_error", ErrorSeverity.FatalError);


        //public static readonly ErrorInfo RedeclaredByInclusion = new ErrorInfo(2005, "redeclared_by_inclusion", ErrorSeverity.FatalError);



        //public static readonly ErrorInfo ClassRedeclaredAtRuntime = new ErrorInfo(2007, "class_redeclared_runtime", ErrorSeverity.FatalError);
        //public static readonly ErrorInfo ClassRedeclaredAtRuntimeByInclusion = new ErrorInfo(2008, "class_redeclared_runtime_include", ErrorSeverity.FatalError);



        //public static readonly ErrorInfo LibraryClassRedeclared = new ErrorInfo(2009, "library_class_redeclared", ErrorSeverity.FatalError);
        //public static readonly ErrorInfo LibraryClassRedeclaredByInclusion = new ErrorInfo(2010, "library_class_redeclared_by_inclusion", ErrorSeverity.FatalError);
        //public static readonly ErrorInfo ClassRedeclaredInAssembly = new ErrorInfo(2011, "class_redeclared_in_assembly", ErrorSeverity.FatalError);
        //public static readonly ErrorInfo AbstractMethodNameNotMatchingImplementation = new ErrorInfo(2012, "abstract_method_name_not_matching_implementation", ErrorSeverity.FatalError);
        public static readonly ErrorInfo SyntaxError = new ErrorInfo(2014, "syntax_error", ErrorSeverity.FatalError);
        public static readonly ErrorInfo CheckVarUseFault = new ErrorInfo(2015, "check_varuse_fault", ErrorSeverity.FatalError);

        public static readonly ErrorInfo CircularBaseClassDependency = new ErrorInfo(2030, "circular_base_class_dependency", ErrorSeverity.FatalError);
        public static readonly ErrorInfo CircularBaseInterfaceDependency = new ErrorInfo(2031, "circular_base_interface_dependency", ErrorSeverity.FatalError);
        public static readonly ErrorInfo MethodMustTakeExacArgsCount = new ErrorInfo(2032, "method_must_take_exact_args_count", ErrorSeverity.FatalError);

        public static readonly ErrorInfo AliasAlreadyInUse = new ErrorInfo(2040, "alias_in_use", ErrorSeverity.FatalError);
        public static readonly ErrorInfo ClassAlreadyInUse = new ErrorInfo(2041, "class_in_use", ErrorSeverity.FatalError);

        public static readonly ErrorInfo TryWithoutCatchOrFinally = new ErrorInfo(2050, "try_without_catch_or_finally", ErrorSeverity.FatalError);
    }

    #endregion
}
