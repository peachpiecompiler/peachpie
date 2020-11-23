using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Roslyn.Utilities;
using Peachpie.CodeAnalysis.Errors;

namespace Pchp.CodeAnalysis.Errors
{
    /// <summary>
    /// A <see cref="CommonMessageProvider"/> implementation for compilation errors stored in <see cref="ErrorCode"/>.
    /// </summary>
    internal class MessageProvider : CommonMessageProvider, IObjectWritable
    {
        public static readonly MessageProvider Instance = new MessageProvider();

        public override string CodePrefix
        {
            get
            {
                return "PHP";
            }
        }

        public override Type ErrorCodeType
        {
            get { return typeof(ErrorCode); }
        }

        bool IObjectWritable.ShouldReuseInSerialization => false;

        public override int ERR_BadCompilationOptionValue => (int)ErrorCode.ERR_BadCompilationOptionValue;

        public override int ERR_BadWin32Resource => (int)ErrorCode.ERR_BadWin32Resource;

        public override int ERR_BinaryFile => (int)ErrorCode.ERR_BinaryFile;

        public override int ERR_CantOpenFileWrite => (int)ErrorCode.ERR_CantOpenFileWrite;

        public override int ERR_CantOpenWin32Icon => (int)ErrorCode.ERR_CantOpenWin32Icon;

        public override int ERR_CantOpenWin32Manifest => (int)ErrorCode.ERR_CantOpenWin32Manifest;

        public override int ERR_CantOpenWin32Resource => (int)ErrorCode.ERR_CantOpenWin32Resource;

        public override int ERR_CantReadResource => (int)ErrorCode.ERR_CantReadResource;

        public override int ERR_CantReadRulesetFile => (int)ErrorCode.ERR_CantReadRulesetFile;

        public override int ERR_CompileCancelled => (int)ErrorCode.ERR_CompileCancelled;

        public override int ERR_EncReferenceToAddedMember => (int)ErrorCode.ERR_EncReferenceToAddedMember;

        public override int ERR_ErrorBuildingWin32Resource => (int)ErrorCode.ERR_ErrorBuildingWin32Resource;

        public override int ERR_ErrorOpeningAssemblyFile => (int)ErrorCode.ERR_ErrorOpeningAssemblyFile;

        public override int ERR_ErrorOpeningModuleFile => (int)ErrorCode.ERR_ErrorOpeningModuleFile;

        public override int ERR_ExpectedSingleScript => (int)ErrorCode.ERR_ExpectedSingleScript;

        public override int ERR_FailedToCreateTempFile => (int)ErrorCode.ERR_FailedToCreateTempFile;

        public override int ERR_FileNotFound => (int)ErrorCode.ERR_FileNotFound;

        public override int ERR_InvalidAssemblyMetadata => (int)ErrorCode.ERR_InvalidAssemblyMetadata;

        public override int ERR_InvalidDebugInformationFormat => (int)ErrorCode.ERR_InvalidDebugInformationFormat;

        public override int ERR_InvalidFileAlignment => (int)ErrorCode.ERR_InvalidFileAlignment;

        public override int ERR_InvalidModuleMetadata => (int)ErrorCode.ERR_InvalidModuleMetadata;

        public override int ERR_InvalidOutputName => (int)ErrorCode.ERR_InvalidOutputName;

        public override int ERR_InvalidPathMap => (int)ErrorCode.ERR_InvalidPathMap;

        public override int ERR_InvalidSubsystemVersion => (int)ErrorCode.ERR_InvalidSubsystemVersion;

        public override int ERR_LinkedNetmoduleMetadataMustProvideFullPEImage => (int)ErrorCode.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage;

        public override int ERR_MetadataFileNotAssembly => (int)ErrorCode.ERR_MetadataFileNotAssembly;

        public override int ERR_MetadataFileNotFound => (int)ErrorCode.ERR_MetadataFileNotFound;

