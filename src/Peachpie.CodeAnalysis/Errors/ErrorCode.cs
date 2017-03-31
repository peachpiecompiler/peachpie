using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Errors
{
    /// <summary>
    /// A database of all possible diagnostics used by PHP compiler. The severity can be determined by the prefix:
    /// "FTL_" stands for fatal error, "ERR_" for error, "WRN_" for warning, "INF_" for visible information and
    /// "HDN_" for hidden information. Messages and other information are stored in the resources,
    /// <see cref="ErrorFacts"/> contains the naming logic.
    /// </summary>
    internal enum ErrorCode
    {
        ERR_BadCompilationOptionValue,
        ERR_BadWin32Resource,
        ERR_BinaryFile,
        ERR_CantOpenFileWrite,
        ERR_CantOpenWin32Icon,
        ERR_CantOpenWin32Manifest,
        ERR_CantOpenWin32Resource,
        ERR_CantReadResource,
        ERR_CantReadRulesetFile,
        ERR_CompileCancelled,
        ERR_EncReferenceToAddedMember,
        ERR_ErrorBuildingWin32Resource,
        ERR_ErrorOpeningAssemblyFile,
        ERR_ErrorOpeningModuleFile,
        ERR_ExpectedSingleScript,
        ERR_FailedToCreateTempFile,
        ERR_FileNotFound,
        ERR_InvalidAssemblyMetadata,
        ERR_InvalidDebugInformationFormat,
        ERR_MetadataFileNotAssembly,
        ERR_InvalidFileAlignment,
        ERR_InvalidModuleMetadata,
        ERR_InvalidOutputName,
        ERR_InvalidPathMap,
        ERR_InvalidSubsystemVersion,
        ERR_LinkedNetmoduleMetadataMustProvideFullPEImage,
        ERR_MetadataFileNotFound,
        ERR_MetadataFileNotModule,
        ERR_MetadataNameTooLong,
        ERR_MetadataReferencesNotSupported,
        ERR_NoSourceFile,
        ERR_OpenResponseFile,
        ERR_OutputWriteFailed,
        ERR_PdbWritingFailed,
        ERR_PermissionSetAttributeFileReadError,
        ERR_PublicKeyContainerFailure,
        ERR_PublicKeyFileFailure,
        ERR_ResourceFileNameNotUnique,
        ERR_ResourceInModule,
        ERR_ResourceNotUnique,
        ERR_TooManyUserStrings,

        ERR_CircularBase,
        ERR_TypeNameCannotBeResolved,

        FTL_InputFileNameTooLong,

        INF_UnableToLoadSomeTypesInAnalyzer,

        WRN_AnalyzerCannotBeCreated,
        WRN_NoAnalyzerInAssembly,
        WRN_NoConfigNotOnCommandLine,
        WRN_PdbLocalNameTooLong,
        WRN_PdbUsingNameTooLong,
        WRN_UnableToLoadAnalyzer,
        WRN_UndefinedFunctionCall,
        WRN_UninitializedVariableUse,
        WRN_UndefinedType,
        WRN_UndefinedMethodCall,
        WRN_EvalDiscouraged,
    }
}
