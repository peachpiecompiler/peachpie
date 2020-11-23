using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Variable kind.
    /// </summary>
    public enum VariableKind
    {
        /// <summary>
        /// Variable is local in the routine.
        /// </summary>
        LocalVariable,

        /// <summary>
        /// Variable is reference to a global variable.
        /// </summary>
        GlobalVariable,

        /// <summary>
        /// Variable refers to a routine parameter.
        /// </summary>
        Parameter,

        /// <summary>
        /// Variable is <c>$this</c> variable.
        /// </summary>
        ThisParameter,

        /// <summary>
        /// Variable was introduced with <c>static</c> declaration.
        /// </summary>
        StaticVariable,

        /// <summary>
        /// Variable is a local synthesized variable, must be indirect.
        /// </summary>
        LocalTemporalVariable,
    }

    internal class SourceLocalSymbol : Symbol, ILocalSymbol, ILocalSymbolInternal
    {
        readonly protected SourceRoutineSymbol _routine;
        readonly string _name;

        public SourceLocalSymbol(SourceRoutineSymbol routine, string name, TextSpan span)
        {
            Debug.Assert(routine != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            _routine = routine;
            _name = name;
        }

        #region Symbol

        public override string Name => _name;

        public override void Accept(SymbolVisitor visitor)
            => visitor.VisitLocal(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitLocal(this);

        public SyntaxNode GetDeclaratorSyntax() => null;

        public override Symbol ContainingSymbol => _routine;

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsExtern => false;

        public override bool IsOverride => false;

        public override bool IsSealed => true;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override SymbolKind Kind => SymbolKind.Local;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        #endregion

        #region ILocalSymbol

        public object ConstantValue => null;

        public bool HasConstantValue => false;

        public bool IsConst => false;

        public bool IsFunctionValue => false;

        public virtual ITypeSymbol Type
        {
            get
            {
                var tsymbol = DeclaringCompilation.GetTypeFromTypeRef(_routine, _routine.ControlFlowGraph.GetLocalTypeMask(this.Name));
                if (tsymbol.SpecialType == SpecialType.System_Void)
                {
                    tsymbol = DeclaringCompilation.CoreTypes.PhpValue;  // temporary workaround for uninitialized variables
                }
                Debug.Assert(tsymbol.IsValidType());
                return tsymbol;
            }
        }

        public bool IsImportedFromMetadata => false;

        public SynthesizedLocalKind SynthesizedKind => SynthesizedLocalKind.UserDefined;

        public virtual bool IsRef => false;

        public virtual RefKind RefKind => RefKind.None;

        public virtual bool IsFixed => false;

        NullableAnnotation ILocalSymbol.NullableAnnotation => NullableAnnotation.None;

        #endregion
    }

    internal class SynthesizedLocalSymbol : SourceLocalSymbol
    {
        readonly TypeSymbol _type;

        public SynthesizedLocalSymbol(SourceRoutineSymbol routine, string name, TypeSymbol type)
            : base(routine, name + "'", default(TextSpan))
        {
            Contract.ThrowIfNull(type);
            _type = type;
        }

        public override ITypeSymbol Type => _type;
    }
}
