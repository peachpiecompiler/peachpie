using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Pchp.Syntax;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP function parameter.
    /// </summary>
    internal sealed class SourceParameterSymbol : ParameterSymbol
    {
        readonly SourceRoutineSymbol _routine;
        readonly FormalParam _syntax;
        readonly int _index;

        public SourceParameterSymbol(SourceRoutineSymbol routine, FormalParam syntax, int index)
        {
            _routine = routine;
            _syntax = syntax;
            _index = index;
        }

        public override Symbol ContainingSymbol => _routine;

        internal override IModuleSymbol ContainingModule => _routine.ContainingModule;

        public override INamedTypeSymbol ContainingType => _routine.ContainingType;

        public override string Name => _syntax.Name.Value;

        public override bool IsThis => _index == -1;

        public FormalParam Syntax => _syntax;

        public override ITypeSymbol Type
        {
            get
            {
                return (IsThis)
                    ? ContainingType // TODO: "?? AnyType" in case of $this in global scope
                    : DeclaringCompilation.GetTypeFromTypeRef(_routine, _routine.ControlFlowGraph.GetParamTypeMask(this), false);
            }
        }

        public override RefKind RefKind
        {
            get
            {
                if (_syntax.IsOut)
                    return RefKind.Out;

                return RefKind.None;
            }
        }

        public override bool IsParams => _syntax.IsVariadic;

        public override int Ordinal => _index;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ConstantValue ExplicitDefaultConstantValue => null;   // TODO
    }

    internal sealed class ThisParameterSymbol : ParameterSymbol
    {
        readonly SourceRoutineSymbol _routine;

        public ThisParameterSymbol(SourceRoutineSymbol routine)
        {
            _routine = routine;
        }

        public override Symbol ContainingSymbol => _routine;

        internal override IModuleSymbol ContainingModule => _routine.ContainingModule;

        public override INamedTypeSymbol ContainingType => _routine.ContainingType;

        public override string Name => VariableName.ThisVariableName.Value;

        public override bool IsThis => true;

        public override ITypeSymbol Type
        {
            get
            {
                return ContainingType; // TODO: "?? AnyType" in case of $this in global scope
            }
        }

        public override RefKind RefKind => RefKind.None;

        public override bool IsParams => false;

        public override int Ordinal => -1;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ConstantValue ExplicitDefaultConstantValue => null;
    }

}
