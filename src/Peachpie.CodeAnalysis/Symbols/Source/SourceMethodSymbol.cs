using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.FlowAnalysis;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax;
using Microsoft.Cci;
using System.Threading;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP class method.
    /// </summary>
    internal partial class SourceMethodSymbol : SourceRoutineSymbol
    {
        readonly SourceTypeSymbol _type;
        readonly MethodDecl/*!*/_syntax;

        Microsoft.CodeAnalysis.Text.TextSpan NameSpan => _syntax.Name.Span.ToTextSpan();

        MethodSymbol _lazyOverridenMethod;

        public SourceMethodSymbol(SourceTypeSymbol/*!*/type, MethodDecl/*!*/syntax)
        {
            Contract.ThrowIfNull(type);
            Contract.ThrowIfNull(syntax);

            _type = type;
            _syntax = syntax;
        }

        internal override bool RequiresLateStaticBoundParam =>
            IsStatic &&                             // `static` in instance method == typeof($this)
            ControlFlowGraph != null &&             // cfg sets {Flags}
            (this.Flags & RoutineFlags.UsesLateStatic) != 0 &&
            (!_type.IsSealed || _type.IsTrait);     // `static` == `self` <=> self is sealed

        public override IMethodSymbol OverriddenMethod
        {
            get
            {
                if ((_commonflags & CommonFlags.OverriddenMethodResolved) == 0)
                {
                    Interlocked.CompareExchange(ref _lazyOverridenMethod, this.ResolveOverride(), null);

                    Debug.Assert(_lazyOverridenMethod != this);

                    _commonflags |= CommonFlags.OverriddenMethodResolved;
                }

                return _lazyOverridenMethod;
            }
        }

        internal override Signature SyntaxSignature => _syntax.Signature;

        internal override TypeRef SyntaxReturnType => _syntax.ReturnType;

        internal override AstNode Syntax => _syntax;

        internal override PHPDocBlock PHPDocBlock => _syntax.PHPDoc;

        internal override IList<Statement> Statements => _syntax.Body?.Statements;

        protected override TypeRefContext CreateTypeRefContext() => TypeRefFactory.CreateTypeRefContext(_type);

        public override void GetDiagnostics(DiagnosticBag diagnostic)
        {
            var name = _syntax.Name.Name;

            // diagnostics:
            if (name.Value.StartsWith("__", StringComparison.Ordinal))
            {
                // magic methods:
                if (name.IsConstructName) // __construct()
                {
                    if (IsStatic)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Errors.ConstructCannotBeStatic, _type.FullName.ToString()));
                    }
                    if (_syntax.ReturnType != null)
                    {
                        // {0} cannot declare a return type
                        diagnostic.Add(this, _syntax.ReturnType.Span.ToTextSpan(), Errors.ErrorCode.ERR_CannotDeclareReturnType, "Constructor " + _type.FullName.ToString(name, false));
                    }
                }
                else if (name.IsDestructName)    // __destruct()
                {
                    diagnostic.Add(this, NameSpan, Errors.ErrorCode.INF_DestructDiscouraged);

                    if (_syntax.Signature.FormalParams.Length != 0)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Errors.DestructCannotTakeArguments, _type.FullName.ToString()));
                    }
                    if (IsStatic)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Errors.DestructCannotBeStatic, _type.FullName.ToString()));
                    }
                    if (_syntax.ReturnType != null)
                    {
                        // {0} cannot declare a return type
                        diagnostic.Add(this, _syntax.ReturnType.Span.ToTextSpan(), Errors.ErrorCode.ERR_CannotDeclareReturnType, "Destructor " + _type.FullName.ToString(name, false));
                    }
                }
                else if (name.IsToStringName)   // __tostring()
                {
                    if ((IsStatic || (_syntax.Modifiers & PhpMemberAttributes.VisibilityMask) != PhpMemberAttributes.Public))
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Warnings.MagicMethodMustBePublicNonStatic, name.Value));
                    }

                    if (_syntax.Signature.FormalParams.Length != 0)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Errors.MethodCannotTakeArguments, _type.FullName.ToString(), name.Value));
                    }
                }
                else if (name.IsCloneName)  // __clone()
                {
                    if (IsStatic)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Errors.CloneCannotBeStatic, _type.FullName.ToString()));
                    }
                    if (_syntax.Signature.FormalParams.Length != 0)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Errors.CloneCannotTakeArguments, _type.FullName.ToString()));
                    }
                    if (_syntax.ReturnType != null)
                    {
                        // {0} cannot declare a return type
                        diagnostic.Add(this, _syntax.ReturnType.Span.ToTextSpan(), Errors.ErrorCode.ERR_CannotDeclareReturnType, _type.FullName.ToString(name, false));
                    }
                }
                else if (name.IsCallName) // __call($name, $args)
                {
                    if (IsStatic || (_syntax.Modifiers & PhpMemberAttributes.VisibilityMask) != PhpMemberAttributes.Public)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Warnings.MagicMethodMustBePublicNonStatic, name.Value));
                    }
                    if (_syntax.Signature.FormalParams.Length != 2)
                    {
                        diagnostic.Add(this, _syntax.Signature.Span.ToTextSpan(), Errors.ErrorCode.ERR_MustTakeArgs, "Method", _type.FullName.ToString(name, false), 2);
                    }
                }
                else if (name.IsCallStaticName) // __callstatic($name, $args)
                {
                    if (!IsStatic || (_syntax.Modifiers & PhpMemberAttributes.VisibilityMask) != PhpMemberAttributes.Public)
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Warnings.CallStatMustBePublicStatic));
                    }
                    if (_syntax.Signature.FormalParams.Length != 2)
                    {
                        diagnostic.Add(this, _syntax.Signature.Span.ToTextSpan(), Errors.ErrorCode.ERR_MustTakeArgs, "Method", _type.FullName.ToString(name, false), 2);
                    }
                }
                else if (name == Devsense.PHP.Syntax.Name.SpecialMethodNames.Set)   // __set($name, $value)
                {
                    if ((IsStatic || (_syntax.Modifiers & PhpMemberAttributes.VisibilityMask) != PhpMemberAttributes.Public))
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Warnings.MagicMethodMustBePublicNonStatic, name.Value));
                    }
                    if (_syntax.Signature.FormalParams.Length != 2)
                    {
                        diagnostic.Add(this, _syntax.Signature.Span.ToTextSpan(), Errors.ErrorCode.ERR_MustTakeArgs, "Method", _type.FullName.ToString(name, false), 2);
                    }
                }
                else if (name == Devsense.PHP.Syntax.Name.SpecialMethodNames.Get)   // __get($name)
                {
                    if ((IsStatic || (_syntax.Modifiers & PhpMemberAttributes.VisibilityMask) != PhpMemberAttributes.Public))
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Warnings.MagicMethodMustBePublicNonStatic, name.Value));
                    }
                    if (_syntax.Signature.FormalParams.Length != 1)
                    {
                        diagnostic.Add(this, _syntax.Signature.Span.ToTextSpan(), Errors.ErrorCode.ERR_MustTakeArgs, "Method", _type.FullName.ToString(name, false), 1);
                    }
                }
                else if (name == Devsense.PHP.Syntax.Name.SpecialMethodNames.Isset)   // __isset($name)
                {
                    if ((IsStatic || (_syntax.Modifiers & PhpMemberAttributes.VisibilityMask) != PhpMemberAttributes.Public))
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Warnings.MagicMethodMustBePublicNonStatic, name.Value));
                    }
                    if (_syntax.Signature.FormalParams.Length != 1)
                    {
                        diagnostic.Add(this, _syntax.Signature.Span.ToTextSpan(), Errors.ErrorCode.ERR_MustTakeArgs, "Method", _type.FullName.ToString(name, false), 1);
                    }
                }
                else if (name == Devsense.PHP.Syntax.Name.SpecialMethodNames.Unset)   // __unset($name)
                {
                    if ((IsStatic || (_syntax.Modifiers & PhpMemberAttributes.VisibilityMask) != PhpMemberAttributes.Public))
                    {
                        diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.ParametersSpan, Devsense.PHP.Errors.Warnings.MagicMethodMustBePublicNonStatic, name.Value));
                    }
                    if (_syntax.Signature.FormalParams.Length != 1)
                    {
                        diagnostic.Add(this, _syntax.Signature.Span.ToTextSpan(), Errors.ErrorCode.ERR_MustTakeArgs, "Method", _type.FullName.ToString(name, false), 1);
                    }
                }
                // ...
            }

            if (_syntax.Modifiers.IsAbstract())
            {
                // abstract member in non-abstract class
                if ((_type.Syntax.MemberAttributes & (PhpMemberAttributes.Abstract | PhpMemberAttributes.Trait)) == 0)  // not abstract nor trait
                {
                    // TODO: ERR_AbstractMethodInNonAbstractClass
                }

                // abstract private
                if ((_syntax.Modifiers & PhpMemberAttributes.VisibilityMask) == PhpMemberAttributes.Private)
                    diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.HeadingSpan, Devsense.PHP.Errors.Errors.AbstractPrivateMethodDeclared));

                // abstract final
                if ((_syntax.Modifiers & PhpMemberAttributes.Final) != 0)
                    diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.HeadingSpan, Devsense.PHP.Errors.Errors.AbstractFinalMethodDeclared));

                // abstract method with body
                if (_syntax.Body != null)
                    diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this,
                        _syntax.HeadingSpan,
                        _type.IsInterface
                            ? Devsense.PHP.Errors.Errors.InterfaceMethodWithBody
                            : Devsense.PHP.Errors.Errors.AbstractMethodWithBody,
                        _type.FullName.ToString(), name.Value));
            }
            else
            {
                if (_syntax.Body == null && !_type.IsInterface)
                {
                    diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(this, _syntax.HeadingSpan, Devsense.PHP.Errors.Errors.NonAbstractMethodWithoutBody,
                        _type.FullName.ToString(), name.Value));
                }
            }

            // overriden method name letter case mismatch:
            if (OverriddenMethod != null && Name != OverriddenMethod.Name && string.Equals(Name, OverriddenMethod.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                diagnostic.Add(this, NameSpan, Errors.ErrorCode.INF_OverrideNameCaseMismatch, Name, OverriddenMethod.Name);
            }

            //
            base.GetDiagnostics(diagnostic);
        }

        internal override SourceFileSymbol ContainingFile => _type.ContainingFile;

        public override string Name => _syntax.Name.Name.Value;

        public override Symbol ContainingSymbol => _type;

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                var accessibility = _syntax.Modifiers.GetAccessibility();                
                if (accessibility == Accessibility.Private && _syntax.Name.Name.IsConstructName)
                {
                    // workaround for `private` __construct()
                    // we have to be able to call __construct even from a derived class
                    accessibility = Accessibility.Protected;
                }

                //
                return accessibility;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                // magic methods:
                if (_syntax.Name.Name.Value.StartsWith("__", StringComparison.Ordinal))
                {
                    if (_syntax.Name.Name.IsToStringName)   // __tostring() : string
                    {
                        return DeclaringCompilation.CoreTypes.String;   // NOTE: we may need PhpString instead in some cases, consider once we implement PhpString as struct
                    }
                    //if (_syntax.Name.Name.IsConstructName)   // __construct() : void
                    //{
                    //    return DeclaringCompilation.CoreTypes.Void; // NOTE: in PHP we can return anything but it is reported as warning
                    //}
                    if (_syntax.Name.Name.IsCloneName) // __clone() : void
                    {
                        return DeclaringCompilation.CoreTypes.Void;
                    }
                }

                // default:
                return base.ReturnType;
            }
        }

        public override bool IsStatic => _syntax.Modifiers.IsStatic();

        public override bool IsAbstract => !IsStatic && (_syntax.Modifiers.IsAbstract() || _type.IsInterface);

        public override bool IsOverride => IsVirtual && this.OverriddenMethod != null && this.SignaturesMatch((MethodSymbol)this.OverriddenMethod);

        public override bool IsSealed => _syntax.Modifiers.IsSealed() && IsVirtual;

        public override bool IsVirtual
        {
            get
            {
                if (IsStatic || DeclaredAccessibility == Accessibility.Private)
                {
                    return false;
                }

                if (!IsAbstract)
                {
                    // __construct is not treated as virtual
                    if (_syntax.Name.Name.IsConstructName || _syntax.Name.Name == _type.FullName.Name)
                    {
                        // not called virtually
                        // its signature usually does not match the base
                        return false; // this.OverriddenMethod != null && this.SignaturesMatch((MethodSymbol)this.OverriddenMethod);
                    }
                }

                // in general, every method in PHP is virtual
                return true;
            }
        }
    }

    /// <summary>
    /// Represents a PHP trait method.
    /// </summary>
    internal class SourceTraitMethodSymbol : SourceMethodSymbol
    {
        public SourceTraitMethodSymbol(SourceTraitTypeSymbol type, MethodDecl syntax)
            : base(type, syntax)
        {
        }

        // abstract trait method must have an empty implementation
        public override bool IsAbstract => false;

        public override bool IsVirtual => false;
        public override bool IsOverride => false;
        internal override bool IsMetadataFinal => false; // final trait method must not be marked sealed in CIL

        // abstract trait method must have an empty implementation
        internal override IList<Statement> Statements => base.IsAbstract ? Array.Empty<Statement>() : base.Statements;
    }
}