        public override int ERR_MetadataFileNotModule => (int)ErrorCode.ERR_MetadataFileNotModule;

        public override int ERR_MetadataNameTooLong => (int)ErrorCode.ERR_MetadataNameTooLong;

        public override int ERR_MetadataReferencesNotSupported => (int)ErrorCode.ERR_MetadataReferencesNotSupported;

        public override int ERR_NoSourceFile => (int)ErrorCode.ERR_NoSourceFile;

        public override int ERR_OpenResponseFile => (int)ErrorCode.ERR_OpenResponseFile;

        public override int ERR_OutputWriteFailed => (int)ErrorCode.ERR_OutputWriteFailed;

        public override int ERR_PdbWritingFailed => (int)ErrorCode.ERR_PdbWritingFailed;

        public override int ERR_PermissionSetAttributeFileReadError => (int)ErrorCode.ERR_PermissionSetAttributeFileReadError;

        public override int ERR_PublicKeyContainerFailure => (int)ErrorCode.ERR_PublicKeyContainerFailure;

        public override int ERR_PublicKeyFileFailure => (int)ErrorCode.ERR_PublicKeyFileFailure;

        public override int ERR_ResourceFileNameNotUnique => (int)ErrorCode.ERR_ResourceFileNameNotUnique;

        public override int ERR_ResourceInModule => (int)ErrorCode.ERR_ResourceInModule;

        public override int ERR_ResourceNotUnique => (int)ErrorCode.ERR_ResourceNotUnique;

        public override int ERR_TooManyUserStrings => (int)ErrorCode.ERR_TooManyUserStrings;

        public override int ERR_BadSourceCodeKind => (int)ErrorCode.ERR_BadSourceCodeKind;

        public override int ERR_BadDocumentationMode => (int)ErrorCode.ERR_BadDocumentationMode;

        public override int ERR_MutuallyExclusiveOptions => (int)ErrorCode.ERR_MutuallyExclusiveOptions;

        public override int ERR_InvalidInstrumentationKind => (int)ErrorCode.ERR_InvalidInstrumentationKind;

        public override int ERR_InvalidHashAlgorithmName => (int)ErrorCode.ERR_InvalidHashAlgorithmName;

        public override int ERR_OptionMustBeAbsolutePath => (int)ErrorCode.ERR_OptionMustBeAbsolutePath;

        public override int ERR_EncodinglessSyntaxTree => (int)ErrorCode.ERR_EncodinglessSyntaxTree;

        public override int ERR_PeWritingFailure => (int)ErrorCode.ERR_PeWritingFailure;

        public override int ERR_ModuleEmitFailure => (int)ErrorCode.ERR_ModuleEmitFailure;

        public override int ERR_EncUpdateFailedMissingAttribute => (int)ErrorCode.ERR_EncUpdateFailedMissingAttribute;

        public override int ERR_InvalidDebugInfo => (int)ErrorCode.ERR_InvalidDebugInfo;

        public override int ERR_BadAssemblyName => (int)ErrorCode.ERR_BadAssemblyName;

        public override int ERR_MultipleAnalyzerConfigsInSameDir => (int)ErrorCode.ERR_MultipleAnalyzerConfigsInSameDir;

        public override int FTL_InvalidInputFileName => (int)ErrorCode.FTL_InvalidInputFileName;

        public override int INF_UnableToLoadSomeTypesInAnalyzer => (int)ErrorCode.INF_UnableToLoadSomeTypesInAnalyzer;

        public override int WRN_AnalyzerCannotBeCreated => (int)ErrorCode.WRN_AnalyzerCannotBeCreated;

        public override int WRN_NoAnalyzerInAssembly => (int)ErrorCode.WRN_NoAnalyzerInAssembly;

        public override int WRN_NoConfigNotOnCommandLine => (int)ErrorCode.WRN_NoConfigNotOnCommandLine;

