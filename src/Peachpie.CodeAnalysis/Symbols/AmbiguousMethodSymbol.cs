using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics.Graph;

namespace Pchp.CodeAnalysis.Symbols
{
    public enum ErrorMethodKind
    {
        None = 0,

        Ambiguous,
        Inaccessible,
        Missing,

        /// <summary>
        /// Method is actually a call to a magic method <c>__callStatic</c> or <c>__call</c>.
        /// </summary>
        MagicCall,
    }

    public interface IErrorMethodSymbol : IMethodSymbol
    {
        ErrorMethodKind ErrorKind { get; }

        /// <summary>
        /// In case of an original method(s) causing error (not visible or ambiguous), gets their enumeration.
        /// </summary>
        ImmutableArray<IMethodSymbol> OriginalSymbols { get; }
    }

    internal abstract class ErrorMethodSymbol : MethodSymbol, IErrorMethodSymbol
    {
        public abstract ErrorMethodKind ErrorKind { get; }

        public virtual ImmutableArray<IMethodSymbol> OriginalSymbols => ImmutableArray<IMethodSymbol>.Empty;

        public override RefKind RefKind => RefKind.None;

        public override bool IsExtern => false;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        public override bool IsImplicitlyDeclared => true;
    }

    internal class AmbiguousMethodSymbol : ErrorMethodSymbol
    {
        public ImmutableArray<MethodSymbol> Ambiguities => _ambiguities;
        readonly ImmutableArray<MethodSymbol> _ambiguities;

        /// <summary>
        /// Gets value indicating the ambiguities are overloads of the same function.
        /// Otherwise <see cref="Ambiguities"/> are different methods and only one of them is declared in runtime.
        /// </summary>
        public bool IsOverloadable => _overloadable;
        readonly bool _overloadable;

        public override ErrorMethodKind ErrorKind => ErrorMethodKind.Ambiguous;

        public override ImmutableArray<IMethodSymbol> OriginalSymbols => _ambiguities.CastArray<IMethodSymbol>();

        public override string Name => _ambiguities[0].Name;

        /// <summary>
        /// Method symbol representing more overloads.
        /// </summary>
        /// <param name="ambiguities">List of ambigous symbols.</param>
        /// <param name="overloadable">
        /// Whether the ambiguities can be resolved by overload resolution (list of method overloads or library method overloads).
        /// Otherwise binding has to be postponed to runtime (source declares more functions with the same name).</param>
        public AmbiguousMethodSymbol(ImmutableArray<MethodSymbol> ambiguities, bool overloadable)
        {
            Debug.Assert(!ambiguities.IsDefaultOrEmpty);
            _ambiguities = ambiguities;
            _overloadable = overloadable;
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                Symbol result = null;
                foreach (var a in _ambiguities)
                {
                    if (result == null || result == a.ContainingType) result = a.ContainingType;
                    else return null;
                }
                return result;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                Accessibility result = 0;
                foreach (var a in _ambiguities) result |= a.DeclaredAccessibility;
                return result;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _ambiguities.SelectMany(a => a.DeclaringSyntaxReferences).ToImmutableArray();
            }
        }

        public override bool IsAbstract => _ambiguities.Any(a => a.IsAbstract);

        public override bool IsOverride => _ambiguities.Any(a => a.IsOverride);

        public override bool IsSealed => _ambiguities.Any(a => a.IsSealed);

        public override bool IsStatic => _ambiguities.Any(a => a.IsStatic);

        public override bool IsVirtual => _ambiguities.Any(a => a.IsVirtual);

        public override ImmutableArray<Location> Locations => _ambiguities.SelectMany(a => a.Locations).AsImmutable();

        public override MethodKind MethodKind => _ambiguities[0].MethodKind;

        public override bool ReturnsVoid => _ambiguities.Any(a => a.ReturnsVoid);

        public override RefKind RefKind => _ambiguities[0].RefKind;

        public override TypeSymbol ReturnType => _ambiguities[0].ReturnType;
    }

    internal sealed class InaccessibleMethodSymbol : AmbiguousMethodSymbol
    {
        public override ErrorMethodKind ErrorKind
        {
            get { return ErrorMethodKind.Inaccessible; }
        }

        public InaccessibleMethodSymbol(ImmutableArray<MethodSymbol> ambiguities) : base(ambiguities, false)
        {
        }
    }

    internal sealed class MagicCallMethodSymbol : ErrorMethodSymbol
    {
        public MethodSymbol RealMethod { get; }

        public string OriginalMethodName { get; }

        public override ErrorMethodKind ErrorKind => ErrorMethodKind.MagicCall;

        public override string Name => RealMethod.Name;

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override bool ReturnsVoid => RealMethod.ReturnsVoid;

        public override TypeSymbol ReturnType => RealMethod.ReturnType;

        public override Symbol ContainingSymbol => RealMethod.ContainingSymbol;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => RealMethod.DeclaringSyntaxReferences;

        public override Accessibility DeclaredAccessibility => RealMethod.DeclaredAccessibility;

        public override bool IsStatic => RealMethod.IsStatic;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        public MagicCallMethodSymbol(string originalMethodName, MethodSymbol realSymbol)
        {
            OriginalMethodName = originalMethodName ?? throw new ArgumentNullException(nameof(originalMethodName));
            RealMethod = realSymbol ?? throw new ArgumentNullException(nameof(realSymbol));
        }
    }

    internal sealed class MissingMethodSymbol : ErrorMethodSymbol
    {
        readonly string _name;

        public override ErrorMethodKind ErrorKind
        {
            get { return ErrorMethodKind.Missing; }

        }

        public override string Name => _name;

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override bool ReturnsVoid => true;

        public override TypeSymbol ReturnType => new MissingMetadataTypeSymbol(string.Empty, 0, false);

        public override Symbol ContainingSymbol => null;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        public MissingMethodSymbol(string name = "")
        {
            _name = name;
        }
    }
}
