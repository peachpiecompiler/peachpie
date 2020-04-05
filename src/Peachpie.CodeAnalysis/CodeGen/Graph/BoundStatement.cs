using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Text;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Semantics
{
    partial class BoundStatement : IGenerator
    {
        internal virtual void Emit(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }

        void IGenerator.Generate(CodeGenerator cg) => Emit(cg);
    }

    partial class BoundEmptyStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            if (cg.EmitPdbSequencePoints && !_span.IsEmpty)
            {
                cg.EmitSequencePoint(_span);
            }
        }
    }

    partial class BoundExpressionStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            if (Expression.IsConstant() == false)
            {
                cg.EmitSequencePoint(this.PhpSyntax);
                cg.EmitPop(Expression.Emit(cg));
                
                //
                if (cg.EmitPdbSequencePoints)
                {
                    cg.Builder.EmitOpCode(ILOpCode.Nop);
                }
            }
        }
    }

    partial class BoundReturnStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            // if generator method -> return via storing the value in generator
            if (cg.Routine.IsGeneratorMethod())
            {
                // g._returnValue = <returned expression>
                if (this.Returned != null)
                {
                    cg.EmitGeneratorInstance();
                    var t = cg.Emit(this.Returned);
                    cg.EmitConvertToPhpValue(t, 0);
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorReturnedValue_Generator_PhpValue);
                }

                // g._state = -2 (closed): got to the end of the generator method
                // .ret
                cg.EmitRet(cg.CoreTypes.Void, forceJumpToExit: true);
                return;
            }

            var rtype = cg.Routine.ReturnType;

            //
            if (this.Returned == null)
            {
                if (!rtype.IsVoid())
                {
                    // <default>
                    cg.EmitLoadDefault(rtype, cg.Routine.ResultTypeMask);
                }
            }
            else
            {
                cg.EmitConvert(this.Returned, rtype);

                // TODO: check for null, if return type is not nullable
                if (cg.Routine.SyntaxReturnType != null && cg.Routine.SyntaxReturnType.IsNullable() == false)
                {
                    //// Template: Debug.Assert( <STACK> != null )
                    //cg.Builder.EmitOpCode(ILOpCode.Dup);
                    //cg.EmitNotNull(rtype, this.Returned.TypeRefMask);
                    //cg.EmitDebugAssert();
                }
            }

            // .ret
            cg.EmitRet(rtype);
        }
    }

    partial class BoundThrowStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            //
            cg.EmitConvert(Thrown, cg.CoreTypes.Exception);

            // throw <stack>;
            cg.Builder.EmitThrow(false);
        }
    }

    partial class BoundFunctionDeclStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.FunctionDecl.HeadingSpan);

            // <ctx>.DeclareFunction ...
            cg.EmitDeclareFunction(this.Function);
        }
    }

    partial class BoundTypeDeclStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.TypeDecl.HeadingSpan);

            // <ctx>.DeclareType<T>()
            cg.EmitDeclareType(this.DeclaredType);
        }
    }

    partial class BoundGlobalVariableStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            // Template: <local> = $GLOBALS.EnsureItemAlias("name")

            var access = BoundAccess.Write.WithWriteRef(default);
            var local = this.Variable.BindPlace(cg);
            var lhs = local.EmitStorePreamble(cg, access);

            // <ctx>.Globals : PhpArray
            cg.EmitLoadContext();
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Context.Globals.Getter);

            // PhpArray.EnsureItemAlias( name ) : PhpAlias
            this.Variable.Name.EmitIntStringKey(cg);
            var t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemAlias_IntStringKey)
                .Expect(cg.CoreTypes.PhpAlias);

            //
            local.EmitStore(cg, ref lhs, t, access);

            lhs.Dispose();
        }
    }

    partial class BoundStaticVariableStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            // synthesize the holder class H { PhpAlias value }
            var holder = _holderClass;
            cg.Module.SynthesizedManager.AddNestedType(holder.ContainingType, holder);

            // Context.GetStatic<H>()
            var getmethod = cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(holder);

            // Template: <local> = &Context.GetStatic<H>().value
            var local = this.Declaration.Variable; // .BindPlace(cg.Builder, BoundAccess.Write.WithWriteRef(TypeRefMask.AnyType), 0);
            var access = BoundAccess.Write.WithWriteRef(default);
            var lhs = local.EmitStorePreamble(cg, access);

            cg.EmitLoadContext();   // <ctx>
            cg.EmitCall(ILOpCode.Callvirt, getmethod);  // .GetStatic<H>()
            cg.Builder.EmitOpCode(ILOpCode.Ldfld);  // .value
            cg.EmitSymbolToken(holder.ValueField, null);

            local.EmitStore(cg, ref lhs, holder.ValueField.Type, access);
            lhs.Dispose();

            // holder initialization routine
            EmitInit(cg.Module, cg.Diagnostics, cg.DeclaringCompilation, holder, Declaration.InitialValue, routine: cg.Routine);
        }

        void EmitInit(Emit.PEModuleBuilder module, DiagnosticBag diagnostic, PhpCompilation compilation, SynthesizedStaticLocHolder holder, BoundExpression initializer, SourceRoutineSymbol routine)
        {
            var requiresContext = initializer != null && initializer.RequiresContext;

            if (requiresContext)
            {
                // emit Init only if it needs Context

                holder.EmitInit(module, (il) =>
                {
                    var cg = new CodeGenerator(il, module, diagnostic, compilation.Options.OptimizationLevel, false,
                        holder.ContainingType, new ArgPlace(compilation.CoreTypes.Context, 1), new ArgPlace(holder, 0), routine: routine);

                    var valuePlace = new FieldPlace(cg.ThisPlaceOpt, holder.ValueField, module);

                    // Template: this.value = <initilizer>;

                    valuePlace.EmitStorePrepare(il);
                    cg.EmitConvert(initializer, valuePlace.Type);
                    valuePlace.EmitStore(il);

                    //
                    il.EmitRet(true);
                });
            }

            // default .ctor
            holder.EmitCtor(module, (il) =>
            {
                if (!requiresContext)
                {
                    // emit default value only if it won't be initialized by Init above

                    var cg = new CodeGenerator(il, module, diagnostic, compilation.Options.OptimizationLevel, false,
                        holder.ContainingType, null, new ArgPlace(holder, 0), routine: routine);

                    var valuePlace = new FieldPlace(cg.ThisPlaceOpt, holder.ValueField, module);

                    // Template: this.value = default(T);

                    valuePlace.EmitStorePrepare(il);
                    if (initializer != null)
                    {
                        cg.EmitConvert(initializer, valuePlace.Type);
                    }
                    else
                    {
                        cg.EmitLoadDefault(valuePlace.Type, 0);
                    }
                    valuePlace.EmitStore(il);
                }

                // base..ctor()
                var ctor = holder.BaseType.InstanceConstructors.Single();
                il.EmitLoadArgumentOpcode(0);   // this
                il.EmitCall(module, diagnostic, ILOpCode.Call, ctor);   // .ctor()

                //
                il.EmitRet(true);
            });
        }
    }

    partial class BoundGlobalConstDeclStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            // Template: internal static int <const>Name;
            var idxfield = cg.Module.SynthesizedManager.GetGlobalConstantIndexField(Name.ToString());

            // Template: Operators.DeclareConstant(ctx, Name, ref idx, Value)
            cg.EmitLoadContext();
            cg.Builder.EmitStringConstant(Name.ToString());
            cg.EmitFieldAddress(idxfield);
            cg.EmitConvert(Value, cg.CoreTypes.PhpValue);
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.DeclareConstant_Context_string_int_PhpValue)
                .Expect(SpecialType.System_Void);
        }
    }

    partial class BoundUnset
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);
            EmitUnset(cg, Variable);
        }

        static void EmitUnset(CodeGenerator cg, BoundReferenceExpression expr)
        {
            if (!expr.Access.IsUnset)
                throw new ArgumentException();

            var place = expr.BindPlace(cg);
            Debug.Assert(place != null);

            var lhs = place.EmitStorePreamble(cg, BoundAccess.Unset);
            place.EmitStore(cg, ref lhs, null, BoundAccess.Unset); // null type -> no value
        }
    }

    partial class BoundYieldStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            Debug.Assert(cg.Routine.ControlFlowGraph.Yields != null);

            var il = cg.Builder;

            // sets currValue, currKey and userKeyReturned

            // Template: Operators.SetGeneratorCurrent(generator, value [,key])
            cg.EmitGeneratorInstance();             // generator
            cg.EmitConvertToPhpValue(YieldedValue); // value (can be NULL)

            if (YieldedKey == null)
            {
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorCurrent_Generator_PhpValue)
                    .Expect(SpecialType.System_Void);
            }
            else
            {
                cg.EmitConvertToPhpValue(cg.Emit(YieldedKey), YieldedKey.TypeRefMask);

                var setcurrent = this.IsYieldFrom
                    ? cg.CoreMethods.Operators.SetGeneratorCurrentFrom_Generator_PhpValue_PhpValue  // does not update auto-incremented key
                    : cg.CoreMethods.Operators.SetGeneratorCurrent_Generator_PhpValue_PhpValue;     // updates Generator max key


                cg.EmitCall(ILOpCode.Call, setcurrent).Expect(SpecialType.System_Void);
            }

            //generator._state = yieldIndex
            cg.EmitGeneratorInstance();
            il.EmitIntConstant(YieldIndex);
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorState_Generator_int);

            // return & set continuation point just after that
            il.EmitRet(true);
            il.MarkLabel(this);

            // Operators.HandleGeneratorException(generator)
            cg.EmitGeneratorInstance();
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.HandleGeneratorException_Generator);
        }
    }

    partial class BoundDeclareStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
        }
    }
}
