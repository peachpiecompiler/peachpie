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
        readonly TypeRef _tref;
        readonly ImmutableArray<Expression> _arguments;
        readonly ImmutableArray<KeyValuePair<VariableName, Expression>> _properties;

        NamedTypeSymbol _type;
        MethodSymbol _ctor;
        ImmutableArray<TypedConstant> _ctorArgs;
        ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArgs;

        public SourceCustomAttribute(TypeRef tref, CallSignature arguments)
        {
            _tref = tref;

            _arguments = ImmutableArray<Expression>.Empty;
            _properties = ImmutableArray<KeyValuePair<VariableName, Expression>>.Empty;

            if (!arguments.IsEmpty)
            {
                foreach (var arg in arguments.Parameters)
                {
                    if (!arg.Exists) continue;

                    if (arg.Name.HasValue)
                    {
                        _properties = _properties.Add(new KeyValuePair<VariableName, Expression>(arg.Name.Value.Name, arg.Expression));
                    }
                    else
                    {
                        _arguments = _arguments.Add(arg.Expression);
                    }
                }
            }
        }

        #region Bind to Symbol and TypedConstant

        internal void Bind(SourceFileSymbol file, SourceTypeSymbol self = null)
        {
            Contract.ThrowIfNull(file);

            var compilation = file.DeclaringCompilation;

            if (_type == null)
            {
                _namedArgs = ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;

                // TODO: check the attribute can be bound to symbol

                var type = compilation.GetTypeFromTypeRef(_tref, self);
                if (type.IsValidType() && compilation.GetWellKnownType(WellKnownType.System_Attribute).IsAssignableFrom(type))
                {
                    // valid CLR attribute
                    // bind strictly

                    // bind arguments
                    if (!TryResolveCtor((NamedTypeSymbol)type, compilation, out _ctor, out _ctorArgs) && type.IsValidType())
                    {
                        //Roslyn.Utilities.JsonWriter
                        compilation.DeclarationDiagnostics.Add(
                            Location.Create(file.SyntaxTree, _tref.Span.ToTextSpan()),
                            Errors.ErrorCode.ERR_NoMatchingOverload,
                            type.Name + "..ctor");
                    }

                    // bind named parameters to CLR attribute properties
                    if (!_properties.IsDefaultOrEmpty)
                    {
                        var namedArgs = new KeyValuePair<string, TypedConstant>[_properties.Length];
                        for (int i = 0; i < namedArgs.Length; i++)
                        {
                            var prop = _properties[i];
                            var member =
                                (Symbol)type.LookupMember<PropertySymbol>(prop.Key.Value) ??
                                (Symbol)type.LookupMember<FieldSymbol>(prop.Key.Value);

                            if (member != null && TryBindTypedConstant(member.GetTypeOrReturnType(), prop.Value, compilation, out var arg))
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
            }
        }

        string ArgumentsToJson()
        {
            string ExpressionToJson(Expression element)
            {
                if (element is LongIntLiteral llit) return llit.Value.ToString();
                if (element is DoubleLiteral dlit) return dlit.Value.ToString("N", System.Globalization.CultureInfo.InvariantCulture);
                if (element is StringLiteral slit)
                {
                    var output = new System.IO.StringWriter();
                    using (var json = new Roslyn.Utilities.JsonWriter(output))
                    {
                        json.Write(slit.Value);
                    }
                    return output.ToString();
                }
                if (element is BoolLiteral blit) return blit.Value ? "true" : "false";
                if (element is NullLiteral) return "null";

                //if (element is TypeRef tref)
                if (element is PseudoClassConstUse pc && pc.Type == PseudoClassConstUse.Types.Class) // TYPE::class
                {
                    return $"\"{pc.TargetType}\"";
                }

                if (element is GlobalConstUse gconst)
                {
                    var qname = gconst.FullName.Name.QualifiedName;
                    if (qname.IsSimpleName)
                    {
                        // common constants
                        if (qname == QualifiedName.True) return "true";
                        if (qname == QualifiedName.False) return "false";
                        if (qname == QualifiedName.Null) return "null";

                        //// lookup constant
                        //var csymbol = compilation.GlobalSemantics.ResolveConstant(qname.Name.Value);
                        //if (csymbol is FieldSymbol fld && fld.HasConstantValue)
                        //{
                        //    return TryBindTypedConstant(target, fld.ConstantValue, out result);
                        //}
                    }

                    // note: namespaced constants are unreachable
                }

                //if (element is ClassConstUse cconst)
                //{
                //    // lookup the type container
                //    var ctype = compilation.GetTypeFromTypeRef(cconst.TargetType);
                //    if (ctype.IsValidType())
                //    {
                //        // lookup constant/enum field (both are FieldSymbol)
                //        var member = ctype.LookupMember<FieldSymbol>(cconst.Name.Value);
                //        if (member != null && member.HasConstantValue)
                //        {
                //            return TryBindTypedConstant(target, member.ConstantValue, out result);
                //        }
                //    }
                //}

                if (element is ArrayEx array)
                {

                }

                throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(element);
            }

            void AppendKey(StringBuilder json, string key)
            {
                json.Append('"');
                json.Append(key);
                json.Append('"');
                json.Append(':');
            }

            var json = new StringBuilder();
            json.Append('{');

            for (int i = 0; i < _arguments.Length; i++)
            {
                if (json.Length > 1) json.Append(',');
                AppendKey(json, i.ToString()); // "i":
                json.Append(ExpressionToJson(_arguments[i]));
            }

            foreach (var named in _properties)
            {
                if (json.Length > 1) json.Append(',');
                AppendKey(json, named.Key.Value); // "i":
                json.Append(ExpressionToJson(named.Value));
            }

            json.Append('}');

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

        static bool TryBindTypedConstant(TypeSymbol target, object value, out TypedConstant result)
        {
            Debug.Assert(!(value is LangElement));

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

            //
            result = default;
            return false;
        }

        static bool TryBindTypedConstant(TypeSymbol target, Expression element, PhpCompilation compilation, out TypedConstant result)
        {
            if (element is LongIntLiteral llit) return TryBindTypedConstant(target, llit.Value, out result);
            if (element is DoubleLiteral dlit) return TryBindTypedConstant(target, dlit.Value, out result);
            if (element is StringLiteral slit) return TryBindTypedConstant(target, slit.Value, out result);
            if (element is BoolLiteral blit) return TryBindTypedConstant(target, blit.Value, out result); 
            if (element is NullLiteral) return TryBindTypedConstant(target, (object)null, out result);

            //if (element is TypeRef tref)
            if (element is PseudoClassConstUse pc && pc.Type == PseudoClassConstUse.Types.Class) // TYPE::class
            {
                if (target.IsStringType())
                {
                    return TryBindTypedConstant(target, pc.TargetType.ToString(), out result);
                }
                else if (target == compilation.GetWellKnownType(WellKnownType.System_Type))
                {
                    result = new TypedConstant(target, TypedConstantKind.Type, compilation.GetTypeFromTypeRef(pc.TargetType));
                    return true;
                }
            }

            if (element is GlobalConstUse gconst)
            {
                var qname = gconst.FullName.Name.QualifiedName;
                if (qname.IsSimpleName)
                {
                    // common constants
                    if (qname == QualifiedName.True) return TryBindTypedConstant(target, true, out result);
                    if (qname == QualifiedName.False) return TryBindTypedConstant(target, true, out result);
                    if (qname == QualifiedName.Null) return TryBindTypedConstant(target, (object)null, out result);

                    // lookup constant
                    var csymbol = compilation.GlobalSemantics.ResolveConstant(qname.Name.Value);
                    if (csymbol is FieldSymbol fld && fld.HasConstantValue)
                    {
                        return TryBindTypedConstant(target, fld.ConstantValue, out result);
                    }
                }

                // note: namespaced constants are unreachable
            }

            if (element is ClassConstUse cconst)
            {
                // lookup the type container
                var ctype = compilation.GetTypeFromTypeRef(cconst.TargetType);
                if (ctype.IsValidType())
                {
                    // lookup constant/enum field (both are FieldSymbol)
                    var member = ctype.LookupMember<FieldSymbol>(cconst.Name.Value);
                    if (member != null && member.HasConstantValue)
                    {
                        return TryBindTypedConstant(target, member.ConstantValue, out result);
                    }
                }
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

        #endregion

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
