using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis
{
    /// <summary>
    /// Options for getting type information from correspodning PHPDoc comments.
    /// </summary>
    [Flags]
    public enum PhpDocTypes
    {
        None = 0,

        /// <summary>
        /// Fields type will be declared according to PHPDoc @var tag.
        /// </summary>
        FieldTypes = 1,

        /// <summary>
        /// Parameter type will be declared according to PHPDoc @param tag.
        /// </summary>
        ParameterTypes = 2,

        /// <summary>
        /// Method return type will be declared according to PHPDoc @return tag.
        /// </summary>
        ReturnTypes = 4,

        /// <summary>
        /// Declare all additional type information from PHPDoc.
        /// </summary>
        All = FieldTypes | ParameterTypes | ReturnTypes,
    }

    /// <summary>
    /// Represents various options that affect compilation, such as 
    /// whether to emit an executable or a library, whether to optimize
    /// generated code, and so on.
    /// </summary>
    public sealed class PhpCompilationOptions : CompilationOptions, IEquatable<PhpCompilationOptions>
    {
        /// <summary>
        /// Compilation root directory.
        /// All script paths will be emitted relatively to this path.
        /// </summary>
        public string BaseDirectory { get; private set; }

        /// <summary>
        /// Compilation root directory.
        /// All script paths will be emitted relatively to this path.
        /// </summary>
        public string SdkDirectory { get; private set; }

        /// <summary>
        /// Options for getting type information from correspodning PHPDoc comments.
        /// </summary>
        public PhpDocTypes PhpDocTypes { get; private set; }

        ///// <summary>
        ///// Flags applied to the top-level binder created for each syntax tree in the compilation 
        ///// as well as for the binder of global imports.
        ///// </summary>
        //internal BinderFlags TopLevelBinderFlags { get; private set; }

        // Defaults correspond to the compiler's defaults or indicate that the user did not specify when that is significant.
        // That's significant when one option depends on another's setting. SubsystemVersion depends on Platform and Target.
        public PhpCompilationOptions(
            OutputKind outputKind,
            string baseDirectory,
            string sdkDirectory,
            bool reportSuppressedDiagnostics = false,
            string moduleName = null,
            string mainTypeName = null,
            string scriptClassName = null,
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug,
            bool checkOverflow = false,
            string cryptoKeyContainer = null,
            string cryptoKeyFile = null,
            ImmutableArray<byte> cryptoPublicKey = default(ImmutableArray<byte>),
            bool? delaySign = null,
            Platform platform = Platform.AnyCpu,
            ReportDiagnostic generalDiagnosticOption = ReportDiagnostic.Default,
            int warningLevel = 4,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions = null,
            bool concurrentBuild = true,
            bool deterministic = false,
            XmlReferenceResolver xmlReferenceResolver = null,
            SourceReferenceResolver sourceReferenceResolver = null,
            MetadataReferenceResolver metadataReferenceResolver = null,
            AssemblyIdentityComparer assemblyIdentityComparer = null,
            StrongNameProvider strongNameProvider = null,
            bool publicSign = false,
            PhpDocTypes phpdocTypes = PhpDocTypes.None)
            : this(outputKind, baseDirectory, sdkDirectory,
                   reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                   optimizationLevel, checkOverflow,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, platform,
                   generalDiagnosticOption, warningLevel,
                   specificDiagnosticOptions, concurrentBuild, deterministic,
                   extendedCustomDebugInformation: true,
                   debugPlusMode: false,
                   xmlReferenceResolver: xmlReferenceResolver,
                   sourceReferenceResolver: sourceReferenceResolver,
                   metadataReferenceResolver: metadataReferenceResolver,
                   assemblyIdentityComparer: assemblyIdentityComparer,
                   strongNameProvider: strongNameProvider,
                   metadataImportOptions: MetadataImportOptions.Public,
                   publicSign: publicSign,
                   phpdocTypes : phpdocTypes)
        {
        }

        // Expects correct arguments.
        internal PhpCompilationOptions(
            OutputKind outputKind,
            string baseDirectory,
            string sdkDirectory,
            bool reportSuppressedDiagnostics,
            string moduleName,
            string mainTypeName,
            string scriptClassName,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            string cryptoKeyContainer,
            string cryptoKeyFile,
            ImmutableArray<byte> cryptoPublicKey,
            bool? delaySign,
            Platform platform,
            ReportDiagnostic generalDiagnosticOption,
            int warningLevel,
            IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions,
            bool concurrentBuild,
            bool deterministic,
            bool extendedCustomDebugInformation,
            bool debugPlusMode,
            XmlReferenceResolver xmlReferenceResolver,
            SourceReferenceResolver sourceReferenceResolver,
            MetadataReferenceResolver metadataReferenceResolver,
            AssemblyIdentityComparer assemblyIdentityComparer,
            StrongNameProvider strongNameProvider,
            MetadataImportOptions metadataImportOptions,
            bool publicSign,
            PhpDocTypes phpdocTypes)
            : base(outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                   cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign, publicSign, optimizationLevel, checkOverflow,
                   platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions.ToImmutableDictionaryOrEmpty(),
                   concurrentBuild, deterministic, extendedCustomDebugInformation, debugPlusMode, xmlReferenceResolver,
                   sourceReferenceResolver, metadataReferenceResolver, assemblyIdentityComparer,
                   strongNameProvider, metadataImportOptions)
        {
            this.BaseDirectory = baseDirectory;
            this.SdkDirectory = sdkDirectory;
            this.PhpDocTypes = phpdocTypes;
        }

        private PhpCompilationOptions(PhpCompilationOptions other) : this(
            outputKind: other.OutputKind,
            baseDirectory: other.BaseDirectory,
            sdkDirectory: other.SdkDirectory,
            moduleName: other.ModuleName,
            mainTypeName: other.MainTypeName,
            scriptClassName: other.ScriptClassName,
            optimizationLevel: other.OptimizationLevel,
            checkOverflow: other.CheckOverflow,
            cryptoKeyContainer: other.CryptoKeyContainer,
            cryptoKeyFile: other.CryptoKeyFile,
            cryptoPublicKey: other.CryptoPublicKey,
            delaySign: other.DelaySign,
            platform: other.Platform,
            generalDiagnosticOption: other.GeneralDiagnosticOption,
            warningLevel: other.WarningLevel,
            specificDiagnosticOptions: other.SpecificDiagnosticOptions,
            concurrentBuild: other.ConcurrentBuild,
            deterministic: other.Deterministic,
            extendedCustomDebugInformation: other.ExtendedCustomDebugInformation,
            debugPlusMode: other.DebugPlusMode,
            xmlReferenceResolver: other.XmlReferenceResolver,
            sourceReferenceResolver: other.SourceReferenceResolver,
            metadataReferenceResolver: other.MetadataReferenceResolver,
            assemblyIdentityComparer: other.AssemblyIdentityComparer,
            strongNameProvider: other.StrongNameProvider,
            metadataImportOptions: other.MetadataImportOptions,
            reportSuppressedDiagnostics: other.ReportSuppressedDiagnostics,
            publicSign: other.PublicSign,
            phpdocTypes: other.PhpDocTypes)
        {
        }

        internal override ImmutableArray<string> GetImports() => ImmutableArray<string>.Empty; // Usings;

        public new PhpCompilationOptions WithOutputKind(OutputKind kind)
        {
            if (kind == this.OutputKind)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { OutputKind = kind };
        }

        public PhpCompilationOptions WithModuleName(string moduleName)
        {
            if (moduleName == this.ModuleName)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { ModuleName = moduleName };
        }

        public PhpCompilationOptions WithScriptClassName(string name)
        {
            if (name == this.ScriptClassName)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { ScriptClassName = name };
        }

        public PhpCompilationOptions WithMainTypeName(string name)
        {
            if (name == this.MainTypeName)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { MainTypeName = name };
        }

        public PhpCompilationOptions WithCryptoKeyContainer(string name)
        {
            if (name == this.CryptoKeyContainer)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { CryptoKeyContainer = name };
        }

        public PhpCompilationOptions WithCryptoKeyFile(string path)
        {
            if (path == this.CryptoKeyFile)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { CryptoKeyFile = path };
        }

        public PhpCompilationOptions WithCryptoPublicKey(ImmutableArray<byte> value)
        {
            if (value.IsDefault)
            {
                value = ImmutableArray<byte>.Empty;
            }

            if (value == this.CryptoPublicKey)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { CryptoPublicKey = value };
        }

        public PhpCompilationOptions WithDelaySign(bool? value)
        {
            if (value == this.DelaySign)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { DelaySign = value };
        }

        public new PhpCompilationOptions WithOptimizationLevel(OptimizationLevel value)
        {
            if (value == this.OptimizationLevel)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { OptimizationLevel = value };
        }

        public PhpCompilationOptions WithOverflowChecks(bool enabled)
        {
            if (enabled == this.CheckOverflow)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { CheckOverflow = enabled };
        }

        public new PhpCompilationOptions WithPlatform(Platform platform)
        {
            if (this.Platform == platform)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { Platform = platform };
        }

        public new PhpCompilationOptions WithPublicSign(bool publicSign)
        {
            if (this.PublicSign == publicSign)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { PublicSign = publicSign };
        }

        protected override CompilationOptions CommonWithGeneralDiagnosticOption(ReportDiagnostic value) => WithGeneralDiagnosticOption(value);

        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> specificDiagnosticOptions) =>
            WithSpecificDiagnosticOptions(specificDiagnosticOptions);

        protected override CompilationOptions CommonWithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions) =>
            WithSpecificDiagnosticOptions(specificDiagnosticOptions);

        protected override CompilationOptions CommonWithReportSuppressedDiagnostics(bool reportSuppressedDiagnostics) =>
            WithReportSuppressedDiagnostics(reportSuppressedDiagnostics);

        public new PhpCompilationOptions WithGeneralDiagnosticOption(ReportDiagnostic value)
        {
            if (this.GeneralDiagnosticOption == value)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { GeneralDiagnosticOption = value };
        }

        public new PhpCompilationOptions WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> values)
        {
            if (values == null)
            {
                values = ImmutableDictionary<string, ReportDiagnostic>.Empty;
            }

            if (this.SpecificDiagnosticOptions == values)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { SpecificDiagnosticOptions = values };
        }

        public new PhpCompilationOptions WithSpecificDiagnosticOptions(IEnumerable<KeyValuePair<string, ReportDiagnostic>> values) =>
            new PhpCompilationOptions(this) { SpecificDiagnosticOptions = values.ToImmutableDictionaryOrEmpty() };

        public new PhpCompilationOptions WithReportSuppressedDiagnostics(bool reportSuppressedDiagnostics)
        {
            if (reportSuppressedDiagnostics == this.ReportSuppressedDiagnostics)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { ReportSuppressedDiagnostics = reportSuppressedDiagnostics };
        }

        public PhpCompilationOptions WithWarningLevel(int warningLevel)
        {
            if (warningLevel == this.WarningLevel)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { WarningLevel = warningLevel };
        }

        public PhpCompilationOptions WithConcurrentBuild(bool concurrentBuild)
        {
            if (concurrentBuild == this.ConcurrentBuild)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { ConcurrentBuild = concurrentBuild };
        }

        public new PhpCompilationOptions WithDeterministic(bool deterministic)
        {
            if (deterministic == this.Deterministic)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { Deterministic = deterministic };
        }

        internal PhpCompilationOptions WithExtendedCustomDebugInformation(bool extendedCustomDebugInformation)
        {
            if (extendedCustomDebugInformation == this.ExtendedCustomDebugInformation)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { ExtendedCustomDebugInformation_internal_protected_set = extendedCustomDebugInformation };
        }

        public PhpCompilationOptions WithDebugPlusMode(bool debugPlusMode)
        {
            if (debugPlusMode == this.DebugPlusMode)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { DebugPlusMode_internal_protected_set = debugPlusMode };
        }

        internal PhpCompilationOptions WithMetadataImportOptions(MetadataImportOptions value)
        {
            if (value == this.MetadataImportOptions)
            {
                return this;
            }

            return new PhpCompilationOptions(this) { MetadataImportOptions_internal_protected_set = value };
        }

        public new PhpCompilationOptions WithXmlReferenceResolver(XmlReferenceResolver resolver)
        {
            if (ReferenceEquals(resolver, this.XmlReferenceResolver))
            {
                return this;
            }

            return new PhpCompilationOptions(this) { XmlReferenceResolver = resolver };
        }

        public new PhpCompilationOptions WithSourceReferenceResolver(SourceReferenceResolver resolver)
        {
            if (ReferenceEquals(resolver, this.SourceReferenceResolver))
            {
                return this;
            }

            return new PhpCompilationOptions(this) { SourceReferenceResolver = resolver };
        }

        public new PhpCompilationOptions WithMetadataReferenceResolver(MetadataReferenceResolver resolver)
        {
            if (ReferenceEquals(resolver, this.MetadataReferenceResolver))
            {
                return this;
            }

            return new PhpCompilationOptions(this) { MetadataReferenceResolver = resolver };
        }

        public new PhpCompilationOptions WithAssemblyIdentityComparer(AssemblyIdentityComparer comparer)
        {
            comparer = comparer ?? AssemblyIdentityComparer.Default;

            if (ReferenceEquals(comparer, this.AssemblyIdentityComparer))
            {
                return this;
            }

            return new PhpCompilationOptions(this) { AssemblyIdentityComparer = comparer };
        }

        public new PhpCompilationOptions WithStrongNameProvider(StrongNameProvider provider)
        {
            if (ReferenceEquals(provider, this.StrongNameProvider))
            {
                return this;
            }

            return new PhpCompilationOptions(this) { StrongNameProvider = provider };
        }

        protected override CompilationOptions CommonWithDeterministic(bool deterministic) => WithDeterministic(deterministic);

        protected override CompilationOptions CommonWithOutputKind(OutputKind kind) => WithOutputKind(kind);

        protected override CompilationOptions CommonWithPlatform(Platform platform) => WithPlatform(platform);

        protected override CompilationOptions CommonWithPublicSign(bool publicSign) => WithPublicSign(publicSign);

        protected override CompilationOptions CommonWithOptimizationLevel(OptimizationLevel value) => WithOptimizationLevel(value);

        protected override CompilationOptions CommonWithAssemblyIdentityComparer(AssemblyIdentityComparer comparer) =>
            WithAssemblyIdentityComparer(comparer);

        protected override CompilationOptions CommonWithXmlReferenceResolver(XmlReferenceResolver resolver) =>
            WithXmlReferenceResolver(resolver);

        protected override CompilationOptions CommonWithSourceReferenceResolver(SourceReferenceResolver resolver) =>
            WithSourceReferenceResolver(resolver);

        protected override CompilationOptions CommonWithMetadataReferenceResolver(MetadataReferenceResolver resolver) =>
            WithMetadataReferenceResolver(resolver);

        protected override CompilationOptions CommonWithStrongNameProvider(StrongNameProvider provider) =>
            WithStrongNameProvider(provider);

        [Obsolete]
        protected override CompilationOptions CommonWithFeatures(ImmutableArray<string> features)
        {
            throw new NotImplementedException();
        }

        internal override void ValidateOptions(ArrayBuilder<Diagnostic> builder)
        {
            ////  /main & /target:{library|netmodule|winmdobj}
            //if (this.MainTypeName != null)
            //{
            //    if (this.OutputKind.IsValid() && !this.OutputKind.IsApplication())
            //    {
            //        builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_NoMainOnDLL));
            //    }

            //    if (!MainTypeName.IsValidClrTypeName())
            //    {
            //        builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(MainTypeName), MainTypeName));
            //    }
            //}

            //if (!Platform.IsValid())
            //{
            //    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadPlatformType, Platform.ToString()));
            //}

            //if (ModuleName != null)
            //{
            //    Exception e = MetadataHelpers.CheckAssemblyOrModuleName(ModuleName, nameof(ModuleName));
            //    if (e != null)
            //    {
            //        builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOption, e.Message));
            //    }
            //}

            //if (!OutputKind.IsValid())
            //{
            //    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(OutputKind), OutputKind.ToString()));
            //}

            //if (!OptimizationLevel.IsValid())
            //{
            //    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(OptimizationLevel), OptimizationLevel.ToString()));
            //}

            //if (ScriptClassName == null || !ScriptClassName.IsValidClrTypeName())
            //{
            //    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(ScriptClassName), ScriptClassName ?? "null"));
            //}

            //if (WarningLevel < 0 || WarningLevel > 4)
            //{
            //    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(WarningLevel), WarningLevel));
            //}

            //if (Usings != null && Usings.Any(u => !u.IsValidClrNamespaceName()))
            //{
            //    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadCompilationOptionValue, nameof(Usings), Usings.Where(u => !u.IsValidClrNamespaceName()).First() ?? "null"));
            //}

            //if (Platform == Platform.AnyCpu32BitPreferred && OutputKind.IsValid() && !(OutputKind == OutputKind.ConsoleApplication || OutputKind == OutputKind.WindowsApplication || OutputKind == OutputKind.WindowsRuntimeApplication))
            //{
            //    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadPrefer32OnLib));
            //}

            //// TODO: add check for 
            ////          (kind == 'arm' || kind == 'appcontainer' || kind == 'winmdobj') &&
            ////          (version >= "6.2")

            //if (!CryptoPublicKey.IsEmpty)
            //{
            //    if (CryptoKeyFile != null)
            //    {
            //        builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_MutuallyExclusiveOptions, nameof(CryptoPublicKey), nameof(CryptoKeyFile)));
            //    }

            //    if (CryptoKeyContainer != null)
            //    {
            //        builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_MutuallyExclusiveOptions, nameof(CryptoPublicKey), nameof(CryptoKeyContainer)));
            //    }
            //}

            //if (PublicSign && DelaySign == true)
            //{
            //    builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_MutuallyExclusiveOptions, nameof(PublicSign), nameof(DelaySign)));
            //}
        }

        public bool Equals(PhpCompilationOptions other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (!base.EqualsHelper(other))
            {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as PhpCompilationOptions);
        }

        public override int GetHashCode()
        {
            return base.GetHashCodeHelper();
        }

        internal override Diagnostic FilterDiagnostic(Diagnostic diagnostic)
        {
            return diagnostic; // return PhpDiagnosticFilter.Filter(diagnostic, WarningLevel, GeneralDiagnosticOption, SpecificDiagnosticOptions);
        }
    }
}
