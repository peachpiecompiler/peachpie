using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Possible conversion operation.
    /// </summary>
    [Flags]
    internal enum ConversionKind
    {
        /// <summary>
        /// A numeric conversion only.
        /// </summary>
        Numeric = 1,

        /// <summary>
        /// Cast the CLR object reference.
        /// </summary>
        Reference = 2,

        /// <summary>
        /// Strict type conversion.
        /// Throws an exception if type does not match and numeric conversion does not exist.
        /// </summary>
        Strict = 4 | Implicit | Numeric | Reference,

        /// <summary>
        /// Implicit conversion.
        /// Produces a default value and a warning if conversion is not successful.
        /// </summary>
        Implicit = 8 | Numeric | Reference,

        /// <summary>
        /// Explicit casting.
        /// Always quiet conversion if conversion is possible.
        /// </summary>
        Explicit = 16 | Numeric | Reference,
    }

    sealed class Conversions
    {
        readonly PhpCompilation _compilation;

        public Conversions(PhpCompilation compilation)
        {
            _compilation = compilation ?? throw ExceptionUtilities.ArgumentNull();
        }

        static CommonConversion IdentityConversion => new CommonConversion(true, true, false, false, true, null);
        static CommonConversion ReferenceConversion => new CommonConversion(true, false, false, true, true, null);
        static CommonConversion ExplicitReferenceConversion => new CommonConversion(true, false, false, true, false, null);
        static CommonConversion NoConversion => new CommonConversion(false, false, false, false, false, null);
        static CommonConversion ImplicitNumeric => new CommonConversion(true, false, true, false, true, null);
        static CommonConversion ExplicitNumeric => new CommonConversion(true, false, true, false, false, null);

        /// <summary>
        /// Calculates "cost" of conversion.
        /// </summary>
        static int ConvCost(CommonConversion conv, TypeSymbol from, TypeSymbol to)
        {
            if (conv.Exists)
            {
                if (conv.IsIdentity)
                {
                    return 0;
                }

                // calculate magically the conversion cost
                // TODO: unify with ConversionCost

                int cost;

                if (conv.IsReference)
                {
                    cost = 1;
                }
                else if (conv.IsNumeric)
                {
                    cost = 1;

                    var clfrom = ClassifyNumericType(from);
                    var clto = ClassifyNumericType(to);

                    if (clfrom.size == 1) // bool
                    {
                        cost += 128;    // bool -> {0|1} // only if there is nothing better!!!
                    }

                    // 
                    cost += Math.Abs(clto.size / 16 - clfrom.size / 16) + (clfrom.floating != clto.floating ? 1 : 0);
                }
                else if (conv.IsUserDefined) // preferring user defined conversions
                {
                    // do not treat all the implicit conversios the same

                    // PhpString <-> string
                    if ((to.IsStringType() || from.IsStringType()) && (to.Is_PhpString() || from.Is_PhpString()))
                    {
                        cost = 2;
                    }
                    else
                    {
                        cost = 4;
                    }
                }
                else
                {
                    cost = 8;
                }

                //
                if (conv.IsImplicit == false)
                {
                    cost *= 2;
                }

                //
                return cost;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        static (bool floating, bool signed, int size) ClassifyNumericType(TypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean: return (false, false, 1); // we classsify boolean as a number as well!
                case SpecialType.System_Char: return (false, false, 8);
                case SpecialType.System_SByte: return (false, true, 8);
                case SpecialType.System_Byte: return (false, false, 8);
                case SpecialType.System_Int16: return (false, true, 16);
                case SpecialType.System_UInt16: return (false, false, 16);
                case SpecialType.System_Int32: return (false, true, 32);
                case SpecialType.System_UInt32: return (false, false, 32);
                case SpecialType.System_Int64: return (false, true, 64);
                case SpecialType.System_UInt64: return (false, false, 64);
                //case SpecialType.System_IntPtr: return (false, true, 64);
                //case SpecialType.System_UIntPtr: return (false, false, 64);
                case SpecialType.System_Single: return (true, true, 32);
                case SpecialType.System_Double: return (true, true, 64);
                case SpecialType.System_Decimal: return (true, true, 128);
                default:

                    if (type.IsEnumType())
                    {
                        return ClassifyNumericType(type.GetEnumUnderlyingType());
                    }

                    return default;
            }
        }

        // numeric conversions
        public static CommonConversion ClassifyNumericConversion(TypeSymbol from, TypeSymbol to)
        {
            var fromnum = ClassifyNumericType(from);
            if (fromnum.size == 0) return NoConversion;

            var tonum = ClassifyNumericType(to);
            if (tonum.size == 0) return NoConversion;

            // both types are numbers,
            // naive conversion:

            if (fromnum.size < tonum.size || (fromnum.size == tonum.size && fromnum.signed == tonum.signed)) // blah
                return ImplicitNumeric;
            else
                return ExplicitNumeric;
        }

        // resolve operator method
        public MethodSymbol ResolveOperator(TypeSymbol receiver, bool hasref, string[] opnames, TypeSymbol[] extensions, TypeSymbol operand = null, TypeSymbol target = null)
        {
            Debug.Assert(receiver != null);
            Debug.Assert(opnames != null && opnames.Length != 0);

            MethodSymbol candidate = null;
            int candidatecost = int.MaxValue;   // candidate cost
            int candidatecost_minor = 0;        // second cost

            for (int ext = -1; ext < extensions.Length; ext++)
            {
                // TODO: go through interfaces

                for (var container = ext < 0 ? receiver : extensions[ext]; container != null; container = container.IsStatic ? null : container.BaseType)
                {
                    if (container.SpecialType == SpecialType.System_ValueType) continue; //

                    for (int i = 0; i < opnames.Length; i++)
                    {
                        var members = container.GetMembers(opnames[i]);
                        for (int m = 0; m < members.Length; m++)
                        {
                            if (members[m] is MethodSymbol method)
                            {
                                if (ext >= 0 && !method.IsStatic) continue;    // only static methods allowed in extension containers
                                if (method.DeclaredAccessibility != Accessibility.Public) continue;
                                if (method.Arity != 0) continue; // CONSIDER

                                // TODO: replace with overload resolution

                                int cost = 0;
                                int cost_minor = 0;

                                if (target != null && method.ReturnType != target)
                                {
                                    var conv = ClassifyConversion(method.ReturnType, target, ConversionKind.Numeric | ConversionKind.Reference);
                                    if (conv.Exists)    // TODO: chain the conversion, sum the cost
                                    {
                                        cost += ConvCost(conv, method.ReturnType, target);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                var ps = method.Parameters;
                                int pconsumed = 0;

                                // TSource receiver,
                                if (method.IsStatic)
                                {
                                    if (ps.Length <= pconsumed) continue;
                                    bool isbyref = ps[pconsumed].RefKind != RefKind.None;
                                    if (isbyref && hasref == false) continue;
                                    // if (container != receiver && ps[pconsumed].HasThisAttribute == false) continue; // [ThisAttribute] // proper extension method
                                    var pstype = ps[pconsumed].Type;
                                    if (pstype != receiver)
                                    {
                                        if (isbyref) continue; // cannot convert addr

                                        var conv = ClassifyConversion(receiver, pstype, ConversionKind.Numeric | ConversionKind.Reference);
                                        if (conv.Exists)   // TODO: chain the conversion
                                        {
                                            cost += ConvCost(conv, receiver, pstype);
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    pconsumed++;
                                }

                                // Context ctx, 
                                if (pconsumed < ps.Length && SpecialParameterSymbol.IsContextParameter(ps[pconsumed]))
                                {
                                    pconsumed++;
                                    cost_minor--; // specialized operator - prefered
                                }

                                // TOperand,
                                if (operand != null)
                                {
                                    if (ps.Length <= pconsumed) continue;
                                    if (ps[pconsumed].Type != operand)
                                    {
                                        var conv = ClassifyConversion(operand, ps[pconsumed].Type, ConversionKind.Implicit);
                                        if (conv.Exists)    // TODO: chain the conversion
                                        {
                                            cost += ConvCost(conv, operand, ps[pconsumed].Type);
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    pconsumed++;
                                }

                                // Context ctx, 
                                if (pconsumed < ps.Length && SpecialParameterSymbol.IsContextParameter(ps[pconsumed]))
                                {
                                    pconsumed++;
                                    cost_minor--;   // specialized operator - prefered
                                }

                                if (ps.Length != pconsumed) continue;

                                if (container.SpecialType == SpecialType.System_Object ||
                                    container.IsValueType)
                                {
                                    //cost++; // should be enabled
                                    cost_minor++;   // implicit conversion
                                }

                                //
                                if (cost < candidatecost || (cost == candidatecost && cost_minor < candidatecost_minor))
                                {
                                    candidate = method;
                                    candidatecost = cost;
                                    candidatecost_minor = cost_minor;
                                }
                            }
                        }
                    }
                }
            }

            //

            return candidate;
        }

        // resolve implicit conversion
        string[] ImplicitConversionOpNames(TypeSymbol target)
        {
            switch (target.SpecialType)
            {
                case SpecialType.System_Char: return new[] { "AsChar", "ToChar" };
                case SpecialType.System_Boolean: return new[] { WellKnownMemberNames.ImplicitConversionName, "AsBoolean", "ToBoolean" };
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int32: return new[] { WellKnownMemberNames.ImplicitConversionName, "AsInt", "ToInt", "ToLong" };
                case SpecialType.System_Int64: return new[] { WellKnownMemberNames.ImplicitConversionName, "ToLong" };
                case SpecialType.System_Single:
                case SpecialType.System_Double: return new[] { WellKnownMemberNames.ImplicitConversionName, "AsDouble", "ToDouble" };
                case SpecialType.System_Decimal: return new[] { WellKnownMemberNames.ImplicitConversionName, "ToDecimal" };
                case SpecialType.System_String: return new[] { WellKnownMemberNames.ImplicitConversionName, "AsString", WellKnownMemberNames.ObjectToString };
                case SpecialType.System_Object: return new[] { "AsObject" }; // implicit conversion to object is not possible
                default:

                    // AsPhpArray

                    // ToNumber
                    if (target == _compilation.CoreTypes.PhpNumber.Symbol) return new[] { WellKnownMemberNames.ImplicitConversionName, "ToNumber" };

                    // ToPhpString
                    if (target == _compilation.CoreTypes.PhpString.Symbol) return new[] { WellKnownMemberNames.ImplicitConversionName, "ToPhpString" };

                    // AsResource
                    // AsObject
                    // AsPhpValue
                    if (target == _compilation.CoreTypes.PhpValue.Symbol) return new[] { WellKnownMemberNames.ImplicitConversionName };

                    // AsPhpAlias
                    if (target == _compilation.CoreTypes.PhpAlias.Symbol) return new[] { WellKnownMemberNames.ImplicitConversionName, "AsPhpAlias" };

                    // enum
                    if (target.IsEnumType()) return ImplicitConversionOpNames(target.GetEnumUnderlyingType());

                    //
                    return new[] { WellKnownMemberNames.ImplicitConversionName };
            }
        }

        string[] ExplicitConversionOpNames(TypeSymbol target)
        {
            switch (target.SpecialType)
            {
                case SpecialType.System_Boolean: return new[] { WellKnownMemberNames.ExplicitConversionName, "ToBoolean" };
                case SpecialType.System_Int32: return new[] { WellKnownMemberNames.ExplicitConversionName, "ToInt", "ToLong" };
                case SpecialType.System_Int64: return new[] { WellKnownMemberNames.ExplicitConversionName, "ToLong" };
                case SpecialType.System_Double: return new[] { WellKnownMemberNames.ExplicitConversionName, "ToDouble" };
                case SpecialType.System_String: return new[] { WellKnownMemberNames.ExplicitConversionName, WellKnownMemberNames.ObjectToString };
                case SpecialType.System_Object: return new[] { "ToObject" };    // implicit conversion to object is not possible
                default:

                    // AsPhpArray
                    if (target == _compilation.CoreTypes.PhpArray.Symbol) return new[] { WellKnownMemberNames.ExplicitConversionName, "ToArray" };

                    // ToNumber
                    if (target == _compilation.CoreTypes.PhpNumber.Symbol) return new[] { WellKnownMemberNames.ExplicitConversionName, "ToNumber" };

                    // ToPhpString
                    if (target == _compilation.CoreTypes.PhpString.Symbol) return new[] { WellKnownMemberNames.ExplicitConversionName, "ToPhpString" };

                    // ToPhpValue
                    // ToPhpAlias

                    // ToBytes
                    if (target.IsSZArray() && ((ArrayTypeSymbol)target).ElementType.SpecialType == SpecialType.System_Byte)
                    {
                        return new[] { WellKnownMemberNames.ExplicitConversionName, "ToBytes" };
                    }

                    return new[] { WellKnownMemberNames.ExplicitConversionName };
            }
        }

        /// <summary>
        /// Checks the type is a reference type (derived from <c>System.Object</c>) but it has a special meaning in PHP's semantics.
        /// Such a type cannot be converted to Object by simple casting.
        /// Includes: string, resource, array, alias.
        /// </summary>
        public static bool IsSpecialReferenceType(TypeSymbol t)
        {
            if (t.IsReferenceType)
            {
                if (t.SpecialType == SpecialType.System_String)
                {
                    return true;
                }

                if (t.ContainingAssembly?.IsPeachpieCorLibrary == true)
                {
                    if (t.Is_PhpAlias() ||
                        t.Is_PhpArray() ||
                        t.Is_PhpResource())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        MethodSymbol TryWellKnownImplicitConversion(TypeSymbol from, TypeSymbol to)
        {
            // Object -> PhpValue
            if (to == _compilation.CoreTypes.PhpValue && from.IsReferenceType && !IsSpecialReferenceType(from))
            {
                // expecting the object to be a class instance
                return _compilation.CoreMethods.PhpValue.FromClass_Object;
            }

            //
            return null;
        }

        public CommonConversion ClassifyConversion(TypeSymbol from, TypeSymbol to, ConversionKind kinds)
        {
            if (from == to)
            {
                return IdentityConversion;
            }

            // implicit conversions handled by 'EmitConversion':
            if (to.SpecialType == SpecialType.System_Void)
            {
                return IdentityConversion;
            }

            // object cast possible implicitly:
            if ((kinds & ConversionKind.Reference) == ConversionKind.Reference)
            {
                if (from.IsReferenceType && to.IsReferenceType && from.IsOfType(to))
                {
                    // (PHP) string, resource, array, alias -> object: NoConversion

                    if (to.SpecialType != SpecialType.System_Object || !IsSpecialReferenceType(from))
                    {
                        return ReferenceConversion;
                    }
                }

                if (to.SpecialType == SpecialType.System_Object && (from.IsInterfaceType() || (from.IsReferenceType && from.IsTypeParameter())))
                {
                    return ReferenceConversion;
                }
            }

            // resolve conversion operator method:
            if ((kinds & ConversionKind.Numeric) == ConversionKind.Numeric)
            {
                var conv = ClassifyNumericConversion(from, to);
                if (conv.Exists)
                {
                    return conv;
                }
            }

            // strict:
            if ((kinds & ConversionKind.Strict) == ConversionKind.Strict)
            {
                var op = ResolveOperator(from, false, ImplicitConversionOpNames(to), new[] { _compilation.CoreTypes.StrictConvert.Symbol }, target: to);
                if (op != null)
                {
                    return new CommonConversion(true, false, false, false, true, op);
                }
            }

            // implicit
            if ((kinds & ConversionKind.Implicit) == ConversionKind.Implicit)
            {
                var op = TryWellKnownImplicitConversion(from, to) ?? ResolveOperator(from, false, ImplicitConversionOpNames(to), new[] { to, _compilation.CoreTypes.Convert.Symbol }, target: to);
                if (op != null)
                {
                    return new CommonConversion(true, false, false, false, true, op);
                }
            }

            // explicit:
            if ((kinds & ConversionKind.Explicit) == ConversionKind.Explicit)
            {
                var op = ResolveOperator(from, false, ExplicitConversionOpNames(to), new[] { to, _compilation.CoreTypes.Convert.Symbol }, target: to);
                if (op != null)
                {
                    return new CommonConversion(true, false, false, false, false, op);
                }
                // explicit reference conversion (reference type -> reference type)
                else if (
                    from.IsReferenceType && to.IsReferenceType &&
                    !IsSpecialReferenceType(from) && !IsSpecialReferenceType(to) &&
                    !from.IsArray() && !to.IsArray())
                {
                    return ExplicitReferenceConversion;
                }
            }

            //
            return NoConversion;
        }
    }
}
