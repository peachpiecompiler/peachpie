﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.Symbols;
using System.Reflection.Metadata;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Peachpie.CodeAnalysis.Utilities;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.CodeGen
{
    static class ConversionsExtensions
    {
        // TODO: EmitImplicitConversion(BoundExpression from, ...) // allows to use receiver by ref in case of a value type (PhpValue)

        /// <summary>
        /// Resolves the conversion operator and emits the conversion, expecting <paramref name="from"/> on STACK.
        /// </summary>
        /// <param name="cg">Code generator.</param>
        /// <param name="from">Type on stack.</param>
        /// <param name="to">Type that will be on stack after the successful operation.</param>
        /// <param name="checked">Whether the numeric conversion is checked.</param>
        /// <param name="strict"><c>True</c> to emit strict conversion if possible.</param>
        /// <returns>Whether operation was emitted.</returns>
        public static bool TryEmitImplicitConversion(this CodeGenerator cg, TypeSymbol from, TypeSymbol to, bool @checked = false, bool strict = false)
        {
            if (from != to)
            {
                var conv = cg.DeclaringCompilation.Conversions.ClassifyConversion(
                    from, to, strict
                        ? ConversionKind.Strict | ConversionKind.Implicit
                        : ConversionKind.Implicit);

                if (conv.Exists)
                {
                    EmitConversion(cg, conv, from, to, @checked: @checked);
                }
                else
                {
                    return false;
                }
            }

            //
            return true;
        }

        public static TypeSymbol EmitImplicitConversion(this CodeGenerator cg, TypeSymbol from, TypeSymbol to, bool @checked = false, bool strict = false)
        {
            if (!TryEmitImplicitConversion(cg, from, to, @checked, strict))
            {
                throw cg.NotImplementedException($"Cannot implicitly convert '{from}' to '{to}'");
            }

            return to;
        }

        public static void EmitNumericConversionToDecimal(this CodeGenerator cg, TypeSymbol from, bool @checked = false)
        {
            switch (from.SpecialType)
            {
                case SpecialType.System_Int32:

                    // (decimal)int32
                    cg.EmitCall(
                        ILOpCode.Call,
                        (MethodSymbol)cg.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Decimal__op_Implicit_FromInt32));
                    return;

                case SpecialType.System_Int64:

                    // (decimal)int64
                    cg.EmitCall(
                        ILOpCode.Call,
                        (MethodSymbol)cg.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Decimal__op_Implicit_FromInt64));
                    return;

                case SpecialType.System_Decimal:
                    // decimal
                    return;

                default:

                    // convert to double
                    EmitNumericConversion(cg, from, cg.CoreTypes.Double, @checked);

                    // (decimal)double
                    cg.EmitCall(
                        ILOpCode.Call,
                        (MethodSymbol)cg.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Decimal__op_Explicit_FromDouble));
                    return;
            }
        }

        public static void EmitNumericConversion(this CodeGenerator cg, TypeSymbol from, TypeSymbol to, bool @checked = false)
        {
            if (to.IsEnumType())
            {
                to = to.GetEnumUnderlyingType();
            }

            if (from.IsEnumType())
            {
                from = from.GetEnumUnderlyingType();
            }

            if (from.SpecialType == SpecialType.System_Decimal)
            {
                // explicit numeric conversion, treating decimal as double

                // (double)System.Decimal
                cg.EmitCall(
                    ILOpCode.Call,
                    (MethodSymbol)cg.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Decimal__op_Explicit_ToDouble));
                cg.Builder.EmitOpCode(ILOpCode.Conv_r8);

                //
                from = cg.CoreTypes.Double;
            }

            if (to.SpecialType == SpecialType.System_Decimal)
            {
                EmitNumericConversionToDecimal(cg, from, @checked);
                return;
            }

            var fromcode = from.PrimitiveTypeCode;
            var tocode = to.PrimitiveTypeCode;

            if (fromcode == tocode)
            {
                return;
            }

            if (tocode == Microsoft.Cci.PrimitiveTypeCode.Boolean)
            {
                switch (fromcode)
                {
                    case Microsoft.Cci.PrimitiveTypeCode.Float32:
                        // Template: !(STACK == 0.0f)
                        cg.Builder.EmitSingleConstant(0.0f);
                        cg.Builder.EmitOpCode(ILOpCode.Ceq);
                        cg.EmitLogicNegation();
                        return;
                    case Microsoft.Cci.PrimitiveTypeCode.Float64:
                        // Template: !(STACK == 0.0)
                        cg.Builder.EmitDoubleConstant(0.0);
                        cg.Builder.EmitOpCode(ILOpCode.Ceq);
                        cg.EmitLogicNegation();
                        return;
                }

                // otherwise,
                // treat boolean as to int32 conversion
                tocode = Microsoft.Cci.PrimitiveTypeCode.Int32;
            }

            if (fromcode == Microsoft.Cci.PrimitiveTypeCode.Boolean)
            {
                fromcode = Microsoft.Cci.PrimitiveTypeCode.Int32;
            }

            //
            cg.Builder.EmitNumericConversion(fromcode, tocode, @checked);
        }

        /// <summary>
        /// Emits the given conversion. 'from' and 'to' matches the classified conversion.
        /// </summary>
        public static void EmitConversion(this CodeGenerator cg, CommonConversion conversion, TypeSymbol from, TypeSymbol to, TypeSymbol op = null, bool @checked = false)
        {
            // {from}, {op} is loaded on stack

            //

            if (conversion.Exists == false)
            {
                throw cg.NotImplementedException($"Conversion from '{from}' to '{to}' ");
            }

            if (conversion.IsNullable)
            {
                if (from.IsNullableType(out var ttype))
                {
                    if (op != null)
                    {
                        // TODO
                        throw new ArgumentException(nameof(op));
                    }

                    var lbltrue = new NamedLabel("has value");
                    var lblend = new NamedLabel("end");
                    var tmp = cg.GetTemporaryLocal(from, true);
                    cg.Builder.EmitLocalStore(tmp);

                    // Template: tmp.HasValue ? convert(tmp.Value) : default
                    cg.Builder.EmitLocalAddress(tmp);
                    cg.EmitCall(ILOpCode.Call, cg.DeclaringCompilation.System_Nullable_T_HasValue(from));
                    cg.Builder.EmitBranch(ILOpCode.Brtrue, lbltrue);

                    // default:
                    cg.EmitLoadDefault(to);
                    cg.Builder.EmitBranch(ILOpCode.Br, lblend);
                    // cg.Builder.AdjustStack(-1); // ?

                    // lbltrue:
                    cg.Builder.MarkLabel(lbltrue);
                    // Template: convert( tmp.GetValueOrDefault() )
                    cg.Builder.EmitLocalAddress(tmp);
                    cg.EmitCall(ILOpCode.Call, cg.DeclaringCompilation.System_Nullable_T_GetValueOrDefault(from)).Expect(ttype);
                    EmitConversion(cg, conversion.WithIsNullable(false), ttype, to, op, @checked);

                    // lblend:
                    cg.Builder.MarkLabel(lblend);

                    return;
                }

                if (to.IsNullableType(out ttype)) // NOTE: not used yet
                {
                    // new Nullable<TType>( convert(from) )
                    EmitConversion(cg, conversion.WithIsNullable(false), from, ttype, op, @checked);
                    cg.EmitCall(ILOpCode.Newobj, ((NamedTypeSymbol)to).InstanceConstructors[0]); // new Nullable<T>( STACK )
                    return;
                }

                throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
            }

            if (conversion.IsIdentity)
            {
                if (op != null)
                {
                    throw new ArgumentException(nameof(op));
                }

                if (to.SpecialType == SpecialType.System_Void)
                {
                    // POP
                    cg.EmitPop(from);
                }

                // nop
            }
            else if (conversion.IsNumeric)
            {
                if (op != null)
                {
                    throw new ArgumentException(nameof(op));
                }

                EmitNumericConversion(cg, from, to, @checked: @checked);
            }
            else if (conversion.IsReference)
            {
                if (op != null)
                {
                    throw new ArgumentException(nameof(op));
                }

                // TODO: ensure from/to is a valid reference type
                cg.EmitCastClass(to);
            }
            else if (conversion.IsUserDefined)
            {
                var method = (MethodSymbol)conversion.MethodSymbol;
                var ps = method.Parameters;
                int pconsumed = 0;

                if (method.HasThis)
                {
                    if (from.IsValueType)
                    {
                        if (op != null || from.IsVoid())
                        {
                            throw new ArgumentException(nameof(op));
                        }

                        cg.EmitStructAddr(from);
                    }
                }
                else
                {
                    if (ps[0].RefKind != RefKind.None) throw new InvalidOperationException();
                    if (from != ps[0].Type)
                    {
                        if (op != null)
                        {
                            if (!ps[0].Type.IsAssignableFrom(from))
                            {
                                throw new ArgumentException(nameof(op));
                            }
                        }
                        else
                        {
                            EmitImplicitConversion(cg, from, ps[0].Type, @checked: @checked);
                        }
                    }
                    pconsumed++;
                }

                if (op != null)
                {
                    if (ps.Length > pconsumed)
                    {
                        EmitImplicitConversion(cg, op, ps[pconsumed].Type, @checked: @checked);
                    }
                    pconsumed++;
                }

                // Context ctx, 
                if (ps.Length > pconsumed && SpecialParameterSymbol.IsContextParameter(ps[pconsumed]))
                {
                    cg.EmitLoadContext();
                    pconsumed++;
                }

                if (ps.Length != pconsumed) throw new InvalidOperationException();

                EmitImplicitConversion(cg, cg.EmitCall(method.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, method), to, @checked: true);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static CommonConversion WithIsNullable(this CommonConversion conv, bool isNullable)
        {
            return new CommonConversion(conv.Exists, conv.IsIdentity, conv.IsNumeric, conv.IsReference, conv.IsImplicit, isNullable, conv.MethodSymbol);
        }
    }
}
