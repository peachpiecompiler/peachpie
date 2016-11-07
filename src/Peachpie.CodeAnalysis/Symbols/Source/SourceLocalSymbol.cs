using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Diagnostics;

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
    }

    internal class SourceLocalSymbol : Symbol, ILocalSymbol, ILocalSymbolInternal
    {
        readonly protected SourceRoutineSymbol _routine;
        readonly string _name;
        readonly VariableKind _kind;

        public SourceLocalSymbol(SourceRoutineSymbol routine, string name, VariableKind kind)
        {
            Debug.Assert(routine != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            _routine = routine;
            _name = name;
            _kind = kind;
        }

        #region Symbol

        public override string Name => _name;

        /// <summary>
        /// Gets local kind.
        /// </summary>
        public VariableKind LocalKind => _kind;

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

        public override bool IsStatic => _kind == VariableKind.StaticVariable;

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

        public virtual ITypeSymbol Type => DeclaringCompilation.GetTypeFromTypeRef(_routine, _routine.ControlFlowGraph.GetLocalTypeMask(this.Name));

        public bool IsImportedFromMetadata => false;

        public SynthesizedLocalKind SynthesizedKind => SynthesizedLocalKind.UserDefined;

        #endregion
    }

    internal class SynthesizedLocalSymbol : SourceLocalSymbol
    {
        readonly TypeSymbol _type;

        public SynthesizedLocalSymbol(SourceRoutineSymbol routine, string name, TypeSymbol type)
            :base(routine, name + "#", VariableKind.LocalVariable)
        {
            Contract.ThrowIfNull(type);
            _type = type;
        }

        public override ITypeSymbol Type => _type;
    }
}
