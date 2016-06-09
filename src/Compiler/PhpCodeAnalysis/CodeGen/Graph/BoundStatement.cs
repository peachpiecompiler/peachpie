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
            // nop
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

            //
            if (this.Returned == null)
            {
                if (cg.Routine.ReturnsVoid)
                {
                    // return;
                    cg.EmitRet(true);
                }
                else
                {
                    // return <default>;
                    cg.EmitRetDefault();
                }
            }
            else
            {
                if (cg.Routine.ReturnsVoid)
                {
                    // <expr>;
                    // return;
                    cg.EmitPop(this.Returned.Emit(cg));
                    cg.EmitRet(true);
                }
                else
                {
                    // return (T)<expr>;
                    cg.EmitConvert(this.Returned, cg.Routine.ReturnType);
                    cg.EmitRet(false);
                }
            }
        }
    }

    partial class BoundFunctionDeclStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(Pchp.Syntax.Text.Span.FromBounds(this.FunctionDecl.EntireDeclarationSpan.Start, this.FunctionDecl.HeadingEndPosition));

            // <ctx>.DeclareFunction ...
            cg.EmitDeclareFunction(this.Function);
        }
    }

    partial class BoundStaticVariableStatement
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            foreach (var v in _variables)
            {
                var getmethod = cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(v._holder);
                var place = v._holderPlace;

                // Template: x = ctx.GetStatic<holder_x>()
                place.EmitStorePrepare(cg.Builder);

                cg.EmitLoadContext();
                cg.EmitCall(ILOpCode.Callvirt, getmethod);

                place.EmitStore(cg.Builder);

                // holder initialization routine
                EmitInit(cg.Module, cg.Diagnostics, cg.DeclaringCompilation, v._holder, (BoundExpression)v.InitialValue);

            }
        }

        void EmitInit(Emit.PEModuleBuilder module, DiagnosticBag diagnostic, PhpCompilation compilation, SynthesizedStaticLocHolder holder, BoundExpression initializer)
        {
            var loctype = holder.ValueField.Type;

            var constant = (initializer != null) ? initializer.ConstantValue : default(Optional<object>);

            if (initializer != null && !constant.HasValue)
            {
                // emit Init only if it needs Context

                holder.EmitInit(module, (il) =>
                {
                    var cg = new CodeGenerator(il, module, diagnostic, OptimizationLevel.Release, false,
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
                if (initializer == null || constant.HasValue)
                {
                    // emit default value only if it won't be initialized by Init above

                    var cg = new CodeGenerator(il, module, diagnostic, OptimizationLevel.Release, false,
                        holder.ContainingType, null, new ArgPlace(holder, 0));

                    var valuePlace = new FieldPlace(cg.ThisPlaceOpt, holder.ValueField);

                    // Template: this.value = default(T);

                    valuePlace.EmitStorePrepare(il);
                    if (constant.HasValue)
                    {
                        cg.EmitConvert(cg.EmitLoadConstant(constant.Value, valuePlace.TypeOpt), 0, valuePlace.TypeOpt);
                    }
                    else
                    {
                        cg.EmitLoadDefaultValue(valuePlace.TypeOpt, 0);
                    }
                    valuePlace.EmitStore(il);
                }

                //
                il.EmitRet(true);
            });
        }
    }

    partial class BoundUnset
    {
        internal override void Emit(CodeGenerator cg)
        {
            cg.EmitSequencePoint(this.PhpSyntax);

            this.VarReferences.ForEach(cg.EmitUnset);
        }
    }
}
