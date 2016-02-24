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
    public enum LocalKind
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
        /// Variable is passed from caller routine.
        /// </summary>
        UseParameter,

        /// <summary>
        /// Variable is <c>$this</c> variable.
        /// </summary>
        ThisVariable,

        /// <summary>
        /// Variable was introduced with <c>static</c> declaration.
        /// </summary>
        StaticVariable,

        /// <summary>
        /// Symbols represents a return value.
        /// </summary>
        ReturnVariable,
    }

    internal class SourceLocalSymbol : Symbol, ILocalSymbol
    {
        readonly SourceRoutineSymbol _routine;
        readonly string _name;
        readonly LocalKind _kind;

        public SourceLocalSymbol(SourceRoutineSymbol routine, string name, LocalKind kind)
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
        public LocalKind LocalKind => _kind;

        public override void Accept(SymbolVisitor visitor)
            => visitor.VisitLocal(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitLocal(this);

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

        public override bool IsStatic => _kind == LocalKind.StaticVariable;

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

        public virtual ITypeSymbol Type => null;

        #endregion
    }

    internal class SourceReturnSymbol : SourceLocalSymbol
    {
        internal const string SpecialName = "<return>";

        public SourceReturnSymbol(SourceRoutineSymbol routine)
            :base(routine, SpecialName, LocalKind.ReturnVariable)
        {

        }
    }
}
