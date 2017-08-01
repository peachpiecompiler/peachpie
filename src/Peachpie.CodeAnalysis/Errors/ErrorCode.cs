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
    /// <remarks>
    /// New diagnostics must be added to the end of the corresponding severity group in order not to change the
    /// codes of the current ones.
    /// </remarks>
    internal enum ErrorCode
    {
        // 
        // Fatal errors
        //
        FTL_InputFileNameTooLong = 1000,

        //
        // Errors
        //
        ERR_BadCompilationOptionValue = 2000,
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
        ERR_StartupObjectNotFound,
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
        ERR_NotYetImplemented, // Used for all valid PHP constructs that Peachipe doesn't currently support.
        ERR_CircularBase,
        ERR_TypeNameCannotBeResolved,
        ERR_PositionalArgAfterUnpacking,    // Cannot use positional argument after argument unpacking
        /// <summary>Call to a member function {0} on {1}</summary>
        ERR_MethodCalledOnNonObject,
        /// <summary>Value of type {0} cannot be passed by reference.</summary>
        ERR_ValueOfTypeCannotBeAliased,

        //
        // Warnings
        //
        WRN_AnalyzerCannotBeCreated = 3000,
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
        /// <summary>The declaration of class, interface or trait is ambiguous since its base types cannot be resolved.</summary>
        WRN_AmbiguousDeclaration,
        WRN_UnreachableCode,
        WRN_NotYetImplementedIgnored,
        WRN_NoSourceFiles,

        //
        // Visible information
        //
        INF_UnableToLoadSomeTypesInAnalyzer = 4000,
    }
}