        public override int WRN_PdbLocalNameTooLong => (int)ErrorCode.WRN_PdbLocalNameTooLong;

        public override int WRN_PdbUsingNameTooLong => (int)ErrorCode.WRN_PdbUsingNameTooLong;

        public override int WRN_UnableToLoadAnalyzer => (int)ErrorCode.WRN_UnableToLoadAnalyzer;

        public override int WRN_GeneratorFailedDuringInitialization => (int)ErrorCode.WRN_GeneratorFailedDuringInitialization;

        public override int WRN_GeneratorFailedDuringGeneration => (int)ErrorCode.WRN_GeneratorFailedDuringGeneration;

        public override string GetErrorDisplayString(ISymbol symbol)
        {
            // show extra info for assembly if possible such as version, public key token etc.
            if (symbol.Kind == SymbolKind.Assembly || symbol.Kind == SymbolKind.Namespace)
            {
                return symbol.ToString();
            }

            return symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        }

        public override Diagnostic CreateDiagnostic(int code, Location location, params object[] args)
        {
            var info = new DiagnosticInfo(this, code, args);
            return new DiagnosticWithInfo(info, location);
        }

        public Diagnostic CreateDiagnostic(ErrorCode code, Location location, params object[] args) =>
            CreateDiagnostic((int)code, location, args);

        public override Diagnostic CreateDiagnostic(DiagnosticInfo info) => new DiagnosticWithInfo(info, Location.None);

        public override string GetCategory(int code)
        {
            //throw new NotImplementedException();

            return Diagnostic.CompilerDiagnosticCategory;
        }

        public override LocalizableString GetDescription(int code)
        {
            return ErrorStrings.ResourceManager.GetString(((ErrorCode)code).ToString() + "_Description");
        }

        public override ReportDiagnostic GetDiagnosticReport(DiagnosticInfo diagnosticInfo, CompilationOptions options)
        {
            throw new NotImplementedException();
        }

        public override string GetHelpLink(int code)
        {
            throw new NotImplementedException();
        }

        public override LocalizableString GetMessageFormat(int code)
        {
            //return new LocalizableResourceString(code.ToString(), ErrorStrings.ResourceManager, typeof(ErrorFacts));
            throw new NotImplementedException();
        }

        public override string GetMessagePrefix(string id, DiagnosticSeverity severity, bool isWarningAsError, CultureInfo culture)
        {
            return string.Format(culture, "{0} {1}",
                severity == DiagnosticSeverity.Error || isWarningAsError ? "error" : "warning",
                id);
        }

        public override DiagnosticSeverity GetSeverity(int code)
        {
            return ErrorFacts.GetSeverity((ErrorCode)code);
        }

        public override LocalizableString GetTitle(int code)
        {
            return ErrorStrings.ResourceManager.GetString(((ErrorCode)code).ToString() + "_Title");
        }

        public override int GetWarningLevel(int code)
        {
            switch ((ErrorCode)code)
            {
                default:
                    return 0;
            }
        }

        public override string LoadMessage(int code, CultureInfo language)
        {
            return ErrorFacts.GetFormatString((ErrorCode)code, language);
        }

        public override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName)
        {
            throw new NotImplementedException();
        }

        public override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName1, string parameterName2)
        {
            throw new NotImplementedException();
        }

        public override void ReportDuplicateMetadataReferenceStrong(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity)
        {
            throw new NotImplementedException();
        }

        public override void ReportDuplicateMetadataReferenceWeak(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity)
        {
            throw new NotImplementedException();
        }

        public override void ReportInvalidAttributeArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        public override void ReportInvalidNamedArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex, ITypeSymbol attributeClass, string parameterName)
        {
            throw new NotImplementedException();
        }

        public override void ReportMarshalUnmanagedTypeNotValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        public override void ReportMarshalUnmanagedTypeOnlyValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        public override void ReportParameterNotValidForType(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex)
        {
            throw new NotImplementedException();
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
