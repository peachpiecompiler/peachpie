using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a simple compiler generated parameter of a given type.
    /// </summary>
    internal class SynthesizedParameterSymbol : ParameterSymbol
    {
        private readonly MethodSymbol _container;
        private readonly TypeSymbol _type;
        private readonly string _name;
        private readonly bool _isParams;
        private readonly ImmutableArray<CustomModifier> _customModifiers;
        private readonly ushort _countOfCustomModifiersPrecedingByRef;
        private readonly RefKind _refKind;
        private readonly ConstantValue _explicitDefaultConstantValue;

        private int _ordinal;

        public SynthesizedParameterSymbol(
            MethodSymbol container,
            TypeSymbol type,
            int ordinal,
            RefKind refKind,
            string name = "",
            bool isParams = false,
            ImmutableArray<CustomModifier> customModifiers = default(ImmutableArray<CustomModifier>),
            ushort countOfCustomModifiersPrecedingByRef = 0,
            ConstantValue explicitDefaultConstantValue = null)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(name != null);
            Debug.Assert(ordinal >= 0);

            _container = container;
            _type = type;
            _ordinal = ordinal;
            _refKind = refKind;
            _name = name;
            _isParams = isParams;
            _customModifiers = customModifiers.NullToEmpty();
            _countOfCustomModifiersPrecedingByRef = countOfCustomModifiersPrecedingByRef;
            _explicitDefaultConstantValue = explicitDefaultConstantValue;
        }

        internal override TypeSymbol Type
        {
            get { return _type; }
        }

        public override RefKind RefKind
        {
            get { return _refKind; }
        }

        //internal override bool IsMetadataIn
        //{
        //    get { return false; }
        //}

        //internal override bool IsMetadataOut
        //{
        //    get { return _refKind == RefKind.Out; }
        //}

        //internal override MarshalPseudoCustomAttributeData MarshallingInformation
        //{
        //    get { return null; }
        //}

        public override string Name
        {
            get { return _name; }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get { return _customModifiers; }
        }

        internal override IEnumerable<AttributeData> GetCustomAttributesToEmit(CommonModuleCompilationState compilationState)
        {
            if (IsParams)
            {
                yield return new SynthesizedAttributeData(
                    (MethodSymbol)DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_ParamArrayAttribute__ctor),
                    ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            yield break;
        }

        public override int Ordinal => _ordinal;
        internal void UpdateOrdinal(int newordinal) { _ordinal = newordinal; }

        public override bool IsParams => _isParams;

        //internal override bool IsMetadataOptional
        //{
        //    get { return false; }
        //}

        public override bool IsImplicitlyDeclared
        {
            get { return SpecialParameterSymbol.IsContextParameter(this) || this.IsParams || base.IsImplicitlyDeclared; }
        }

        internal override ConstantValue ExplicitDefaultConstantValue => _explicitDefaultConstantValue;

        public override bool IsOptional => _explicitDefaultConstantValue != null;

        //internal override bool IsIDispatchConstant
        //{
        //    get { return false; }
        //}

        //internal override bool IsIUnknownConstant
        //{
        //    get { return false; }
        //}

        //internal override bool IsCallerLineNumber
        //{
        //    get { return false; }
        //}

        //internal override bool IsCallerFilePath
        //{
        //    get { return false; }
        //}

        //internal override bool IsCallerMemberName
        //{
        //    get { return false; }
        //}

        public sealed override ushort CountOfCustomModifiersPrecedingByRef
        {
            get { return _countOfCustomModifiersPrecedingByRef; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        //internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        //{
        //    // Emit [Dynamic] on synthesized parameter symbols when the original parameter was dynamic 
        //    // in order to facilitate debugging.  In the case the necessary attributes are missing 
        //    // this is a no-op.  Emitting an error here, or when the original parameter was bound, would
        //    // adversely effect the compilation or potentially change overload resolution.  
        //    var compilation = this.DeclaringCompilation;
        //    if (Type.ContainsDynamic() && compilation.HasDynamicEmitAttributes())
        //    {
        //        var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
        //        var diagnostic = boolType.GetUseSiteDiagnostic();
        //        if ((diagnostic == null) || (diagnostic.Severity != DiagnosticSeverity.Error))
        //        {
        //            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.Type, this.CustomModifiers.Length, this.RefKind));
        //        }
        //    }
        //}

        /// <summary>
        /// For each parameter of a source method, construct a corresponding synthesized parameter
        /// for a destination method.
        /// </summary>
        /// <param name="sourceMethod">Has parameters.</param>
        /// <param name="destinationMethod">Needs parameters.</param>
        /// <returns>Synthesized parameters to add to destination method.</returns>
        internal static ImmutableArray<ParameterSymbol> DeriveParameters(MethodSymbol sourceMethod, MethodSymbol destinationMethod)
        {
            var builder = ArrayBuilder<ParameterSymbol>.GetInstance();

            foreach (var oldParam in sourceMethod.Parameters)
            {
                //same properties as the old one, just change the owner
                builder.Add(new SynthesizedParameterSymbol(destinationMethod, oldParam.Type, oldParam.Ordinal,
                    oldParam.RefKind, oldParam.Name, false, oldParam.CustomModifiers, oldParam.CountOfCustomModifiersPrecedingByRef));
            }

            return builder.ToImmutableAndFree();
        }
    }
}
