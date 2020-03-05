using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CodeGen;
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

        public static void EmitImplicitConversion(this CodeGenerator cg, TypeSymbol from, TypeSymbol to, bool @checked = false, bool strict = false)
        {
            if (!TryEmitImplicitConversion(cg, from, to, @checked, strict))
            {
                throw cg.NotImplementedException($"Cannot implicitly convert '{from}' to '{to}'");
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

            var fromcode = from.PrimitiveTypeCode;
            var tocode = to.PrimitiveTypeCode;

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
                        if (op != null)
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
    }
}
