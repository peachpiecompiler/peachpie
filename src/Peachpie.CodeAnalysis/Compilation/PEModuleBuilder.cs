using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics.Model;
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

                    // AddScriptReference<Script>()
                    var AddScriptReferenceMethod = (MethodSymbol)methods.Context.AddScriptReference_TScript.Symbol.Construct(this.ScriptType);
                    il.EmitCall(this, diagnostic, ILOpCode.Call, AddScriptReferenceMethod);

                    // int exitcode = 0;
                    var exitcode_loc = il.LocalSlotManager.AllocateSlot(types.Int32.Symbol, LocalSlotConstraints.None);
                    il.EmitIntConstant(0);
                    il.EmitLocalStore(exitcode_loc);

                    // create Context
                    var ctx_loc = il.LocalSlotManager.AllocateSlot(types.Context.Symbol, LocalSlotConstraints.None);

                    // ctx_loc = Context.Create***(args)
                    var createMethodName = (_compilation.Options.OutputKind == OutputKind.ConsoleApplication) ? "CreateConsole" : "CreateEmpty";
                    MethodSymbol create_method = _compilation.CoreTypes.Context.Symbol.LookupMember<MethodSymbol>(createMethodName);
                    Debug.Assert(create_method != null);
                    Debug.Assert(create_method.ParameterCount == 1);
                    Debug.Assert(create_method.Parameters[0].Type == args_place.TypeOpt);
                    args_place.EmitLoad(il);
                    il.EmitOpCode(ILOpCode.Call, +1);
                    il.EmitToken(create_method, null, diagnostic);
                    il.EmitLocalStore(ctx_loc);

                    // Template:
                    // try { Main(...); } catch (ScriptDiedException) { } finally { ctx.Dispose; }

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
                                if (p.Type == types.Context && p.Name == SpecialParameterSymbol.ContextName)
                                {
                                    // <ctx>
                                    il.EmitLocalLoad(ctx_loc);
                                }
                                else if (p.Type == types.PhpArray && p.Name == SpecialParameterSymbol.LocalsName)
                                {
                                    // <ctx>.Globals
                                    il.EmitLocalLoad(ctx_loc);
                                    il.EmitCall(this, diagnostic, ILOpCode.Call, methods.Context.Globals.Getter)
                                        .Expect(p.Type);
                                }
                                else if (p.Type == types.Object && p.Name == SpecialParameterSymbol.ThisName)
                                {
                                    // null
                                    il.EmitNullConstant();
                                }
                                else
                                {
                                    throw new NotImplementedException();    // TODO: default parameter
                                }
                            }

                            if (il.EmitCall(this, diagnostic, ILOpCode.Call, method).SpecialType != SpecialType.System_Void)
                                il.EmitOpCode(ILOpCode.Pop);
                        }
                        il.CloseLocalScope();   // /Try

                        il.AdjustStack(1); // Account for exception on the stack.
                        il.OpenLocalScope(ScopeType.Catch, Compilation.CoreTypes.ScriptDiedException.Symbol);
                        {
                            // exitcode = <exception>.ProcessStatus(ctx)
                            il.EmitLocalLoad(ctx_loc);
                            il.EmitCall(this, diagnostic, ILOpCode.Callvirt, Compilation.CoreTypes.ScriptDiedException.Symbol.LookupMember<MethodSymbol>("ProcessStatus"));
                            il.EmitLocalStore(exitcode_loc);
                        }
                        il.CloseLocalScope();   // /Catch
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
                    // TODO: CodeGenerator.EmitConvertToPhpValue(cg.EmitCall(main))

                    // load arguments
                    foreach (var p in main.Parameters)
                    {
                        il.EmitLoadArgumentOpcode(p.Ordinal);
                    }

                    // call <Main>
                    var result = il.EmitCall(this, diagnostic, ILOpCode.Call, main);

                    // convert result to PhpValue
                    CodeGenerator.EmitConvertToPhpValue(result, 0, il, this, diagnostic);
                    il.EmitRet(false);
                },
                null, diagnostic, false);

            SetMethodBody(wrapper, body);
        }

        /// <summary>
        /// Emit body of enumeration of referenced functions.
        /// </summary>
        internal void CreateEnumerateReferencedFunctions(DiagnosticBag diagnostic)
        {
            var method = this.ScriptType.EnumerateReferencedFunctionsSymbol;
            var functions = GlobalSymbolProvider.ResolveExtensionContainers(this.Compilation)
                .SelectMany(c => c.GetMembers().OfType<MethodSymbol>())
                .Where(GlobalSymbolProvider.IsFunction);

            // void (Action<string, RuntimeMethodHandle> callback)
            var body = MethodGenerator.GenerateMethodBody(this, method,
                (il) =>
                {
                    var action_string_method = method.Parameters[0].Type;
                    Debug.Assert(action_string_method.Name == "Action");
                    var invoke = action_string_method.DelegateInvokeMethod();
                    Debug.Assert(invoke != null);

                    foreach (var f in functions)
                    {
                        // callback.Invoke(f.Name, f)
                        il.EmitLoadArgumentOpcode(0);
                        il.EmitStringConstant(f.Name);
                        il.EmitLoadToken(this, diagnostic, f, null);
                        il.EmitCall(this, diagnostic, ILOpCode.Callvirt, invoke);
                    }

                    //
                    il.EmitRet(true);
                },
                null, diagnostic, false);

            SetMethodBody(method, body);
        }

        /// <summary>
        /// Emit body of enumeration of referenced types.
        /// </summary>
        internal void CreateEnumerateReferencedTypes(DiagnosticBag diagnostic)
        {
            var method = this.ScriptType.EnumerateReferencedTypesSymbol;
            var types = this.Compilation.GlobalSemantics.GetReferencedTypes();

            // void (Action<string, RuntimeTypeHandle> callback)
            var body = MethodGenerator.GenerateMethodBody(this, method,
                (il) =>
                {
                    var action_string_method = method.Parameters[0].Type;
                    Debug.Assert(action_string_method.Name == "Action");
                    var invoke = action_string_method.DelegateInvokeMethod();
                    Debug.Assert(invoke != null);

                    foreach (var t in types)
                    {
                        // callback.Invoke(t.Name, t)
                        il.EmitLoadArgumentOpcode(0);
                        il.EmitStringConstant(t.Name);
                        il.EmitLoadToken(this, diagnostic, t, null);
                        il.EmitCall(this, diagnostic, ILOpCode.Callvirt, invoke);
                    }

                    //
                    il.EmitRet(true);
                },
                null, diagnostic, false);

            SetMethodBody(method, body);
        }

        /// <summary>
        /// Emit body of enumeration of scripts Main function.
        /// </summary>
        internal void CreateEnumerateScriptsSymbol(DiagnosticBag diagnostic)
        {
            var method = this.ScriptType.EnumerateScriptsSymbol;
            var files = this.Compilation.SourceSymbolCollection.GetFiles();

            // void (Action<string, RuntimeMethodHandle> callback)
            var body = MethodGenerator.GenerateMethodBody(this, method,
                (il) =>
                {
                    var action_string_method = method.Parameters[0].Type;
                    Debug.Assert(action_string_method.Name == "Action");
                    var invoke = action_string_method.DelegateInvokeMethod();
                    Debug.Assert(invoke != null);

                    foreach (var f in files)
                    {
                        // callback.Invoke(f.Name, f)
                        il.EmitLoadArgumentOpcode(0);
                        il.EmitStringConstant(f.RelativeFilePath);
                        il.EmitLoadToken(this, diagnostic, f.MainMethod, null);
                        il.EmitCall(this, diagnostic, ILOpCode.Callvirt, invoke);
                    }

                    //
                    il.EmitRet(true);
                },
                null, diagnostic, false);

            SetMethodBody(method, body);
        }

        /// <summary>
        /// Emit body of enumeration of app-wide global constants.
        /// </summary>
        internal void CreateEnumerateConstantsSymbol(DiagnosticBag diagnostic)
        {
            var method = this.ScriptType.EnumerateConstantsSymbol;
            var consts = this.Compilation.GlobalSemantics.GetExportedConstants().Cast<FieldSymbol>();   // TODO: PropertySymbol

            // void (Action<string, RuntimeMethodHandle> callback)
            var body = MethodGenerator.GenerateMethodBody(this, method,
                (il) =>
                {
                    var cg = new CodeGenerator(il, this, diagnostic, this.Compilation.Options.OptimizationLevel, false, this.ScriptType, null, null);

                    var action_string_method = method.Parameters[0].Type;
                    Debug.Assert(action_string_method.Name == "Action");
                    var invoke = action_string_method.DelegateInvokeMethod();
                    Debug.Assert(invoke != null);

                    foreach (var c in consts)
                    {
                        Debug.Assert(c.IsConst || (c.IsStatic && c.IsReadOnly));

                        // callback.Invoke(c.Name, c.Value, c.IgnoreCase)
                        il.EmitLoadArgumentOpcode(0);

                        // string : name
                        il.EmitStringConstant(c.MetadataName);

                        // PhpValue : value
                        TypeSymbol consttype;
                        var constvalue = c.GetConstantValue(false);
                        if (constvalue != null)
                        {
                            consttype = cg.EmitLoadConstant(constvalue.Value, cg.CoreTypes.PhpValue);
                        }
                        else
                        {
                            consttype = new FieldPlace(null, c).EmitLoad(il);
                        }
                        cg.EmitConvertToPhpValue(consttype, 0);

                        // bool : ignore case
                        il.EmitBoolConstant(false);

                        // Invoke(...)
                        il.EmitCall(this, diagnostic, ILOpCode.Callvirt, invoke);
                    }

                    //
                    il.EmitRet(true);
                },
                null, diagnostic, false);

            SetMethodBody(method, body);
        }
    }
}
