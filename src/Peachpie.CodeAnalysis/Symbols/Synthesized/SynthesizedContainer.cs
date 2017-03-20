using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// A container synthesized for a lambda, iterator method, async method, or dynamic-sites.
    /// </summary>
    internal abstract class SynthesizedContainer : NamedTypeSymbol
    {
        private readonly string _name;
        private readonly TypeMap _typeMap;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;

        protected SynthesizedContainer(string name, int parameterCount, bool returnsVoid)
        {
            Debug.Assert(name != null);
            _name = name;
            _typeMap = TypeMap.Empty;
            _typeParameters = CreateTypeParameters(parameterCount, returnsVoid);
        }

        protected SynthesizedContainer(string name, MethodSymbol topLevelMethod)
        {
            Debug.Assert(name != null);
            Debug.Assert(topLevelMethod != null);

            _name = name;
            _typeMap = TypeMap.Empty.WithAlphaRename(topLevelMethod, this, out _typeParameters);
        }

        protected SynthesizedContainer(string name, ImmutableArray<TypeParameterSymbol> typeParameters, TypeMap typeMap)
        {
            Debug.Assert(name != null);
            Debug.Assert(!typeParameters.IsDefault);
            Debug.Assert(typeMap != null);

            _name = name;
            _typeParameters = typeParameters;
            _typeMap = typeMap;
        }

        private ImmutableArray<TypeParameterSymbol> CreateTypeParameters(int parameterCount, bool returnsVoid)
        {
            var typeParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance(parameterCount + (returnsVoid ? 0 : 1));
            if (parameterCount != 0)
            {
                throw new NotImplementedException();
                //for (int i = 0; i < parameterCount; i++)
                //{
                //    typeParameters.Add(new AnonymousTypeManager.AnonymousTypeParameterSymbol(this, i, "T" + (i + 1)));
                //}
            }

            if (!returnsVoid)
            {
                //typeParameters.Add(new AnonymousTypeManager.AnonymousTypeParameterSymbol(this, parameterCount, "TResult"));
                throw new NotImplementedException();
            }

            return typeParameters.ToImmutableAndFree();
        }

        internal TypeMap TypeMap
        {
            get { return _typeMap; }
        }

        internal virtual MethodSymbol Constructor
        {
            get { return null; }
        }

        internal sealed override bool IsInterface
        {
            get { return this.TypeKind == TypeKind.Interface; }
        }

        //internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        //{
        //    base.AddSynthesizedAttributes(compilationState, ref attributes);

        //    if (ContainingSymbol.Kind == SymbolKind.NamedType && ContainingSymbol.IsImplicitlyDeclared)
        //    {
        //        return;
        //    }

        //    var compilation = ContainingSymbol.DeclaringCompilation;

        //    // this can only happen if frame is not nested in a source type/namespace (so far we do not do this)
        //    // if this happens for whatever reason, we do not need "CompilerGenerated" anyways
        //    Debug.Assert(compilation != null, "SynthesizedClass is not contained in a source module?");

        //    AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(
        //        WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        //}

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public sealed override string Name
        {
            get { return _name; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        public override IEnumerable<string> MemberNames
        {
            get { return SpecializedCollections.EmptyEnumerable<string>(); }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get { return this; }
        }

        public override bool IsSealed
        {
            get { return true; }
        }

        public override bool IsAbstract
        {
            get { return (object)Constructor == null; }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get { return StaticCast<TypeSymbol>.From(TypeParameters); }
        }

        internal override bool HasTypeArgumentsCustomModifiers => false;

        public override ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal) => GetEmptyTypeArgumentCustomModifiers(ordinal);

        public override ImmutableArray<Symbol> GetMembers()
        {
            Symbol constructor = this.Constructor;
            return (object)constructor == null ? ImmutableArray<Symbol>.Empty : ImmutableArray.Create(constructor);
        }

        public override ImmutableArray<Symbol> GetMembers(string name, bool ignoreCase = false)
        {
            var ctor = Constructor;
            return ((object)ctor != null && name == ctor.Name) ? ImmutableArray.Create<Symbol>(ctor) : ImmutableArray<Symbol>.Empty;
        }

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            foreach (var m in this.GetMembers())
            {
                switch (m.Kind)
                {
                    case SymbolKind.Field:
                        yield return (FieldSymbol)m;
                        break;
                }
            }
        }

        //internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        //{
        //    return this.GetMembersUnordered();
        //}

        //internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        //{
        //    return this.GetMembers(name);
        //}

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Private; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override ImmutableArray<NamedTypeSymbol> Interfaces
        {
            get
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
        }

        //internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        //{
        //    return ImmutableArray<NamedTypeSymbol>.Empty;
        //}

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw new NotImplementedException();
        }

        //internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        //{
        //    return CalculateInterfacesToEmit();
        //}

        public override NamedTypeSymbol BaseType //NoUseSiteDiagnostics
        {
            get { return ContainingAssembly.GetSpecialType(this.TypeKind == TypeKind.Struct ? SpecialType.System_ValueType : SpecialType.System_Object); }
        }

        //internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        //{
        //    return BaseTypeNoUseSiteDiagnostics;
        //}

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return Interfaces; // NoUseSiteDiagnostics(basesBeingResolved);
        }

        public override bool MightContainExtensionMethods
        {
            get { return false; }
        }

        public override int Arity
        {
            get { return TypeParameters.Length; }
        }

        internal override bool MangleName
        {
            get { return Arity > 0; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return false; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get { return false; }
        }

        //internal override bool IsComImport
        //{
        //    get { return false; }
        //}

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        //internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        //{
        //    return ImmutableArray<string>.Empty;
        //}

        //internal override bool HasDeclarativeSecurity
        //{
        //    get { return false; }
        //}

        //internal override CharSet MarshallingCharSet
        //{
        //    get { return DefaultMarshallingCharSet; }
        //}

        //internal override bool IsSerializable
        //{
        //    get { return false; }
        //}

        //internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        //{
        //    throw ExceptionUtilities.Unreachable;
        //}

        //internal override AttributeUsageInfo GetAttributeUsageInfo()
        //{
        //    return default(AttributeUsageInfo);
        //}

        internal override TypeLayout Layout
        {
            get { return default(TypeLayout); }
        }

        //internal override bool HasSpecialName
        //{
        //    get { return false; }
        //}
    }
}
