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
            if (cg.EmitPdbSequencePoints)
            {
                var span = _span;
                //if (span.IsEmpty && PhpSyntax != null)
                //{
                //    span = PhpSyntax.Span.ToTextSpan();
                //}

                if (!span.IsEmpty)
                {
                    cg.EmitSequencePoint(_span);
                    cg.Builder.EmitOpCode(ILOpCode.Nop);
                }
            }
        }
    }

    partial class BoundExpressionStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);
            cg.EmitPop(this.Expression.Emit(cg));
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
                cg.EmitGeneratorInstance();
                cg.Builder.EmitIntConstant(-2);
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorState_Generator_int);

                cg.Builder.EmitRet(true);
                return;
            }

            var rtype = cg.Routine.ReturnType;
            var rvoid = rtype.SpecialType == SpecialType.System_Void;

            //
            if (this.Returned == null)
            {
                if (rvoid)
                {
                    // <void>
                }
                else
                {
                    // <default>
                    cg.EmitLoadDefault(rtype, cg.Routine.ResultTypeMask);
                }
            }
            else
            {
                var t = cg.Emit(this.Returned);

                if (rvoid)
                {
                    // <expr>;
                    cg.EmitPop(t);
                }
                else
                {
                    if (cg.Routine.SyntaxSignature.AliasReturn)
                    {
                        Debug.Assert(rtype == cg.CoreTypes.PhpAlias);
                    }
                    else
                    {
                        // return by value
                        //if (this.Returned.TypeRefMask.IsRef)
                        {
                            // dereference
                            t = cg.EmitDereference(t);
                        }
                    }

                    // return (T)<expr>;
                    cg.EmitConvert(t, this.Returned.TypeRefMask, rtype);
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

            //var t = cg.Emit(Thrown);
            //if (t.IsReferenceType)
            //{
            //    //if (!t.IsEqualToOrDerivedFrom(cg.CoreTypes.Exception))
            //    //{
            //    //    throw new NotImplementedException();    // Wrap to System.Exception
            //    //}
            //    cg.EmitCastClass(t, cg.CoreTypes.Exception);
            //}
            //else
            //{
            //    //if (t == cg.CoreTypes.PhpValue)
            //    //{

            //    //}

            //    throw new NotImplementedException();    // Wrap to System.Exception
            //}

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
            cg.EmitDeclareType(this.Type);
        }
    }

    partial class BoundGlobalVariableStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            // Template: <local> = $GLOBALS.EnsureItemAlias("name")

            var local = this.Variable.BindPlace(cg);
            local.EmitStorePrepare(cg);

            // <ctx>.Globals : PhpArray
            cg.EmitLoadContext();
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Context.Globals.Getter);

            // PhpArray.EnsureItemAlias( name ) : PhpAlias
            this.Variable.Name.EmitIntStringKey(cg);
            var t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemAlias_IntStringKey)
                .Expect(cg.CoreTypes.PhpAlias);

            //
            local.EmitStore(cg, t);
        }
    }

    partial class BoundStaticVariableStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            // synthesize the holder class H { PhpAlias value }
            var holder = cg.Factory.DeclareStaticLocalHolder(this.Declaration.Name, cg.CoreTypes.PhpAlias); // (TypeSymbol)((ILocalSymbol)this.Declaration.Variable.Symbol).Type);

            // Context.GetStatic<H>()
            var getmethod = cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(holder);
            
            // Template: <local> = &Context.GetStatic<H>().value
            var local = this.Declaration.Variable.BindPlace(cg.Builder, BoundAccess.Write.WithWriteRef(TypeRefMask.AnyType), 0);
            local.EmitStorePrepare(cg);

            cg.EmitLoadContext();   // <ctx>
            cg.EmitCall(ILOpCode.Callvirt, getmethod);  // .GetStatic<H>()
            cg.Builder.EmitOpCode(ILOpCode.Ldfld);  // .value
            cg.EmitSymbolToken(holder.ValueField, null);

            local.EmitStore(cg, holder.ValueField.Type);

            // holder initialization routine
            EmitInit(cg.Module, cg.Diagnostics, cg.DeclaringCompilation, holder, Declaration.InitialValue);
        }

        void EmitInit(Emit.PEModuleBuilder module, DiagnosticBag diagnostic, PhpCompilation compilation, SynthesizedStaticLocHolder holder, BoundExpression initializer)
        {
            var requiresContext = initializer != null && initializer.RequiresContext;

            if (requiresContext)
            {
                // emit Init only if it needs Context

                holder.EmitInit(module, (il) =>
                {
                    var cg = new CodeGenerator(il, module, diagnostic, compilation.Options.OptimizationLevel, false,
                        holder.ContainingType, new ArgPlace(compilation.CoreTypes.Context, 1), new ArgPlace(holder, 0));

                    var valuePlace = new FieldPlace(cg.ThisPlaceOpt, holder.ValueField);

                    // Template: this.value = <initilizer>;

                    valuePlace.EmitStorePrepare(il);
                    cg.EmitConvert(initializer, valuePlace.TypeOpt);
                    valuePlace.EmitStore(il);

                    //
                    il.EmitRet(true);
                });
            }

            // default .ctor
            holder.EmitCtor(module, (il) =>
            {
                // base..ctor()
                var ctor = holder.BaseType.InstanceConstructors.Single();
                il.EmitLoadArgumentOpcode(0);   // this
                il.EmitCall(module, diagnostic, ILOpCode.Call, ctor);   // .ctor()

                if (!requiresContext)
                {
                    // emit default value only if it won't be initialized by Init above

                    var cg = new CodeGenerator(il, module, diagnostic, compilation.Options.OptimizationLevel, false,
                        holder.ContainingType, null, new ArgPlace(holder, 0));

                    var valuePlace = new FieldPlace(cg.ThisPlaceOpt, holder.ValueField);

                    // Template: this.value = default(T);

                    valuePlace.EmitStorePrepare(il);
                    if (initializer != null)
                    {
                        cg.EmitConvert(initializer, valuePlace.TypeOpt);
                    }
                    else
                    {
                        cg.EmitLoadDefault(valuePlace.TypeOpt, 0);
                    }
                    valuePlace.EmitStore(il);
                }

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

            place.EmitStorePrepare(cg);
            place.EmitStore(cg, null);
        }
    }

    partial class BoundYieldStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            Debug.Assert(cg.Routine.ControlFlowGraph.Yields != null);

            // yieldIndex is 1-based because zero is reserved for to-first-yield-run.
            var yieldIndex = Array.IndexOf(cg.Routine.ControlFlowGraph.Yields, this) + 1;
            Debug.Assert(yieldIndex >= 1);

            var il = cg.Builder;

            // sets currValue and currKey on generator object
            setAsPhpValueOnGenerator(cg, YieldedValue, cg.CoreMethods.Operators.SetGeneratorCurrValue_Generator_PhpValue);
            setAsPhpValueOnGenerator(cg, YieldedKey, cg.CoreMethods.Operators.SetGeneratorCurrKey_Generator_PhpValue);


            // generator._userKeyReturned = (YieldedKey != null)
            var userKeyReturned = (YieldedKey != null);
            cg.EmitGeneratorInstance();
            cg.Builder.EmitBoolConstant(userKeyReturned);
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorReturnedUserKey_Generator_bool);


            //generator._state = yieldIndex
            cg.EmitGeneratorInstance();
            il.EmitIntConstant(yieldIndex);
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetGeneratorState_Generator_int);


            // return & set continuation point just after that
            il.EmitRet(true);
            il.MarkLabel(this);

            // if(generator._currException != null) throw ex;
            cg.EmitGeneratorInstance();
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetGeneratorThrownException_Generator);

            var excNotNull = new NamedLabel("generator._currException == null");
            il.EmitBranch(ILOpCode.Brfalse, excNotNull);

            // load the exception to be thrown on stack (so it can be nulled)
            cg.EmitGeneratorInstance();
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetGeneratorThrownException_Generator);

            //g._curException = null : clear the field after throwing the exception
            cg.EmitGeneratorInstance();
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.NullGeneratorThrownException_Generator);

            il.EmitThrow(false);

            il.MarkLabel(excNotNull);

        }

        private void setAsPhpValueOnGenerator(CodeGenerator cg, BoundExpression valueExpr, CoreMethod setMethod)
        {
            cg.EmitGeneratorInstance();

            if (valueExpr == null)
            {
                cg.Emit_PhpValue_Null();
            }
            else
            {
                cg.EmitConvertToPhpValue(cg.Emit(valueExpr), valueExpr.TypeRefMask);
            }

            cg.EmitCall(ILOpCode.Call, setMethod);
        }
    }

    partial class BoundDeclareStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
        }
    }
}
