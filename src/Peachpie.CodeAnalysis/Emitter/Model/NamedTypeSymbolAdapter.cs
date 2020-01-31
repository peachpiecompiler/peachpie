using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;
using Pchp.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Linq;

namespace Pchp.CodeAnalysis.Symbols
{
    internal partial class NamedTypeSymbol :
        Cci.ITypeReference,
        Cci.ITypeDefinition,
        Cci.INamedTypeReference,
        Cci.INamedTypeDefinition,
        Cci.INamespaceTypeReference,
        Cci.INamespaceTypeDefinition,
        Cci.INestedTypeReference,
        Cci.INestedTypeDefinition,
        Cci.IGenericTypeInstanceReference,
        Cci.ISpecializedNestedTypeReference
    {
        bool Cci.ITypeReference.IsEnum
        {
            get { return this.TypeKind == TypeKind.Enum; }
        }

        bool Cci.ITypeReference.IsValueType
        {
            get { return this.IsValueType; }
        }

        Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return AsTypeDefinitionImpl(moduleBeingBuilt);
        }

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (this.IsDefinition)
                {
                    return this.PrimitiveTypeCode;
                }

                return Cci.PrimitiveTypeCode.NotPrimitive;
            }
        }

        TypeDefinitionHandle Cci.ITypeReference.TypeDef
        {
            get
            {
                var peNamedType = this as PENamedTypeSymbol;
                if ((object)peNamedType != null)
                {
                    return peNamedType.Handle;
                }

                return default(TypeDefinitionHandle);
            }
        }

        Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get { return null; }
        }

        Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (!this.IsDefinition &&
                    this.Arity > 0)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get { return null; }
        }

        Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (this.IsDefinition &&
                    (object)this.ContainingType == null)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(this.IsDefinitionOrDistinct());

            if ((object)this.ContainingType == null &&
                this.IsDefinition &&
                object.ReferenceEquals(this.ContainingModule, moduleBeingBuilt.SourceModule))
            {
                return this;
            }

            return null;
        }

        Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference
        {
            get
            {
                if ((object)this.ContainingType != null)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return AsNestedTypeDefinitionImpl(moduleBeingBuilt);
        }

        private Cci.INestedTypeDefinition AsNestedTypeDefinitionImpl(PEModuleBuilder moduleBeingBuilt)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if ((object)this.ContainingType != null &&
                this.IsDefinition &&
                object.ReferenceEquals(this.ContainingModule, moduleBeingBuilt.SourceModule))
            {
                return this;
            }

            return null;
        }

        Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (!this.IsDefinition &&
                    (this.Arity == 0 || PEModuleBuilder.IsGenericType(this.ContainingType)))
                {
                    Debug.Assert((object)this.ContainingType != null &&
                            PEModuleBuilder.IsGenericType(this.ContainingType));
                    return this;
                }

                return null;
            }
        }

        Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return AsTypeDefinitionImpl(moduleBeingBuilt);
        }

        private Cci.ITypeDefinition AsTypeDefinitionImpl(PEModuleBuilder moduleBeingBuilt)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (this.IsDefinition && // can't be generic instantiation
                object.ReferenceEquals(this.ContainingModule, moduleBeingBuilt.SourceModule)) // must be declared in the module we are building
            {
                return this;
            }

            return null;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
            //We've not yet discovered a scenario in which we need this.
            //If you're hitting this exception. Uncomment the code below
            //and add a unit test.
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return AsTypeDefinitionImpl(moduleBeingBuilt);
        }

        Cci.ITypeReference Cci.ITypeDefinition.GetBaseClass(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.ITypeReference)this).AsTypeDefinition(context) != null);
            NamedTypeSymbol baseType = this.BaseType; // .BaseTypeNoUseSiteDiagnostics;

            if (this.TypeKind == TypeKind.Submission)
            {
                // although submission semantically doesn't have a base we need to emit one into metadata:
                Debug.Assert((object)baseType == null);
                baseType = this.ContainingAssembly.GetSpecialType(SpecialType.System_Object);
            }

            return ((object)baseType != null)
                ? moduleBeingBuilt.Translate(baseType, null /* (SyntaxNode)context.SyntaxNodeOpt */, context.Diagnostics)
                : null;
        }

        IEnumerable<Cci.IEventDefinition> Cci.ITypeDefinition.GetEvents(EmitContext context)
        {
            CheckDefinitionInvariant();
            return GetEventsToEmit().Cast<Cci.IEventDefinition>();
        }

        internal virtual IEnumerable<IEventSymbol> GetEventsToEmit()
        {
            CheckDefinitionInvariant();

            foreach (var m in this.GetMembers())
            {
                if (m.Kind == SymbolKind.Event)
                {
                    //yield return (EventSymbol)m;
                    throw new System.NotImplementedException();
                }
            }

            yield break;
        }

        IEnumerable<Cci.MethodImplementation> Cci.ITypeDefinition.GetExplicitImplementationOverrides(EmitContext context)
        {
            CheckDefinitionInvariant();

            if (this.TypeKind == TypeKind.Interface)
            {
                yield break;
            }

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (var method in this.GetMembers().OfType<MethodSymbol>().Concat(moduleBeingBuilt.GetSynthesizedMethods(this)))
            {
                Debug.Assert((object)method.PartialDefinitionPart == null); // must be definition

                var explicitImplementations = method.ExplicitInterfaceImplementations;
                if (explicitImplementations.Length != 0)
                {
                    foreach (var implemented in explicitImplementations)
                    {
                        yield return new Cci.MethodImplementation(method, moduleBeingBuilt.TranslateOverriddenMethodReference(implemented, context.SyntaxNodeOpt, context.Diagnostics));
                    }
                }

                //if (method.RequiresExplicitOverride())
                //{
                //    // If C# and the runtime don't agree on the overridden method, then 
                //    // we will mark the method as newslot (see MethodSymbolAdapter) and
                //    // specify the override explicitly.
                //    // This mostly affects accessors - C# ignores method interactions
                //    // between accessors and non-accessors, whereas the runtime does not.
                //    yield return new Microsoft.Cci.MethodImplementation(method, moduleBeingBuilt.TranslateOverriddenMethodReference((MethodSymbol)method.OverriddenMethod, context.SyntaxNodeOpt, context.Diagnostics));
                //}
                //else if (method.MethodKind == MethodKind.Destructor && this.SpecialType != SpecialType.System_Object)
                //{
                //    // New in Roslyn: all destructors explicitly override (or are) System.Object.Finalize so that
                //    // they are guaranteed to be runtime finalizers.  As a result, it is no longer possible to create
                //    // a destructor that will never be invoked by the runtime.
                //    // NOTE: If System.Object doesn't contain a destructor, you're on your own - this destructor may
                //    // or not be called by the runtime.
                //    TypeSymbol objectType = this.DeclaringCompilation.GetSpecialType(SpecialType.System_Object);
                //    foreach (Symbol objectMember in objectType.GetMembers(WellKnownMemberNames.DestructorName))
                //    {
                //        MethodSymbol objectMethod = objectMember as MethodSymbol;
                //        if ((object)objectMethod != null && objectMethod.MethodKind == MethodKind.Destructor)
                //        {
                //            yield return new Microsoft.Cci.MethodImplementation(method, moduleBeingBuilt.TranslateOverriddenMethodReference(objectMethod, context.SyntaxNodeOpt, context.Diagnostics));
                //        }
                //    }
                //}
            }
        }

        IEnumerable<Cci.IFieldDefinition> Cci.ITypeDefinition.GetFields(EmitContext context)
        {
            CheckDefinitionInvariant();

            foreach (var f in GetFieldsToEmit())
            {
                yield return (Cci.IFieldDefinition)f;
            }

            var generated = ((PEModuleBuilder)context.Module).GetSynthesizedFields(this);
            if (generated != null)
            {
                foreach (var f in generated)
                {
                    yield return f;
                }
            }
        }

        internal abstract IEnumerable<IFieldSymbol> GetFieldsToEmit();

        IEnumerable<Cci.IGenericTypeParameter> Cci.ITypeDefinition.GenericParameters
        {
            get
            {
                CheckDefinitionInvariant();

                foreach (var t in this.TypeParameters)
                {
                    yield return (Cci.IGenericTypeParameter)t;
                }
            }
        }

        ushort Cci.ITypeDefinition.GenericParameterCount
        {
            get
            {
                CheckDefinitionInvariant();

                return GenericParameterCountImpl;
            }
        }

        private ushort GenericParameterCountImpl
        {
            get { return (ushort)this.Arity; }
        }

        IEnumerable<Cci.TypeReferenceWithAttributes> Cci.ITypeDefinition.Interfaces(EmitContext context)
        {
            Debug.Assert(((Cci.ITypeReference)this).AsTypeDefinition(context) != null);

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (NamedTypeSymbol iiface in this.GetInterfacesToEmit())
            {
                var typeRef = moduleBeingBuilt.Translate(iiface, null, context.Diagnostics,
                    fromImplements: true);
                yield return new Cci.TypeReferenceWithAttributes(typeRef);
            }

            yield break;
        }

        protected ImmutableArray<NamedTypeSymbol> CalculateInterfacesToEmit()
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(this.ContainingModule is SourceModuleSymbol);

            ArrayBuilder<NamedTypeSymbol> builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            HashSet<NamedTypeSymbol> seen = null;
            InterfacesVisit(this, builder, ref seen);
            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Add the type to the builder and then recurse on its interfaces.
        /// </summary>
        /// <remarks>
        /// Pre-order depth-first search.
        /// </remarks>
        private static void InterfacesVisit(NamedTypeSymbol namedType, ArrayBuilder<NamedTypeSymbol> builder, ref HashSet<NamedTypeSymbol> seen)
        {
            // It's not clear how important the order of these interfaces is, but Dev10
            // maintains pre-order depth-first/declaration order, so we probably should as well.
            // That's why we're not using InterfacesAndTheirBaseInterfaces - it's an unordered set.
            foreach (NamedTypeSymbol @interface in namedType.Interfaces) // NoUseSiteDiagnostics())
            {
                if (seen == null)
                {
                    // Don't allocate until we see at least one interface.
                    seen = new HashSet<NamedTypeSymbol>();
                }
                if (seen.Add(@interface))
                {
                    builder.Add(@interface);
                    InterfacesVisit(@interface, builder, ref seen);
                }
            }
        }

        /// <summary>
        /// Gets the set of interfaces to emit on this type. This set can be different from the set returned by Interfaces property.
        /// </summary>
        internal abstract ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit();

        bool Cci.ITypeDefinition.IsAbstract
        {
            get
            {
                CheckDefinitionInvariant();
                return IsMetadataAbstract;
            }
        }

        internal virtual bool IsMetadataAbstract
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsAbstract || this.IsStatic;
            }
        }

        bool Cci.ITypeDefinition.IsBeforeFieldInit
        {
            get
            {
                CheckDefinitionInvariant();

                switch (this.TypeKind)
                {
                    case TypeKind.Enum:
                    case TypeKind.Delegate:
                        return false;
                }

                // PHP does not have exlicit static constructors:
                ////apply the beforefieldinit attribute unless there is an explicitly specified static constructor
                //foreach (var member in GetMembers(WellKnownMemberNames.StaticConstructorName))
                //{
                //    if (!member.IsImplicitlyDeclared)
                //    {
                //        return false;
                //    }
                //}

                return true;
            }
        }

        bool Cci.ITypeDefinition.IsComObject
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }

        bool Cci.ITypeDefinition.IsGeneric
        {
            get
            {
                CheckDefinitionInvariant();
                return this.Arity != 0;
            }
        }

        bool Cci.ITypeDefinition.IsInterface
        {
            get
            {
                CheckDefinitionInvariant();
                return this.TypeKind == TypeKind.Interface;
            }
        }

        bool Cci.ITypeDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }

        bool Cci.ITypeDefinition.IsSerializable
        {
            get
            {
                CheckDefinitionInvariant();
                return false; // this.IsSerializable;
            }
        }

        bool Cci.ITypeDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return false; // this.HasSpecialName;
            }
        }

        bool Cci.ITypeDefinition.IsWindowsRuntimeImport
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsWindowsRuntimeImport;
            }
        }

        bool Cci.ITypeDefinition.IsSealed
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsMetadataSealed;
            }
        }

        internal virtual bool IsMetadataSealed
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsSealed || this.IsStatic;
            }
        }

        IEnumerable<Cci.IMethodDefinition> Cci.ITypeDefinition.GetMethods(EmitContext context)
        {
            CheckDefinitionInvariant();

            foreach (var method in this.GetMethodsToEmit())
            {
                Debug.Assert((object)method != null);
                yield return (Cci.IMethodDefinition)method;
            }

            foreach (var m in ((PEModuleBuilder)context.Module).GetSynthesizedMethods(this))
            {
                yield return m;
            }
        }

        /// <summary>
        /// To represent a gap in interface's v-table null value should be returned in the appropriate position,
        /// unless the gap has a symbol (happens if it is declared in source, for example).
        /// </summary>
        internal virtual IEnumerable<IMethodSymbol> GetMethodsToEmit()
        {
            CheckDefinitionInvariant();

            foreach (var m in this.GetMembers())
            {
                if (m.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)m;

                    //if (method.IsPartialDefinition())
                    //{
                    //    // Don't emit partial methods without an implementation part.
                    //    if ((object)method.PartialImplementationPart == null)
                    //    {
                    //        continue;
                    //    }
                    //}
                    //// Don't emit the default value type constructor - the runtime handles that
                    //else if (method.IsDefaultValueTypeConstructor())
                    //{
                    //    continue;
                    //}

                    yield return method;
                }
            }
        }

        IEnumerable<Cci.INestedTypeDefinition> Cci.ITypeDefinition.GetNestedTypes(EmitContext context)
        {
            CheckDefinitionInvariant();

            foreach (NamedTypeSymbol type in this.GetTypeMembers()) // Ordered.
            {
                yield return type;
            }

            var generated = ((PEModuleBuilder)context.Module).GetSynthesizedTypes(this).Cast<Cci.INestedTypeDefinition>();
            foreach (var t in generated)
            {
                yield return t;
            }
        }

        IEnumerable<Cci.IPropertyDefinition> Cci.ITypeDefinition.GetProperties(EmitContext context)
        {
            CheckDefinitionInvariant();

            foreach (var property in this.GetPropertiesToEmit())
            {
                Debug.Assert((object)property != null);
                yield return (Cci.IPropertyDefinition)property;
            }

            IEnumerable<Cci.IPropertyDefinition> generated = ((PEModuleBuilder)context.Module).GetSynthesizedProperties(this);

            if (generated != null)
            {
                foreach (var p in generated)
                {
                    yield return p;
                }
            }
        }

        internal virtual IEnumerable<IPropertySymbol> GetPropertiesToEmit()
        {
            CheckDefinitionInvariant();

            foreach (var m in this.GetMembers())
            {
                if (m.Kind == SymbolKind.Property)
                {
                    yield return (IPropertySymbol)m;
                }
            }
        }

        bool Cci.ITypeDefinition.HasDeclarativeSecurity
        {
            get
            {
                CheckDefinitionInvariant();
                return false; // this.HasDeclarativeSecurity;
            }
        }

        IEnumerable<Cci.SecurityAttribute> Cci.ITypeDefinition.SecurityAttributes
        {
            get
            {
                CheckDefinitionInvariant();
                return /*this.GetSecurityInformation() ??*/ SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
            }
        }

        ushort Cci.ITypeDefinition.Alignment
        {
            get
            {
                CheckDefinitionInvariant();
                var layout = this.Layout;
                return (ushort)layout.Alignment;
            }
        }

        LayoutKind Cci.ITypeDefinition.Layout
        {
            get
            {
                CheckDefinitionInvariant();
                return this.Layout.Kind;
            }
        }

        uint Cci.ITypeDefinition.SizeOf
        {
            get
            {
                CheckDefinitionInvariant();
                return (uint)this.Layout.Size;
            }
        }

        CharSet Cci.ITypeDefinition.StringFormat
        {
            get
            {
                CheckDefinitionInvariant();

                return CharSet.Ansi; // this.MarshallingCharSet; //throw new System.NotImplementedException();
            }
        }

        ushort Cci.INamedTypeReference.GenericParameterCount
        {
            get { return GenericParameterCountImpl; }
        }

        bool Cci.INamedTypeReference.MangleName
        {
            get
            {
                return this.Arity != 0;
            }
        }

        string Cci.INamedEntity.Name
        {
            get
            {
                // We're using SourceTypeSymbol.MetadataName as Name (because changing the Name would cause too much refactoring for now);
                // In PHP we can have more classes with the same name in the same namespace,
                // so the compiler (SourceTypeSymbol) generates different MetadataName for it.
                string unsuffixedName = this is SourceTypeSymbol srct ? srct.MetadataName : this.Name;

                // CLR generally allows names with dots, however some APIs like IMetaDataImport
                // can only return full type names combined with namespaces. 
                // see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
                // When working with such APIs, names with dots become ambiguous since metadata 
                // consumer cannot figure where namespace ends and actual type name starts.
                // Therefore it is a good practice to avoid type names with dots.
                // Exception: The EE copies type names from metadata, which may contain dots already.
                Debug.Assert(this.IsErrorType() ||
                    !unsuffixedName.Contains(".") ||
                    this.OriginalDefinition is PENamedTypeSymbol, "type name contains dots: " + unsuffixedName);

                return unsuffixedName;
            }
        }

        Cci.IUnitReference Cci.INamespaceTypeReference.GetUnit(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.ITypeReference)this).AsNamespaceTypeReference != null);
            Debug.Assert(this.ContainingAssembly != null);
            return moduleBeingBuilt.Translate(this.ContainingAssembly, context.Diagnostics);
        }

        public virtual string NamespaceName
        {
            get
            {
                // INamespaceTypeReference is a type contained in a namespace
                // if this method is called for a nested type, we are in big trouble.
                Debug.Assert(((Cci.ITypeReference)this).AsNamespaceTypeReference != null);
                var ns = this.ContainingSymbol as INamespaceSymbol;
                if (ns != null)
                {
                    return ns.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat);
                }

                return string.Empty;
            }
        }

        bool Cci.INamespaceTypeDefinition.IsPublic
        {
            get
            {
                Debug.Assert((object)this.ContainingType == null && this.ContainingModule is SourceModuleSymbol);

                return PEModuleBuilder.MemberVisibility(this) == Cci.TypeMemberVisibility.Public;
            }
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(((Cci.ITypeReference)this).AsNestedTypeReference != null);

            Debug.Assert(this.IsDefinitionOrDistinct());

            if (!this.IsDefinition)
            {
                return moduleBeingBuilt.Translate(this.ContainingType,
                                                  syntaxNodeOpt: context.SyntaxNodeOpt,
                                                  diagnostics: context.Diagnostics);
            }

            return (Cci.ITypeReference)this.ContainingType;
        }

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                Debug.Assert((object)this.ContainingType != null);
                CheckDefinitionInvariant();

                return (Cci.ITypeDefinition)this.ContainingType;
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                Debug.Assert((object)this.ContainingType != null);
                CheckDefinitionInvariant();

                return PEModuleBuilder.MemberVisibility(this);
            }
        }

        ImmutableArray<Cci.ITypeReference> Cci.IGenericTypeInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var builder = ArrayBuilder<Microsoft.Cci.ITypeReference>.GetInstance();
            Debug.Assert(((Cci.ITypeReference)this).AsGenericTypeInstanceReference != null);

            var modifiers = default(ImmutableArray<ImmutableArray<CustomModifier>>);

            //if (this.HasTypeArgumentsCustomModifiers)
            //{
            //    modifiers = this.TypeArgumentsCustomModifiers;
            //}

            var arguments = this.TypeArguments; // .TypeArgumentsNoUseSiteDiagnostics;

            for (int i = 0; i < arguments.Length; i++)
            {
                var arg = moduleBeingBuilt.Translate(arguments[i], syntaxNodeOpt: context.SyntaxNodeOpt, diagnostics: context.Diagnostics);

                if (!modifiers.IsDefault && !modifiers[i].IsDefaultOrEmpty)
                {
                    arg = new Cci.ModifiedTypeReference(arg, modifiers[i].As<Cci.ICustomModifier>());
                }

                builder.Add(arg);
            }

            return builder.ToImmutableAndFree();
        }

        Cci.INamedTypeReference Cci.IGenericTypeInstanceReference.GetGenericType(EmitContext context)
        {
            Debug.Assert(((Cci.ITypeReference)this).AsGenericTypeInstanceReference != null);
            return GenericTypeImpl;
        }

        private Cci.INamedTypeReference GenericTypeImpl
        {
            get
            {
                return (Cci.INamedTypeReference)this.OriginalDefinition;
            }
        }

        Cci.INestedTypeReference Cci.ISpecializedNestedTypeReference.GetUnspecializedVersion(EmitContext context)
        {
            Debug.Assert(((Cci.ITypeReference)this).AsSpecializedNestedTypeReference != null);
            var result = GenericTypeImpl.AsNestedTypeReference;

            Debug.Assert(result != null);
            return result;
        }

        public ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics => TypeArguments;
    }
}
