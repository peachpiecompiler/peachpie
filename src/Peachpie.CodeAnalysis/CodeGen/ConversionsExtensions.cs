using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using System.Reflection.Metadata;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.CodeGen
{
    static class ConversionsExtensions
    {
        public static void EmitImplicitConversion(this CodeGenerator cg, TypeSymbol from, TypeSymbol to)
        {
            if (from != to)
            {
                var conv = cg.DeclaringCompilation.ClassifyCommonConversion(from, to);
                if (conv.IsImplicit || conv.IsNumeric)
                {
                    EmitConversion(cg, conv, from, to);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot implicitly convert '{from}' to '{to}'.");
                }
            }
        }

        public static void EmitNumericConversion(this CodeGenerator cg, TypeSymbol from, TypeSymbol to, bool @checked = false)
        {
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
        public static void EmitConversion(this CodeGenerator cg, CommonConversion conversion, TypeSymbol from, TypeSymbol to)
        {
            // {from} is loaded on stack

            //

            if (conversion.Exists == false)
            {
                throw new ArgumentException($"Conversion from '{from}' to '{to}' does not exist.");
            }

            if (conversion.IsIdentity)
            {
                if (to.SpecialType == SpecialType.System_Void)
                {
                    // POP
                    cg.EmitPop(from);
                }

                // nop
            }
            else if (conversion.IsNumeric)
            {
                EmitNumericConversion(cg, from, to, false);
            }
            else if (conversion.IsReference)
            {
                // TODO: ensure from/to is valid reference type
                cg.EmitCastClass(to);
            }
            else if (conversion.IsUserDefined)
            {
                var method = (MethodSymbol)conversion.MethodSymbol;
                var ps = method.Parameters;
                int pconsumed = 0;

                if (method.HasThis)
                {
                    if (from.IsValueType) cg.EmitStructAddr(from);
                }
                else
                {
                    if (ps[0].RefKind != RefKind.None) throw new InvalidOperationException();
                    EmitImplicitConversion(cg, from, ps[0].Type);
                    pconsumed++;
                }

                // Context ctx, 
                if (ps.Length > pconsumed && SpecialParameterSymbol.IsContextParameter(ps[pconsumed]))
                {
                    cg.EmitLoadContext();
                    pconsumed++;
                }

                if (ps.Length != pconsumed) throw new InvalidOperationException();

                EmitImplicitConversion(cg, cg.EmitCall(ILOpCode.Call, method), to);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
