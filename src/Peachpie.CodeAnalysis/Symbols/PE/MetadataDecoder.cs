using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols.PE;
using Roslyn.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Helper class to resolve metadata tokens and signatures.
    /// </summary>
    internal class MetadataDecoder : MetadataDecoder<PEModuleSymbol, TypeSymbol, MethodSymbol, FieldSymbol, Symbol>
    {
        /// <summary>
        /// Type context for resolving generic type arguments.
        /// </summary>
        private readonly PENamedTypeSymbol _typeContextOpt;

        /// <summary>
        /// Method context for resolving generic method type arguments.
        /// </summary>
        private readonly PEMethodSymbol _methodContextOpt;

        public MetadataDecoder(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol context) :
            this(moduleSymbol, context, null)
        {
        }

        public MetadataDecoder(
            PEModuleSymbol moduleSymbol,
            PEMethodSymbol context) :
            this(moduleSymbol, (PENamedTypeSymbol)context.ContainingType, context)
        {
        }

        public MetadataDecoder(
            PEModuleSymbol moduleSymbol) :
            this(moduleSymbol, null, null)
        {
        }

        private MetadataDecoder(PEModuleSymbol moduleSymbol, PENamedTypeSymbol typeContextOpt, PEMethodSymbol methodContextOpt)
            // TODO (tomat): if the containing assembly is a source assembly and we are about to decode assembly level attributes, we run into a cycle,
            // so for now ignore the assembly identity.
            : base(moduleSymbol.Module, (moduleSymbol.ContainingAssembly is PEAssemblySymbol) ? moduleSymbol.ContainingAssembly.Identity : null, SymbolFactory.Instance, moduleSymbol)
        {
            Debug.Assert((object)moduleSymbol != null);

            _typeContextOpt = typeContextOpt;
            _methodContextOpt = methodContextOpt;
        }

        internal PEModuleSymbol ModuleSymbol
        {
            get { return moduleSymbol; }
        }

        protected override TypeSymbol GetGenericMethodTypeParamSymbol(int position)
        {
            if ((object)_methodContextOpt == null)
            {
                return new UnsupportedMetadataTypeSymbol(); // type parameter not associated with a method
            }

            var typeParameters = _methodContextOpt.TypeParameters;

            if (typeParameters.Length <= position)
            {
                return new UnsupportedMetadataTypeSymbol(); // type parameter position too large
            }

            return (TypeSymbol)typeParameters[position];
        }

        protected override TypeSymbol GetGenericTypeParamSymbol(int position)
        {
            PENamedTypeSymbol type = _typeContextOpt;

            while ((object)type != null && (type.MetadataArity - type.Arity) > position)
            {
                type = type.ContainingSymbol as PENamedTypeSymbol;
            }

            if ((object)type == null || type.MetadataArity <= position)
            {
                return new UnsupportedMetadataTypeSymbol(); // position of type parameter too large
            }

            position -= type.MetadataArity - type.Arity;
            Debug.Assert(position >= 0 && position < type.Arity);

            return (TypeSymbol)type.TypeParameters[position];
        }

        protected override ConcurrentDictionary<TypeDefinitionHandle, TypeSymbol> GetTypeHandleToTypeMap()
        {
            return moduleSymbol.TypeHandleToTypeMap;
        }

        protected override ConcurrentDictionary<TypeReferenceHandle, TypeSymbol> GetTypeRefHandleToTypeMap()
        {
            return moduleSymbol.TypeRefHandleToTypeMap;
        }

        protected override TypeSymbol LookupNestedTypeDefSymbol(TypeSymbol container, ref MetadataTypeName emittedName)
        {
            var result = container.LookupMetadataType(ref emittedName);
            Debug.Assert((object)result != null);

            return result;
        }

        /// <summary>
        /// Lookup a type defined in referenced assembly.
        /// </summary>
        /// <param name="referencedAssemblyIndex"></param>
        /// <param name="emittedName"></param>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(
            int referencedAssemblyIndex,
            ref MetadataTypeName emittedName)
        {
            try
            {
                AssemblySymbol assembly = (AssemblySymbol)moduleSymbol.ReferencedAssemblySymbols[referencedAssemblyIndex];
                return assembly.LookupTopLevelMetadataType(ref emittedName, digThroughForwardedTypes: true);
            }
            catch (Exception e) when (FatalError.Report(e)) // Trying to get more useful Watson dumps.
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Lookup a type defined in a module of a multi-module assembly.
        /// </summary>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(string moduleName, ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            throw new NotImplementedException();
            //foreach (ModuleSymbol m in moduleSymbol.ContainingAssembly.Modules)
            //{
            //    if (string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase))
            //    {
            //        if ((object)m == (object)moduleSymbol)
            //        {
            //            return moduleSymbol.LookupTopLevelMetadataType(ref emittedName, out isNoPiaLocalType);
            //        }
            //        else
            //        {
            //            isNoPiaLocalType = false;
            //            return m.LookupTopLevelMetadataType(ref emittedName);
            //        }
            //    }
            //}

            //isNoPiaLocalType = false;
            //return new MissingMetadataTypeSymbol.TopLevel(new MissingModuleSymbolWithName(moduleSymbol.ContainingAssembly, moduleName), ref emittedName, SpecialType.None);
        }

        /// <summary>
        /// Lookup a type defined in this module.
        /// This method will be called only if the type we are
        /// looking for hasn't been loaded yet. Otherwise, MetadataDecoder
        /// would have found the type in TypeDefRowIdToTypeMap based on its 
        /// TypeDef row id. 
        /// </summary>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            isNoPiaLocalType = false;
            return moduleSymbol.LookupTopLevelMetadataType(ref emittedName); //, out isNoPiaLocalType);
        }

        protected override int GetIndexOfReferencedAssembly(AssemblyIdentity identity)
        {
            // Go through all assemblies referenced by the current module and
            // find the one which *exactly* matches the given identity.
            // No unification will be performed
            var assemblies = this.moduleSymbol.ReferencedAssemblies;
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (identity.Equals(assemblies[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        ///// <summary>
        ///// Perform a check whether the type or at least one of its generic arguments 
        ///// is defined in the specified assemblies. The check is performed recursively. 
        ///// </summary>
        //public static bool IsOrClosedOverATypeFromAssemblies(TypeSymbol symbol, ImmutableArray<AssemblySymbol> assemblies)
        //{
        //    switch (symbol.Kind)
        //    {
        //        case SymbolKind.TypeParameter:
        //            return false;

        //        case SymbolKind.ArrayType:
        //            return IsOrClosedOverATypeFromAssemblies(((ArrayTypeSymbol)symbol).ElementType, assemblies);

        //        case SymbolKind.PointerType:
        //            return IsOrClosedOverATypeFromAssemblies(((PointerTypeSymbol)symbol).PointedAtType, assemblies);

        //        case SymbolKind.DynamicType:
        //            return false;

        //        case SymbolKind.ErrorType:
        //            goto case SymbolKind.NamedType;
        //        case SymbolKind.NamedType:

        //            var namedType = (NamedTypeSymbol)symbol;
        //            AssemblySymbol containingAssembly = symbol.OriginalDefinition.ContainingAssembly;
        //            int i;

        //            if ((object)containingAssembly != null)
        //            {
        //                for (i = 0; i < assemblies.Length; i++)
        //                {
        //                    if (ReferenceEquals(containingAssembly, assemblies[i]))
        //                    {
        //                        return true;
        //                    }
        //                }
        //            }

        //            do
        //            {
        //                var arguments = namedType.TypeArgumentsNoUseSiteDiagnostics;
        //                int count = arguments.Length;

        //                for (i = 0; i < count; i++)
        //                {
        //                    if (IsOrClosedOverATypeFromAssemblies(arguments[i], assemblies))
        //                    {
        //                        return true;
        //                    }
        //                }

        //                namedType = (NamedTypeSymbol)namedType.ContainingType;
        //            }
        //            while ((object)namedType != null);

        //            return false;

        //        default:
        //            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        //    }
        //}

        protected override TypeSymbol SubstituteNoPiaLocalType(
            TypeDefinitionHandle typeDef,
            ref MetadataTypeName name,
            string interfaceGuid,
            string scope,
            string identifier)
        {
            TypeSymbol result;

            try
            {
                bool isInterface = Module.IsInterfaceOrThrow(typeDef);
                TypeSymbol baseType = null;

                if (!isInterface)
                {
                    EntityHandle baseToken = Module.GetBaseTypeOfTypeOrThrow(typeDef);

                    if (!baseToken.IsNil)
                    {
                        baseType = GetTypeOfToken(baseToken);
                    }
                }

                throw new NotImplementedException();
                //result = SubstituteNoPiaLocalType(
                //    ref name,
                //    isInterface,
                //    baseType,
                //    interfaceGuid,
                //    scope,
                //    identifier,
                //    (AssemblySymbol)moduleSymbol.ContainingAssembly);
            }
            catch (BadImageFormatException mrEx)
            {
                result = GetUnsupportedMetadataTypeSymbol(mrEx);
            }

            Debug.Assert((object)result != null);

            ConcurrentDictionary<TypeDefinitionHandle, TypeSymbol> cache = GetTypeHandleToTypeMap();
            Debug.Assert(cache != null);

            TypeSymbol newresult = cache.GetOrAdd(typeDef, result);
            Debug.Assert(ReferenceEquals(newresult, result) || (newresult.Kind == SymbolKind.ErrorType));

            return newresult;
        }

        protected override MethodSymbol FindMethodSymbolInType(TypeSymbol typeSymbol, MethodDefinitionHandle targetMethodDef)
        {
            Debug.Assert(typeSymbol is PENamedTypeSymbol || typeSymbol is ErrorTypeSymbol);

            foreach (Symbol member in typeSymbol.GetMembers())//.GetMembersUnordered())
            {
                PEMethodSymbol method = member as PEMethodSymbol;
                if ((object)method != null && method.Handle == targetMethodDef)
                {
                    return method;
                }
            }

            return null;
        }

        protected override FieldSymbol FindFieldSymbolInType(TypeSymbol typeSymbol, FieldDefinitionHandle fieldDef)
        {
            Debug.Assert(typeSymbol is PENamedTypeSymbol || typeSymbol is ErrorTypeSymbol);

            foreach (Symbol member in typeSymbol.GetMembers())//.GetMembersUnordered())
            {
                PEFieldSymbol field = member as PEFieldSymbol;
                if ((object)field != null && field.Handle == fieldDef)
                {
                    return field;
                }
            }

            return null;
        }

        internal override Symbol GetSymbolForMemberRef(MemberReferenceHandle memberRef, TypeSymbol scope = null, bool methodsOnly = false)
        {
            TypeSymbol targetTypeSymbol = GetMemberRefTypeSymbol(memberRef);

            if ((object)scope != null)
            {
                Debug.Assert(scope.Kind == SymbolKind.NamedType || scope.Kind == SymbolKind.ErrorType);

                // We only want to consider members that are at or above "scope" in the type hierarchy.
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                if (scope != targetTypeSymbol &&
                    !(targetTypeSymbol.IsInterfaceType()
                        ? scope.AllInterfaces.Contains((NamedTypeSymbol)targetTypeSymbol)
                        : scope.IsDerivedFrom(targetTypeSymbol, ignoreDynamic: false, useSiteDiagnostics: ref useSiteDiagnostics)))
                {
                    return null;
                }
            }

            // We're going to use a special decoder that can generate usable symbols for type parameters without full context.
            // (We're not just using a different type - we're also changing the type context.)
            var memberRefDecoder = new MemberRefMetadataDecoder(moduleSymbol, targetTypeSymbol);

            return memberRefDecoder.FindMember(targetTypeSymbol, memberRef, methodsOnly);
        }

        protected override void EnqueueTypeSymbolInterfacesAndBaseTypes(Queue<TypeDefinitionHandle> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeSymbol typeSymbol)
        {
            throw new NotImplementedException();
            //foreach (NamedTypeSymbol @interface in typeSymbol.InterfacesNoUseSiteDiagnostics())
            //{
            //    EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, @interface);
            //}

            //EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, typeSymbol.BaseTypeNoUseSiteDiagnostics);
        }

        protected override void EnqueueTypeSymbol(Queue<TypeDefinitionHandle> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeSymbol typeSymbol)
        {
            if ((object)typeSymbol != null)
            {
                PENamedTypeSymbol peTypeSymbol = typeSymbol as PENamedTypeSymbol;
                if ((object)peTypeSymbol != null && ReferenceEquals(peTypeSymbol.ContainingPEModule, moduleSymbol))
                {
                    typeDefsToSearch.Enqueue(peTypeSymbol.Handle);
                }
                else
                {
                    typeSymbolsToSearch.Enqueue(typeSymbol);
                }
            }
        }

        protected override MethodDefinitionHandle GetMethodHandle(MethodSymbol method)
        {
            PEMethodSymbol peMethod = method as PEMethodSymbol;
            if ((object)peMethod != null && ReferenceEquals(peMethod.ContainingModule, moduleSymbol))
            {
                return peMethod.Handle;
            }

            return default(MethodDefinitionHandle);
        }
    }
}
