using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Peachpie.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract class WrappedParameterSymbol : ParameterSymbol
    {
        protected readonly ParameterSymbol underlyingParameter;

        protected WrappedParameterSymbol(ParameterSymbol underlyingParameter)
        {
            Debug.Assert((object)underlyingParameter != null);

            this.underlyingParameter = underlyingParameter;
        }

        public abstract override Symbol ContainingSymbol
        {
            get;
        }

        protected override Symbol OriginalSymbolDefinition => this;

        public sealed override bool Equals(object obj)
        {
            if ((object)this == obj)
            {
                return true;
            }

            // Equality of ordinal and containing symbol is a correct
            // implementation for all ParameterSymbols, but we don't 
            // define it on the base type because most can simply use
            // ReferenceEquals.

            var other = obj as WrappedParameterSymbol;
            return (object)other != null &&
                this.Ordinal == other.Ordinal &&
                this.ContainingSymbol.Equals(other.ContainingSymbol);
        }

        public sealed override int GetHashCode()
        {
            return Hash.Combine(ContainingSymbol, underlyingParameter.Ordinal);
        }

        #region Forwarded

        public override BoundExpression Initializer => underlyingParameter.Initializer;

        public override FieldSymbol DefaultValueField => underlyingParameter.DefaultValueField;

        internal override TypeSymbol Type => underlyingParameter.Type;

        public sealed override RefKind RefKind => underlyingParameter.RefKind;

        //internal sealed override bool IsMetadataIn
        //{
        //    get { return underlyingParameter.IsMetadataIn; }
        //}

        //internal sealed override bool IsMetadataOut
        //{
        //    get { return underlyingParameter.IsMetadataOut; }
        //}

        public sealed override ImmutableArray<Location> Locations
        {
            get { return underlyingParameter.Locations; }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return underlyingParameter.DeclaringSyntaxReferences; }
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            return underlyingParameter.GetAttributes();
        }

        //internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        //{
        //    underlyingParameter.AddSynthesizedAttributes(compilationState, ref attributes);
        //}

        internal sealed override ConstantValue ExplicitDefaultConstantValue
        {
            get { return underlyingParameter.ExplicitDefaultConstantValue; }
        }

        public override int Ordinal
        {
            get { return underlyingParameter.Ordinal; }
        }

        public override bool IsParams
        {
            get { return underlyingParameter.IsParams; }
        }

        //internal override bool IsMetadataOptional
        //{
        //    get { return underlyingParameter.IsMetadataOptional; }
        //}

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingParameter.IsImplicitlyDeclared; }
        }

        public sealed override string Name
        {
            get { return underlyingParameter.Name; }
        }

        public sealed override string MetadataName
        {
            get { return underlyingParameter.MetadataName; }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get { return underlyingParameter.CustomModifiers; }
        }

        internal override ImportValueAttributeData ImportValueAttributeData => underlyingParameter.ImportValueAttributeData;

        public override bool HasNotNull => underlyingParameter.HasNotNull;

        public override bool IsPhpRw => underlyingParameter.IsPhpRw;

        //internal override MarshalPseudoCustomAttributeData MarshallingInformation
        //{
        //    get { return underlyingParameter.MarshallingInformation; }
        //}

        //internal override UnmanagedType MarshallingType
        //{
        //    get { return underlyingParameter.MarshallingType; }
        //}

        //internal override bool IsIDispatchConstant
        //{
        //    get { return underlyingParameter.IsIDispatchConstant; }
        //}

        //internal override bool IsIUnknownConstant
        //{
        //    get { return underlyingParameter.IsIUnknownConstant; }
        //}

        //internal override bool IsCallerLineNumber
        //{
        //    get { return underlyingParameter.IsCallerLineNumber; }
        //}

        //internal override bool IsCallerFilePath
        //{
        //    get { return underlyingParameter.IsCallerFilePath; }
        //}

        //internal override bool IsCallerMemberName
        //{
        //    get { return underlyingParameter.IsCallerMemberName; }
        //}

        //internal sealed override ushort CountOfCustomModifiersPrecedingByRef
        //{
        //    get { return underlyingParameter.CountOfCustomModifiersPrecedingByRef; }
        //}

        #endregion
    }
}
