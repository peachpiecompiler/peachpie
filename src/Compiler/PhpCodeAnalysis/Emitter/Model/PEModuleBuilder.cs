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

        readonly StringTokenMap _stringsInILMap = new StringTokenMap();
        readonly TokenMap<Cci.IReference> _referencesInILMap = new TokenMap<Cci.IReference>();
        Cci.IMethodReference _peEntryPoint, _debugEntryPoint;

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

        public ArrayMethods ArrayMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Cci.IAssembly AsAssembly => this as Cci.IAssembly;

        public IEnumerable<Cci.ICustomAttribute> AssemblyAttributes
        {
            get
            {
                yield break; // throw new NotImplementedException();
            }
        }

        public IEnumerable<Cci.SecurityAttribute> AssemblySecurityAttributes
        {
            get
            {
                yield break; // throw new NotImplementedException();
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
                return 0; // throw new NotImplementedException();
            }
        }

        public OutputKind Kind => _outputKind;

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
                yield break; // throw new NotImplementedException();
            }
        }

        public string ModuleName => Name;

        public IEnumerable<Cci.IModuleReference> ModuleReferences
        {
            get
            {
                yield break; // throw new NotImplementedException();
            }
        }

        public virtual string Name => _sourceModule.Name;

        Cci.IMethodReference Cci.IModule.PEEntryPoint => _peEntryPoint;
        Cci.IMethodReference Cci.IModule.DebugEntryPoint => _debugEntryPoint;

        internal void SetPEEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition((IMethodSymbol)method));
            Debug.Assert(_outputKind.IsApplication());

            _peEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        internal void SetDebugEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition((IMethodSymbol)method));

            _debugEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        public Cci.ModulePropertiesForSerialization Properties => _serializationProperties;

        public IEnumerable<Cci.IWin32Resource> Win32Resources
        {
            get
            {
                return ImmutableArray<Cci.IWin32Resource>.Empty; // throw new NotImplementedException();
            }
            private set
            {
                throw new NotImplementedException();
            }
        }

        public Cci.ResourceSection Win32ResourceSection
        {
            get
            {
                return null; // throw new NotImplementedException();
            }
            private set
            {
                throw new NotImplementedException();
            }
        }

        internal override Compilation CommonCompilation => _compilation;

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

        internal override EmitOptions EmitOptions => _emitOptions;

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

        public virtual void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IModule)this);
        }

        public ImmutableArray<Cci.AssemblyReferenceAlias> GetAssemblyReferenceAliases(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.IAssemblyReference> GetAssemblyReferences(EmitContext context)
        {
            //Cci.IAssemblyReference corLibrary = GetCorLibraryReferenceToEmit(context);

            //// Only add Cor Library reference explicitly, PeWriter will add
            //// other references implicitly on as needed basis.
            //if (corLibrary != null)
            //{
            //    yield return corLibrary;
            //}

            if (_outputKind != OutputKind.NetModule)
            {
                //// Explicitly add references from added modules
                //foreach (var aRef in GetAssemblyReferencesFromAddedModules(context.Diagnostics))
                //{
                //    yield return aRef;
                //}
            }

            yield break;
        }

        public IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public Cci.IAssembly GetContainingAssembly(EmitContext context)
        {
            return _outputKind.IsNetModule() ? null : (Cci.IAssembly)this;
        }

        public Cci.IAssemblyReference GetCorLibrary(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.ITypeReference> GetExportedTypes(EmitContext context)
        {
            return ImmutableArray<Cci.ITypeReference>.Empty; // throw new NotImplementedException();
        }

        public uint GetFakeStringTokenForIL(string value)
        {
            return _stringsInILMap.GetOrAddTokenFor(value);
        }

        public uint GetFakeSymbolTokenForIL(Cci.IReference symbol, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            bool added;
            uint token = _referencesInILMap.GetOrAddTokenFor(symbol, out added);
            if (added)
            {
                ReferenceDependencyWalker.VisitReference(symbol, new EmitContext(this, syntaxNode, diagnostics));
            }
            return token;
        }

        public Cci.IFieldReference GetFieldForData(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<Cci.UsedNamespaceOrType> GetImports()
        {
            return ImmutableArray<Cci.UsedNamespaceOrType>.Empty; // throw new NotImplementedException();
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
            return _referencesInILMap.GetItem(token);
        }

        IEnumerable<Cci.ManagedResource> _lazyManagedResources;

        public IEnumerable<Cci.ManagedResource> GetResources(EmitContext context)
        {
            if (_lazyManagedResources == null)
            {
                var builder = ArrayBuilder<Cci.ManagedResource>.GetInstance();

                foreach (ResourceDescription r in ManifestResources)
                {
                    builder.Add(r.ToManagedResource(this));
                }

                if (_outputKind != OutputKind.NetModule)
                {
                    // Explicitly add resources from added modules
                    AddEmbeddedResourcesFromAddedModules(builder, context.Diagnostics);
                }

                _lazyManagedResources = builder.ToImmutableAndFree();
            }

            return _lazyManagedResources;
        }

        protected virtual void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics)
        {
            throw new NotSupportedException(); // override
        }

        public string GetStringFromToken(uint token)
        {
            return _stringsInILMap.GetItem(token);
        }
        
        public IEnumerable<string> GetStrings() => _stringsInILMap.GetAllItems();

        public MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context)
        {
            return ImmutableArray<Cci.INamespaceTypeDefinition>.Empty; //throw new NotImplementedException();
        }

        public bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType t)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.IReference> ReferencesInIL(out int count)
        {
            return _referencesInILMap.GetAllItemsAndCount(out count);
        }

        internal override void CompilationFinished()
        {
            this.CompilationState.Freeze();
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

        private bool IsSourceDefinition(IMethodSymbol method)
        {
            return (object)method.ContainingModule == _sourceModule && method.IsDefinition;
        }
    }
}
