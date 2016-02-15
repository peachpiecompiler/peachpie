// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Cci = Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit.NoPia;

namespace Pchp.CodeAnalysis.Emit
{
    internal class PEModuleBuilder : CommonPEModuleBuilder, Cci.IModule, ITokenDeferral
    {
        private readonly IModuleSymbol _sourceModule;
        private readonly PhpCompilation _compilation;
        private readonly OutputKind _outputKind;
        private readonly EmitOptions _emitOptions;
        private readonly Cci.ModulePropertiesForSerialization _serializationProperties;

        internal readonly IEnumerable<ResourceDescription> ManifestResources;
        internal readonly CommonModuleCompilationState CompilationState;

        // This is a map from the document "name" to the document.
        // Document "name" is typically a file path like "C:\Abc\Def.cs". However, that is not guaranteed.
        // For compatibility reasons the names are treated as case-sensitive in C# and case-insensitive in VB.
        // Neither language trims the names, so they are both sensitive to the leading and trailing whitespaces.
        // NOTE: We are not considering how filesystem or debuggers do the comparisons, but how native implementations did.
        // Deviating from that may result in unexpected warnings or different behavior (possibly without warnings).
        private readonly ConcurrentDictionary<string, Cci.DebugSourceDocument> _debugDocuments;

        protected PEModuleBuilder(
            PhpCompilation compilation,
            IModuleSymbol sourceModule,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            OutputKind outputKind,
            EmitOptions emitOptions)
        {
            Debug.Assert(sourceModule != null);
            Debug.Assert(serializationProperties != null);

            _compilation = compilation;
            _sourceModule = sourceModule;
            _serializationProperties = serializationProperties;
            this.ManifestResources = manifestResources;
            _outputKind = outputKind;
            _emitOptions = emitOptions;
            this.CompilationState = new CommonModuleCompilationState();
            _debugDocuments = new ConcurrentDictionary<string, Cci.DebugSourceDocument>(compilation.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        }

        internal void SetDebugEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            //Debug.Assert(method == null || IsSourceDefinition((IMethodSymbol)method));

            //_debugEntryPoint = Translate(method, diagnostics, needDeclaration: true);

            throw new NotImplementedException();
        }

        public ArrayMethods ArrayMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Cci.IAssembly AsAssembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<Cci.ICustomAttribute> AssemblyAttributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<Cci.SecurityAttribute> AssemblySecurityAttributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Cci.IMethodReference DebugEntryPoint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string DefaultNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool GenerateVisualBasicStylePdb
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int HintNumberOfMethodDefinitions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public OutputKind Kind
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<string> LinkedAssembliesDebugInfo
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<Cci.ICustomAttribute> ModuleAttributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ModuleName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<Cci.IModuleReference> ModuleReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Cci.IMethodReference PEEntryPoint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Cci.ModulePropertiesForSerialization Properties
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<Cci.IWin32Resource> Win32Resources
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Cci.ResourceSection Win32ResourceSection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override Compilation CommonCompilation
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override CommonEmbeddedTypesManager CommonEmbeddedTypesManagerOpt
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override CommonModuleCompilationState CommonModuleCompilationState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override EmitOptions EmitOptions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override bool SupportsPrivateImplClass
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Cci.IDefinition AsDefinition(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public void Dispatch(Cci.MetadataVisitor visitor)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<Cci.AssemblyReferenceAlias> GetAssemblyReferenceAliases(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.IAssemblyReference> GetAssemblyReferences(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public Cci.IAssembly GetContainingAssembly(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public Cci.IAssemblyReference GetCorLibrary(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.ITypeReference> GetExportedTypes(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public uint GetFakeStringTokenForIL(string value)
        {
            throw new NotImplementedException();
        }

        public uint GetFakeSymbolTokenForIL(Cci.IReference value, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        public Cci.IFieldReference GetFieldForData(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<Cci.UsedNamespaceOrType> GetImports()
        {
            throw new NotImplementedException();
        }

        public Cci.IMethodReference GetInitArrayHelper()
        {
            throw new NotImplementedException();
        }

        public Cci.ITypeReference GetPlatformType(Cci.PlatformType t, EmitContext context)
        {
            throw new NotImplementedException();
        }

        public Cci.IReference GetReferenceFromToken(uint token)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.ManagedResource> GetResources(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public string GetStringFromToken(uint token)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetStrings()
        {
            throw new NotImplementedException();
        }

        public MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType t)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.IReference> ReferencesInIL(out int count)
        {
            throw new NotImplementedException();
        }

        internal override void CompilationFinished()
        {
            throw new NotImplementedException();
        }

        internal override Cci.ITypeReference EncTranslateType(ITypeSymbol type, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<Cci.INamespaceTypeDefinition> GetAnonymousTypes()
        {
            throw new NotImplementedException();
        }

        internal override ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> GetSynthesizedMembers()
        {
            throw new NotImplementedException();
        }

        internal override Cci.IAssemblyReference Translate(IAssemblySymbol symbol, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        internal override Cci.IMethodReference Translate(IMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration)
        {
            throw new NotImplementedException();
        }

        internal override Cci.ITypeReference Translate(ITypeSymbol symbol, SyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(EmitContext context)
        {
            throw new NotImplementedException();
        }
    }
}
