using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    sealed class SourceCustomAttribute : BaseAttributeData
    {
        readonly ClassTypeRef _tref;
        readonly ImmutableArray<LangElement> _arguments;
        readonly ImmutableArray<KeyValuePair<Name, LangElement>> _properties;

        NamedTypeSymbol _type;
        MethodSymbol _ctor;
        ImmutableArray<TypedConstant> _ctorArgs;
        ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArgs;

        public SourceCustomAttribute(ClassTypeRef tref, IList<KeyValuePair<Name, LangElement>> arguments)
        {
            _tref = tref;

            if (arguments != null && arguments.Count != 0)
            {
                // count args
                int nargs = 0;
                while (nargs < arguments.Count && arguments[nargs].Key.Value == null)
                    nargs++;

                //
                _arguments = arguments.Take(nargs).Select(x => x.Value).AsImmutable();
                _properties = arguments.Skip(nargs).AsImmutable();
            }
            else
            {
                _arguments = ImmutableArray<LangElement>.Empty;
                _properties = ImmutableArray<KeyValuePair<Name, LangElement>>.Empty;
            }
        }

        internal void Bind(Symbol symbol)
        {
            Debug.Assert(symbol != null);

            if (_type == null)
            {
                var type = (NamedTypeSymbol)symbol.DeclaringCompilation.GlobalSemantics.ResolveType(_tref.ClassName)
                    ?? new MissingMetadataTypeSymbol(_tref.ClassName.ClrName(), 0, false);

                // bind arguments
                TryResolveCtor(type, symbol.DeclaringCompilation, out _ctor, out _ctorArgs);

                // bind named parameters
                if (type.IsErrorTypeOrNull() || _properties.IsDefaultOrEmpty)
                {
                    _namedArgs = ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;
                }
                else
                {
                    var namedArgs = new KeyValuePair<string, TypedConstant>[_properties.Length];
                    for (int i = 0; i < namedArgs.Length; i++)
                    {
                        var prop = _properties[i];
                        var member =
                            (Symbol)type.LookupMember<PropertySymbol>(prop.Key.Value) ??
                            (Symbol)type.LookupMember<FieldSymbol>(prop.Key.Value);

                        if (member != null && TryBindTypedConstant(member.GetTypeOrReturnType(), prop.Value, symbol.DeclaringCompilation, out var arg))
                        {
                            namedArgs[i] = new KeyValuePair<string, TypedConstant>(prop.Key.Value, arg);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    _namedArgs = namedArgs.AsImmutable();
                }

                //
                _type = type;
            }
        }

        static bool TryBindTypedConstant(TypeSymbol target, LangElement element, PhpCompilation compilation, out TypedConstant result)
        {
            result = default;

            if (element is LongIntLiteral llit)
            {
                switch (target.SpecialType)
                {
                    case SpecialType.System_Int32:
                        result = new TypedConstant(compilation.CoreTypes.Int32.Symbol, TypedConstantKind.Primitive, (int)llit.Value);
                        return true;
                    case SpecialType.System_Int64:
                        result = new TypedConstant(compilation.CoreTypes.Long.Symbol, TypedConstantKind.Primitive, llit.Value);
                        return true;
                    case SpecialType.System_UInt32:
                        result = new TypedConstant(compilation.GetSpecialType(SpecialType.System_UInt32), TypedConstantKind.Primitive, (uint)llit.Value);
                        return true;
                    case SpecialType.System_UInt64:
                        result = new TypedConstant(compilation.GetSpecialType(SpecialType.System_UInt64), TypedConstantKind.Primitive, (ulong)llit.Value);
                        return true;
                    case SpecialType.System_Double:
                        result = new TypedConstant(compilation.GetSpecialType(SpecialType.System_Double), TypedConstantKind.Primitive, (double)llit.Value);
                        return true;
                    case SpecialType.System_Single:
                        result = new TypedConstant(compilation.GetSpecialType(SpecialType.System_Single), TypedConstantKind.Primitive, (float)llit.Value);
                        return true;
                    default:
                        return false;
                }
            }

            if (element is DoubleLiteral dlit)
            {
                switch (target.SpecialType)
                {
                    case SpecialType.System_Double:
                        result = new TypedConstant(compilation.CoreTypes.Double.Symbol, TypedConstantKind.Primitive, dlit.Value);
                        return true;
                    case SpecialType.System_Single:
                        result = new TypedConstant(compilation.GetSpecialType(SpecialType.System_Single), TypedConstantKind.Primitive, (float)dlit.Value);
                        return true;
                    default:
                        return false;
                }
            }

            if (element is StringLiteral slit)
            {
                switch (target.SpecialType)
                {
                    case SpecialType.System_String:
                        result = new TypedConstant(compilation.CoreTypes.String.Symbol, TypedConstantKind.Primitive, slit.Value);
                        return true;
                    case SpecialType.System_Char:
                        if (slit.Value.Length != 1) goto default;
                        result = new TypedConstant(compilation.GetSpecialType(SpecialType.System_Char), TypedConstantKind.Primitive, slit.Value[0]);
                        return true;
                    default:
                        return false;
                }
            }

            if (element is TypeRef tref)
            {
                result = new TypedConstant(compilation.GetWellKnownType(WellKnownType.System_Type), TypedConstantKind.Type, compilation.GetTypeFromTypeRef(tref));
                return target == compilation.GetWellKnownType(WellKnownType.System_Type);
            }

            if (element is GlobalConstUse gconst)
            {
                //
            }

            if (element is ClassConstUse cconst)
            {
                // 
            }

            //
            return false;
        }

        bool TryResolveCtor(NamedTypeSymbol type, PhpCompilation compilation, out MethodSymbol ctor, out ImmutableArray<TypedConstant> args)
        {
            if (type.IsValidType())
            {
                var candidates = type.InstanceConstructors;
                for (int i = 0; i < candidates.Length; i++)
                {
                    var m = candidates[i];

                    if (m.DeclaredAccessibility != Accessibility.Public) continue; // TODO: or current class context
                    if (m.IsGenericMethod) { Debug.Fail("unexpected"); continue; } // NS
                    if (m.ParameterCount < _arguments.Length) continue; // be strict

                    var match = true;
                    var ps = m.Parameters;
                    var boundargs = new TypedConstant[ps.Length];

                    for (var pi = 0; match && pi < ps.Length; pi++)
                    {
                        if (pi >= _arguments.Length)
                        {
                            //if (ps[pi].IsOptional)
                            //{
                            //    boundargs[pi] = ps[pi].ExplicitDefaultConstantValue.AsTypedConstant();
                            //    continue; // ok
                            //}
                            //else
                            {
                                match = false;
                                break;
                            }
                        }

                        if (TryBindTypedConstant(ps[pi].Type, _arguments[pi], compilation, out var arg))
                        {
                            boundargs[pi] = arg;
                        }
                        else
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        ctor = m;
                        args = boundargs.AsImmutable();
                        return true;
                    }
                }
            }

            //
            ctor = new MissingMethodSymbol();
            args = ImmutableArray<TypedConstant>.Empty;
            return false;
        }

        public override NamedTypeSymbol AttributeClass => _type ?? throw ExceptionUtilities.Unreachable;

        public override MethodSymbol AttributeConstructor => _ctor ?? throw ExceptionUtilities.Unreachable;

        public override SyntaxReference ApplicationSyntaxReference => throw new NotImplementedException();

        protected internal override ImmutableArray<TypedConstant> CommonConstructorArguments => _ctorArgs;

        protected internal override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => _namedArgs;

        internal override int GetTargetAttributeSignatureIndex(Symbol targetSymbol, AttributeDescription description)
        {
            throw new NotImplementedException();
        }
    }
}
