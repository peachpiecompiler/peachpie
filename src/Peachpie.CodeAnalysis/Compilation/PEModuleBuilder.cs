using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics.Model;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
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
        static string EntryPointScriptName(MethodSymbol method)
        {
            if (method is SourceRoutineSymbol routine)
            {
                return routine.ContainingFile.RelativeFilePath;
            }

            return string.Empty;
        }

        /// <summary>
        /// Create real CIL entry point, where it calls given method.
        /// </summary>
        internal void CreateEntryPoint(MethodSymbol method, DiagnosticBag diagnostic)
        {
            // "static int Main(string[] args)"
            var realmethod = new SynthesizedMethodSymbol(this.ScriptType, "Main", true, false, _compilation.CoreTypes.Int32, Accessibility.Private);
            realmethod.SetParameters(new SynthesizedParameterSymbol(realmethod, ArrayTypeSymbol.CreateSZArray(this.Compilation.SourceAssembly, this.Compilation.CoreTypes.String), 0, RefKind.None, "args"));

            //
            var body = MethodGenerator.GenerateMethodBody(this, realmethod,
                (il) =>
                {
                    var types = this.Compilation.CoreTypes;
                    var methods = this.Compilation.CoreMethods;
                    var args_place = new ParamPlace(realmethod.Parameters[0]);

                    // int exitcode = 0;
                    var exitcode_loc = il.LocalSlotManager.AllocateSlot(types.Int32.Symbol, LocalSlotConstraints.None);
                    il.EmitIntConstant(0);
                    il.EmitLocalStore(exitcode_loc);

                    // create Context
                    var ctx_loc = il.LocalSlotManager.AllocateSlot(types.Context.Symbol, LocalSlotConstraints.None);
                    var ex_loc = il.LocalSlotManager.AllocateSlot(types.Exception.Symbol, LocalSlotConstraints.None);
                    var onUnhandledException_method = types.Context.Symbol.LookupMember<MethodSymbol>("OnUnhandledException");

                    if (_compilation.Options.OutputKind == OutputKind.ConsoleApplication)
                    {
                        // CreateConsole(string mainscript, params string[] args)
                        var create_method = types.Context.Symbol.LookupMember<MethodSymbol>("CreateConsole", m =>
                        {
                            return
                                m.ParameterCount == 2 &&
                                m.Parameters[0].Type == types.String &&     // string mainscript
                                m.Parameters[1].Type == args_place.Type;    // params string[] args
                        });
                        Debug.Assert(create_method != null);
                        
                        il.EmitStringConstant(EntryPointScriptName(method));    // mainscript
                        args_place.EmitLoad(il);                                // args

                        il.EmitOpCode(ILOpCode.Call, +2);
                        il.EmitToken(create_method, null, diagnostic);
                    }
                    else
                    {
                        // CreateEmpty(args)
                        MethodSymbol create_method = types.Context.Symbol.LookupMember<MethodSymbol>("CreateEmpty");
                        Debug.Assert(create_method != null);
                        Debug.Assert(create_method.ParameterCount == 1);
                        Debug.Assert(create_method.Parameters[0].Type == args_place.Type);
                        args_place.EmitLoad(il);    // args
                        il.EmitOpCode(ILOpCode.Call, +1);
                        il.EmitToken(create_method, null, diagnostic);
                    }

                    il.EmitLocalStore(ctx_loc);

                    // Template:
                    // try { Main(...); } catch (ScriptDiedException) { } catch (Exception) { ... } finally { ctx.Dispose(); }

                    il.OpenLocalScope(ScopeType.TryCatchFinally);   // try { try ... } finally {}
                    il.OpenLocalScope(ScopeType.Try);
                    {
                        // IL requires catches and finally block to be distinct try

                        il.OpenLocalScope(ScopeType.TryCatchFinally);   // try {} catch (ScriptDiedException) {}
                        il.OpenLocalScope(ScopeType.Try);
                        {
                            // emit .call method;
                            if (method.HasThis)
                            {
                                throw new NotImplementedException();    // TODO: create instance of ContainingType
                            }

                            // params
                            foreach (var p in method.Parameters)
                            {
                                switch (p.Name)
                                {
                                    case SpecialParameterSymbol.ContextName:
                                        // <ctx>
                                        il.EmitLocalLoad(ctx_loc);
                                        break;
                                    case SpecialParameterSymbol.LocalsName:
                                        // <ctx>.Globals
                                        il.EmitLocalLoad(ctx_loc);
                                        il.EmitCall(this, diagnostic, ILOpCode.Call, methods.Context.Globals.Getter)
                                            .Expect(p.Type);
                                        break;
                                    case SpecialParameterSymbol.ThisName:
                                        // null
                                        il.EmitNullConstant();
                                        break;
                                    case SpecialParameterSymbol.SelfName:
                                        // default(RuntimeTypeHandle)
                                        var runtimetypehandle_loc = il.LocalSlotManager.AllocateSlot(types.RuntimeTypeHandle.Symbol, LocalSlotConstraints.None);
                                        il.EmitValueDefault(this, diagnostic, runtimetypehandle_loc);
                                        il.LocalSlotManager.FreeSlot(runtimetypehandle_loc);
                                        break;
                                    default:
                                        throw new ArgumentException(p.Name);
                                }
                            }

                            if (il.EmitCall(this, diagnostic, ILOpCode.Call, method).SpecialType != SpecialType.System_Void)
                                il.EmitOpCode(ILOpCode.Pop);
                        }
                        il.CloseLocalScope();   // /Try

                        il.AdjustStack(1); // Account for exception on the stack.
                        il.OpenLocalScope(ScopeType.Catch, types.ScriptDiedException.Symbol);
                        {
                            // exitcode = <exception>.ProcessStatus(ctx)
                            il.EmitLocalLoad(ctx_loc);
                            il.EmitCall(this, diagnostic, ILOpCode.Callvirt, types.ScriptDiedException.Symbol.LookupMember<MethodSymbol>("ProcessStatus"));
                            il.EmitLocalStore(exitcode_loc);
                        }
                        il.CloseLocalScope();   // /Catch
                        if (onUnhandledException_method != null) // only if runtime defines the method (backward compat.)
                        {
                            il.OpenLocalScope(ScopeType.Catch, types.Exception.Symbol);
                            {
                                // <ex_loc> = <stack>
                                il.EmitLocalStore(ex_loc);

                                // <ctx_loc>.OnUnhandledException( <ex_loc> ) : bool
                                il.EmitLocalLoad(ctx_loc);
                                il.EmitLocalLoad(ex_loc);
                                il.EmitCall(this, diagnostic, ILOpCode.Callvirt, onUnhandledException_method)
                                    .Expect(SpecialType.System_Boolean);

                                // if ( !<bool> )
                                // {
                                var lbl_end = new object();
                                il.EmitBranch(ILOpCode.Brtrue, lbl_end);

                                // rethrow <ex_loc>;
                                il.EmitLocalLoad(ex_loc);
                                il.EmitThrow(true);

                                // }
                                il.MarkLabel(lbl_end);
                            }
                            il.CloseLocalScope();   // /Catch
                        }
                        il.CloseLocalScope();   // /TryCatch
                    }
                    il.CloseLocalScope();   // /Try

                    il.OpenLocalScope(ScopeType.Finally);
                    {
                        // ctx.Dispose
                        il.EmitLocalLoad(ctx_loc);
                        il.EmitOpCode(ILOpCode.Call, -1);
                        il.EmitToken(methods.Context.Dispose.Symbol, null, diagnostic);
                    }
                    il.CloseLocalScope();   // /Finally
                    il.CloseLocalScope();   // /TryCatch

                    // return ctx.ExitCode
                    il.EmitLocalLoad(exitcode_loc);
                    il.EmitRet(false);
                },
                null, diagnostic, false);

            SetMethodBody(realmethod, body);

            //
            this.ScriptType.EntryPointSymbol = realmethod;
        }

        /// <summary>
        /// Emits body of scripts main wrapper converting main result to <c>PhpValue</c>.
        /// </summary>
        /// <param name="wrapper">&lt;Main&gt;`0 method, that calls real Main.</param>
        /// <param name="main">Real scripts main method.</param>
        /// <param name="diagnostic">DiagnosticBag.</param>
        internal void CreateMainMethodWrapper(MethodSymbol wrapper, MethodSymbol main, DiagnosticBag diagnostic)
        {
            if (wrapper == null)
                return;

            // generate body of <wrapper> calling <main>
            Debug.Assert(wrapper.IsStatic);
            Debug.Assert(main.IsStatic);

            Debug.Assert(wrapper.ReturnType == _compilation.CoreTypes.PhpValue);
            Debug.Assert(wrapper.ParameterCount == main.ParameterCount);

            //
            var body = MethodGenerator.GenerateMethodBody(this, wrapper,
                (il) =>
                {
                    using (var cg = new CodeGenerator(il, this, diagnostic, this.Compilation.Options.OptimizationLevel, false, main.ContainingType, null, null))
                    {
                        // load arguments
                        foreach (var p in main.Parameters)
                        {
                            il.EmitLoadArgumentOpcode(p.Ordinal);
                        }

                        // call <Main>
                        var result = il.EmitCall(this, diagnostic, ILOpCode.Call, main);

                        // convert result to PhpValue
                        cg.EmitConvertToPhpValue(result, 0);

                        il.EmitRet(false);
                    }
                },
                null, diagnostic, false);

            SetMethodBody(wrapper, body);
        }
    }
}
