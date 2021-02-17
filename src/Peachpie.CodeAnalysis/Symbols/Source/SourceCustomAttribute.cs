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
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.Symbols
{
    sealed partial class SourceCustomAttribute : BaseAttributeData
    {
        readonly IBoundTypeRef _tref;

        readonly ImmutableArray<BoundArgument> _arguments;

        readonly PhpCompilation _compilation;

        /// <summary>
        /// Attribute arguments.
        /// </summary>
        public ImmutableArray<BoundArgument> Arguments => _arguments;

        NamedTypeSymbol _type;
        MethodSymbol _ctor;
        ImmutableArray<TypedConstant> _ctorArgs;
        ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArgs;

        public SourceCustomAttribute(PhpCompilation compilation, SourceTypeSymbol containingType, IBoundTypeRef tref, ImmutableArray<BoundArgument> arguments)
        {
            _compilation = compilation;
            _tref = tref;
            _arguments = arguments;

            TypeCtx = new FlowAnalysis.TypeRefContext(compilation, containingType);
        }

        #region Bind to Symbol and TypedConstant

        bool Bind()
        {
            var compilation = _compilation;

            if (_type == null)
            {
                _namedArgs = ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;

                // TODO: check the attribute can be bound to symbol

                var type = _tref.ResolveRuntimeType(compilation);
                if (type.IsValidType() && compilation.GetWellKnownType(WellKnownType.System_Attribute).IsAssignableFrom(type))
                {
                    // valid CLR attribute
                    // bind strictly

                    // bind arguments
                    if (!TryResolveCtor((NamedTypeSymbol)type, compilation, out _ctor, out _ctorArgs))
                    {
                        throw new InvalidOperationException("no matching .ctor");
                    }

                    // bind named parameters to CLR attribute properties
                    foreach (var arg in _arguments)
                    {
                        if (arg.ParameterName == null)
                            continue;

                        var member =
                           (Symbol)type.LookupMember<PropertySymbol>(arg.ParameterName) ??
                           (Symbol)type.LookupMember<FieldSymbol>(arg.ParameterName);

                        if (member != null && TryBindTypedConstant(member.GetTypeOrReturnType(), arg.Value.ConstantValue, out var constant))
                        {
                            _namedArgs = _namedArgs.Add(new KeyValuePair<string, TypedConstant>(arg.ParameterName, constant));
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    //
                    _type = (NamedTypeSymbol)type;
                }
                else
                {
                    // store just the metadata
                    _type = compilation.CoreTypes.PhpCustomAtribute ?? throw new InvalidOperationException("PhpCustomAtribute not defined.");
                    _ctor = _type.Constructors.Single(m =>
                        m.ParameterCount == 2 &&
                        m.Parameters[0].Type.IsStringType() &&
                        m.Parameters[1].Type.IsByteArray());

                    //compilation.DeclarationDiagnostics.Add(
                    //    Location.Create(file.SyntaxTree, _tref.Span.ToTextSpan()),
                    //    Errors.ErrorCode.ERR_TypeNameCannotBeResolved,
                    //    _tref.ToString());

                    //type = new MissingMetadataTypeSymbol(_tref.ToString(), 0, false);

                    _ctorArgs = ImmutableArray.Create(
                        compilation.CreateTypedConstant(_tref.ToString()),
                        compilation.CreateTypedConstant(Encoding.UTF8.GetBytes(ArgumentsToJson())));
                }

                // TODO: validate {type} attribute TARGET if any
                //Attribute::TARGET_CLASS
                //Attribute::TARGET_FUNCTION
                //Attribute::TARGET_METHOD
                //Attribute::TARGET_PROPERTY
                //Attribute::TARGET_CLASS_CONSTANT
                //Attribute::TARGET_PARAMETER
                //Attribute::TARGET_ALL
            }

            //
            return _type != null;
        }

        /// <summary>Simple AST to JSON serialization.</summary>
        string ArgumentsToJson()
        {
            void ExpressionToJson(BoundExpression element, StringBuilder output)
            {
                if (element.ConstantValue.HasValue)
                {
                    switch (element.ConstantValue.Value)
                    {
                        case int i:
                            output.Append(i);
                            break;
                        case long l:
                            output.Append(l);
                            break;
                        case double d:
                            output.Append(d.ToString("N", System.Globalization.CultureInfo.InvariantCulture));
                            break;
                        case string s:
                            using (var writer = new System.IO.StringWriter(output))
                            using (var json = new Roslyn.Utilities.JsonWriter(writer))
                            {
                                json.Write(s);
                            }
                            break;
                        case bool b:
                            output.Append(b ? "true" : "false");
                            break;
                        case null:
                            output.Append("null");
                            break;
                        default:
                            throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(element.ConstantValue.Value);
                    }
                }
                else
                {
                    switch (element)
                    {

                        case BoundPseudoClassConst pc: // TYPE::class
                            if (pc.TargetType is Semantics.TypeRef.BoundClassTypeRef cref) // not good
                            {
                                output.Append('"');
                                output.Append(cref.ClassName.ToString());
                                output.Append('"');
                                break;
                            }
                            goto default;

                        case BoundArrayEx array:
                            if (array.Items.Length == 0)
                            {
                                output.Append("[]");
                            }
                            else if (array.Items.All(x => x.Key == null))
                            {
                                output.Append('[');
                                for (int i = 0; i < array.Items.Length; i++)
                                {
                                    if (i != 0) output.Append(',');
                                    ExpressionToJson(array.Items[i].Value, output);
                                }
                                output.Append(']');
                            }
                            else
                            {
                                output.Append('{');
                                for (int i = 0; i < array.Items.Length; i++)
                                {
                                    if (i != 0) output.Append(',');
                                    output.Append('"');
                                    output.Append(array.Items[i].Key switch
                                    {
                                        BoundLiteral lit => lit.ConstantValue.Value.ToString(),
                                        null => i.ToString(),
                                        _ => Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(array.Items[i].Key),
                                    });
                                    output.Append('"');
                                    output.Append(':');
                                    ExpressionToJson(array.Items[i].Value, output);
                                }
                                output.Append('}');
                            }
                            break;
                        default:
                            throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(element);
                    }
                }
            }

            void AppendKey(StringBuilder json, string key)
            {
                json.Append('"');
                json.Append(key);
                json.Append('"');
                json.Append(':');
            }

            if (_arguments.Length == 0)
            {
                return string.Empty;
            }

            var json = new StringBuilder();

            if (_arguments.All(a => a.ParameterName == null))
            {
                // use array syntax
                json.Append('[');
                for (int i = 0; i < _arguments.Length; i++)
                {
                    if (json.Length > 1) json.Append(',');
                    ExpressionToJson(_arguments[i].Value, json);
                }
                json.Append(']');
            }
            else
            {
                // use object syntax
                json.Append('{');

                for (int i = 0; i < _arguments.Length; i++)
                {
                    if (json.Length > 1) json.Append(',');
                    AppendKey(json, _arguments[i].ParameterName ?? i.ToString()); // "i":
                    ExpressionToJson(_arguments[i].Value, json);
                }

                json.Append('}');
            }

            //
            return json.ToString();
        }

        static bool TryBindTypedConstant(TypeSymbol target, long value, out TypedConstant result)
        {
            switch (target.SpecialType)
            {
                case SpecialType.System_Byte:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, (byte)value);
                    return true;
                case SpecialType.System_Int32:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, (int)value);
                    return true;
                case SpecialType.System_Int64:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, value);
                    return true;
                case SpecialType.System_UInt32:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, (uint)value);
                    return true;
                case SpecialType.System_UInt64:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, (ulong)value);
                    return true;
                case SpecialType.System_Double:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, (double)value);
                    return true;
                case SpecialType.System_Single:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, (float)value);
                    return true;
                default:

                    if (target.IsEnumType())
                    {
                        return TryBindTypedConstant(target.GetEnumUnderlyingType(), value, out result);
                    }

                    result = default;
                    return false;
            }
        }

        static bool TryBindTypedConstant(TypeSymbol target, double value, out TypedConstant result)
        {
            switch (target.SpecialType)
            {
                case SpecialType.System_Double:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, value);
                    return true;
                case SpecialType.System_Single:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, (float)value);
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        static bool TryBindTypedConstant(TypeSymbol target, string value, out TypedConstant result)
        {
            switch (target.SpecialType)
            {
                case SpecialType.System_String:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, value);
                    return true;
                case SpecialType.System_Char:
                    if (value.Length != 1) goto default;
                    result = new TypedConstant(target, TypedConstantKind.Primitive, value[0]);
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        static bool TryBindTypedConstant(TypeSymbol target, bool value, out TypedConstant result)
        {
            switch (target.SpecialType)
            {
                case SpecialType.System_Boolean:
                    result = new TypedConstant(target, TypedConstantKind.Primitive, value);
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        static bool TryBindTypedConstant(TypeSymbol target, Optional<object> constant, out TypedConstant result)
        {
            if (constant.HasValue)
            {
                var value = constant.Value;
                if (ReferenceEquals(value, null))
                {
                    // NULL
                    result = new TypedConstant(target, TypedConstantKind.Primitive, null);
                    return true && target.IsReferenceType;
                }

                if (value is int i) return TryBindTypedConstant(target, i, out result);
                if (value is long l) return TryBindTypedConstant(target, l, out result);
                if (value is uint u) return TryBindTypedConstant(target, u, out result);
                if (value is byte b8) return TryBindTypedConstant(target, b8, out result);
                if (value is double d) return TryBindTypedConstant(target, d, out result);
                if (value is float f) return TryBindTypedConstant(target, f, out result);
                if (value is string s) return TryBindTypedConstant(target, s, out result);
                if (value is char c) return TryBindTypedConstant(target, c.ToString(), out result);
                if (value is bool b) return TryBindTypedConstant(target, b, out result);
            }
            //
            result = default;
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
                        if (pi >= _arguments.Length || _arguments[pi].ParameterName != null)
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

                        if (TryBindTypedConstant(ps[pi].Type, _arguments[pi].Value.ConstantValue, out var arg))
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

        #endregion

        public override NamedTypeSymbol AttributeClass => Bind() ? _type : throw ExceptionUtilities.Unreachable;

        public override MethodSymbol AttributeConstructor => Bind() ? _ctor : throw ExceptionUtilities.Unreachable;

        public override SyntaxReference ApplicationSyntaxReference => throw new NotImplementedException();

        protected internal override ImmutableArray<TypedConstant> CommonConstructorArguments => Bind() ? _ctorArgs : throw ExceptionUtilities.Unreachable;

        protected internal override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => Bind() ? _namedArgs : throw ExceptionUtilities.Unreachable;

        internal override int GetTargetAttributeSignatureIndex(Symbol targetSymbol, AttributeDescription description)
        {
            throw new NotImplementedException();
        }
    }
}
