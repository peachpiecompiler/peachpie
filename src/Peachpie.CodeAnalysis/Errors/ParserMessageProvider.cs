using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using Devsense.PHP.Errors;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Errors
{
    /// <summary>
    /// This class stores a database of errors that can be thrown by Devsense PHP parser. It is populated dynamically -
    /// every time the parser throws an error, it must be registered in this class so that its details (such as format
    /// string) can be later retrieved.
    /// </summary>
    internal class ParserMessageProvider : CommonMessageProvider, IObjectWritable, IObjectReadable
    {
        private ConcurrentDictionary<int, ErrorInfo> _errorInfos = new ConcurrentDictionary<int, ErrorInfo>();

        public static readonly ParserMessageProvider Instance = new ParserMessageProvider();

        private ParserMessageProvider()
        {
        }

        public override string CodePrefix => "PHP";

        public override Type ErrorCodeType => typeof(int);

        public override int ERR_BadCompilationOptionValue
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_BadWin32Resource
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_BinaryFile
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_CantOpenFileWrite
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_CantOpenWin32Icon
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_CantOpenWin32Manifest
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_CantOpenWin32Resource
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_CantReadResource
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_CantReadRulesetFile
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_CompileCancelled
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_EncReferenceToAddedMember
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_ErrorBuildingWin32Resource
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_ErrorOpeningAssemblyFile
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_ErrorOpeningModuleFile
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_ExpectedSingleScript
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_FailedToCreateTempFile
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_FileNotFound
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidAssemblyMetadata
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidDebugInformationFormat
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidFileAlignment
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidModuleMetadata
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidOutputName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidPathMap
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidSubsystemVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_LinkedNetmoduleMetadataMustProvideFullPEImage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_MetadataFileNotAssembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_MetadataFileNotFound
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_MetadataFileNotModule
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_MetadataNameTooLong
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_MetadataReferencesNotSupported
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_NoSourceFile
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_OpenResponseFile
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_OutputWriteFailed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_PdbWritingFailed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_PermissionSetAttributeFileReadError
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_PublicKeyContainerFailure
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_PublicKeyFileFailure
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_ResourceFileNameNotUnique
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_ResourceInModule
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_ResourceNotUnique
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_TooManyUserStrings
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int FTL_InputFileNameTooLong
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int INF_UnableToLoadSomeTypesInAnalyzer
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int WRN_AnalyzerCannotBeCreated
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int WRN_NoAnalyzerInAssembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int WRN_NoConfigNotOnCommandLine
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int WRN_PdbLocalNameTooLong
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int WRN_PdbUsingNameTooLong
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int WRN_UnableToLoadAnalyzer
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string ConvertSymbolToString(int errorCode, ISymbol symbol)
        {
            throw new NotImplementedException();
        }

        public override Diagnostic CreateDiagnostic(int code, Location location, params object[] args)
        {
            var info = new DiagnosticInfo(this, code, args);
            return new DiagnosticWithInfo(info, location);
        }

        public Diagnostic CreateDiagnostic(bool isWarningAsError, int code, Location location, params object[] args)
        {
            var info = new DiagnosticInfo(this, isWarningAsError, code, args);
            return new DiagnosticWithInfo(info, location);
        }

        public override string GetCategory(int code) => Diagnostic.CompilerDiagnosticCategory;

        public override LocalizableString GetDescription(int code)
        {
            return null;
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
            throw new NotImplementedException();
        }

        public override string GetMessagePrefix(string id, DiagnosticSeverity severity, bool isWarningAsError, CultureInfo culture)
        {
            return MessageProvider.Instance.GetMessagePrefix(id, severity, isWarningAsError, culture);
        }

        public override DiagnosticSeverity GetSeverity(int code)
        {
            var parserSeverity = _errorInfos[code].Severity;
            return ConvertSeverity(parserSeverity);
        }

        public override LocalizableString GetTitle(int code)
        {
            return null;
        }

        public override int GetWarningLevel(int code)
        {
            return 0;
        }

        public override string LoadMessage(int code, CultureInfo language)
        {
            return _errorInfos[code].FormatString;
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

        public void RegisterError(ErrorInfo errorInfo)
        {
            _errorInfos.GetOrAdd(errorInfo.Id, errorInfo);
        }

        private static DiagnosticSeverity ConvertSeverity(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Information:
                    return DiagnosticSeverity.Info;
                case ErrorSeverity.Warning:
                case ErrorSeverity.WarningAsError:      // TODO: Check if it is right
                    return DiagnosticSeverity.Warning;
                case ErrorSeverity.Error:
                case ErrorSeverity.FatalError:
                    return DiagnosticSeverity.Error;
                default:
                    throw new ArgumentException(nameof(severity));
            }
        }

        Func<ObjectReader, object> IObjectReadable.GetReader()
        {
            throw new NotImplementedException();
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}