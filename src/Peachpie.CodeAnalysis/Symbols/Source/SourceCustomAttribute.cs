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
                _type = (NamedTypeSymbol)symbol.DeclaringCompilation.GlobalSemantics.ResolveType(_tref.ClassName)
                    ?? new MissingMetadataTypeSymbol(_tref.ClassName.ClrName(), 0, false);

                _ctorArgs = _arguments
                    .Select(element => ToTypedConstant(element, symbol.DeclaringCompilation))
                    .AsImmutable();

                _namedArgs = _properties
                    .Select(pair => new KeyValuePair<string, TypedConstant>(pair.Key.Value, ToTypedConstant(pair.Value, symbol.DeclaringCompilation)))
                    .AsImmutable();

                _ctor = ResolveCtor(_type, ref _ctorArgs);
            }
        }

        static TypedConstant ToTypedConstant(LangElement element, PhpCompilation compilation)
        {
            if (element is LongIntLiteral llit)
            {
                // note: convert to uint, int, ulong, double, float, if matches .ctor type
                return new TypedConstant(compilation.CoreTypes.Long.Symbol, TypedConstantKind.Primitive, llit.Value);
            }

            if (element is StringLiteral slit)
            {
                return new TypedConstant(compilation.CoreTypes.String.Symbol, TypedConstantKind.Primitive, slit.Value);
            }

            if (element is DoubleLiteral dlit)
            {
                // note: convert to float, if matches .ctor type
                return new TypedConstant(compilation.CoreTypes.Double.Symbol, TypedConstantKind.Primitive, dlit.Value);
            }

            if (element is TypeRef tref)
            {
                return new TypedConstant(compilation.GetWellKnownType(WellKnownType.System_Type), TypedConstantKind.Type, compilation.GetTypeFromTypeRef(tref));
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
            throw ExceptionUtilities.UnexpectedValue(element);
        }

        static MethodSymbol ResolveCtor(NamedTypeSymbol type, ref ImmutableArray<TypedConstant> args)
        {
            if (type.IsValidType())
            {
                var candidates = type.InstanceConstructors;
                for (int i = 0; i < candidates.Length; i++)
                {
                    var m = candidates[i];

                    if (m.DeclaredAccessibility != Accessibility.Public) continue; // TODO: or current class context
                    if (m.IsGenericMethod) { Debug.Fail("unexpected"); continue; } // NS
                    if (m.ParameterCount < args.Length) continue; // be strict

                    var match = true;
                    var ps = m.Parameters;
                    for (var pi = 0; pi < ps.Length; pi ++)
                    {
                        if (pi >= args.Length)
                        {
                            if (ps[pi].IsOptional) continue; // ok
                            //
                            match = false;
                            break;
                        }

                        var pt = ps[pi].Type;
                        if (pt.Equals(args[pi].Type)) continue; // ok
                        match = false; // TODO: type conv
                    }

                    if (match)
                    {
                        return m;
                    }
                }
            }

            return new MissingMethodSymbol();
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
