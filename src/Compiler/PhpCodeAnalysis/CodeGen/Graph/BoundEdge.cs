using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class Edge : IGenerator
    {
        /// <summary>
        /// Generates or enqueues next blocks to the worklist.
        /// </summary>
        internal abstract void Generate(CodeGenerator cg);

        void IGenerator.Generate(CodeGenerator cg) => this.Generate(cg);
    }

    partial class SimpleEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            cg.Scope.ContinueWith(NextBlock);
        }
    }

    partial class ConditionalEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            Contract.ThrowIfNull(Condition);

            if (IsLoop) // perf
            {
                cg.Builder.EmitBranch(ILOpCode.Br, this.Condition);

                // {
                cg.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }

                // if (Condition)
                cg.Builder.MarkLabel(this.Condition);
                cg.EmitConvert(this.Condition, cg.CoreTypes.Boolean);
                cg.Builder.EmitBranch(ILOpCode.Brtrue, TrueTarget);
            }
            else
            {
                // if (Condition)
                cg.EmitConvert(this.Condition, cg.CoreTypes.Boolean);
                cg.Builder.EmitBranch(ILOpCode.Brfalse, FalseTarget);

                // {
                cg.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }
            }

            cg.Scope.ContinueWith(FalseTarget);
        }
    }

    partial class TryCatchEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachEnumereeEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachMoveNextEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }
    }

    partial class SwitchEdge
    {
        static bool IsInt32(object value) => value is int || (value is long && (long)value <= int.MaxValue && (long)value >= int.MinValue);
        static bool IsString(object value) => value is string;

        internal override void Generate(CodeGenerator cg)
        {
            // four cases:
            // 1. just single or none case label that can be replaced with single IF
            // 2. switch over integers, using native CIL switch
            // 3. switch over strings, using C# static Dictionary and CIL switch
            // 4. PHP style switch which is just a bunch of IFs

            if (this.CaseBlocks.Length == 0 || this.CaseBlocks[0].IsDefault)
            {
                // no SWITCH or IF needed

                cg.EmitPop(this.SwitchValue.WithAccess(BoundAccess.None).Emit(cg)); // None Access, also using BoundExpression.Emit directly to avoid CodeGenerator type specialization which is not needed
                if (this.CaseBlocks.Length == 1)
                {
                    cg.GenerateScope(this.CaseBlocks[0], NextBlock.Ordinal);
                }
                else
                {
                    throw new InvalidOperationException();  // default case should be the last one
                }
            }
            else
            {
                // TODO: CIL Switch:
                //bool allconsts = this.CaseBlocks.All(c => c.IsDefault || c.CaseValue.ConstantValue.HasValue);
                //bool allconstints = allconsts && this.CaseBlocks.All(c => c.IsDefault || IsInt32(c.CaseValue.ConstantValue.Value));
                //bool allconststrings = allconsts && this.CaseBlocks.All(c => c.IsDefault || IsString(c.CaseValue.ConstantValue.Value));
                
                //if (allconstints)
                //{

                //}
                //else if (allconststrings)
                //{

                //}
                //else
                {
                    // <switch_loc> = <SwitchValue>;
                    var switch_type = cg.Emit(this.SwitchValue);
                    var switch_loc = cg.GetTemporaryLocal(switch_type);
                    cg.Builder.EmitLocalStore(switch_loc);

                    //
                    for (int i = 0; i < this.CaseBlocks.Length; i++)
                    {
                        var this_block = this.CaseBlocks[i];
                        var next_case = (i + 1 < this.CaseBlocks.Length) ? this.CaseBlocks[i + 1] : null;
                        object next_mark = (object)next_case?.CaseValue ?? next_case ?? NextBlock;
                        
                        if (!this_block.IsDefault)
                        {
                            // <CaseValue>:
                            cg.Builder.MarkLabel(this_block.CaseValue);

                            // if (<switch_loc> == c.CaseValue)
                            cg.Builder.EmitLocalLoad(switch_loc);
                            BoundBinaryEx.EmitEquality(cg, switch_type, this_block.CaseValue);
                            cg.Builder.EmitBranch(ILOpCode.Brfalse, next_mark);
                        }

                        // {
                        cg.GenerateScope(this_block, (next_case ?? NextBlock).Ordinal);
                        // }
                    }

                    cg.ReturnTemporaryLocal(switch_loc);
                }
            }

            //
            cg.Scope.ContinueWith(NextBlock);
        }
    }
}
