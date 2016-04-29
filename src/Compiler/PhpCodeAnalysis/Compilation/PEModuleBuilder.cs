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

namespace Pchp.CodeAnalysis.Emit
{
    partial class PEModuleBuilder
    {
        internal void CreateEntryPoint(MethodSymbol method, DiagnosticBag diagnostic)
        {
            // "static void Main()"
            var realmethod = new SynthesizedMethodSymbol(this.ScriptType, "Main", true, _compilation.CoreTypes.Void);

            //
            var body = MethodGenerator.GenerateMethodBody(this, realmethod,
                (il) =>
                {
                    var types = this.Compilation.CoreTypes;

                    // create Context
                    var ctx_loc = il.LocalSlotManager.AllocateSlot(types.Context.Symbol, LocalSlotConstraints.None);

                    // ctx_loc = Context.Create***()
                    MethodSymbol create_method = (_compilation.Options.OutputKind == OutputKind.ConsoleApplication)
                        ? _compilation.CoreMethods.Context.CreateConsole.Symbol // Context.CreateConsole()
                        : null;
                    Debug.Assert(create_method != null);
                    il.EmitOpCode(ILOpCode.Call, +1);
                    il.EmitToken(create_method, null, diagnostic);
                    il.EmitLocalStore(ctx_loc);

                    // emit .call method;
                    int stack = 0;
                    if (method.HasThis)
                    {
                        stack--;
                        throw new NotImplementedException();    // TODO: create instance of ContainingType
                    }

                    // params
                    foreach (var p in method.Parameters)
                    {
                        if (p.Type == types.Context && p.Name == SpecialParameterSymbol.ContextName)
                        {
                            // <ctx>
                            il.EmitLocalLoad(ctx_loc);
                        }
                        else if (p.Type == types.PhpArray && p.Name == SpecialParameterSymbol.LocalsName)
                        {
                            // <ctx>.Globals
                            il.EmitLocalLoad(ctx_loc);
                            il.EmitCall(this, diagnostic, ILOpCode.Call, this.Compilation.Context_Globals.GetMethod)
                                .Expect(p.Type);
                        }
                        else
                        {
                            throw new NotImplementedException();    // TODO: default parameter
                        }
                    }
                    stack -= method.ParameterCount;

                    il.EmitOpCode(ILOpCode.Call, stack);
                    il.EmitToken(method, null, diagnostic);

                    // ctx.Dispose
                    il.EmitLocalLoad(ctx_loc);
                    il.EmitOpCode(ILOpCode.Call, -1);
                    il.EmitToken(this.Compilation.CoreMethods.Context.Dispose.Symbol, null, diagnostic);

                    // return
                    if (!method.ReturnsVoid)
                        il.EmitOpCode(ILOpCode.Pop);

                    Debug.Assert(method.ReturnsVoid);
                    il.EmitRet(true);
                },
                null, diagnostic, false);
            SetMethodBody(realmethod, body);

            //
            this.ScriptType.EntryPointSymbol = realmethod;
        }
    }
}
