using Microsoft.CodeAnalysis;
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
            // "static void Main()"
            var realmethod = new SynthesizedMethodSymbol(this.ScriptType, "Main", true, false, _compilation.CoreTypes.Void);

            //
            var body = MethodGenerator.GenerateMethodBody(this, realmethod,
                (il) =>
                {
                    var types = this.Compilation.CoreTypes;

                    // AddScriptReference<Script>()
                    var AddScriptReferenceMethod = (MethodSymbol)this.Compilation.CoreMethods.Context.AddScriptReference_TScript.Symbol.Construct(this.ScriptType);
                    il.EmitCall(this, diagnostic, ILOpCode.Call, AddScriptReferenceMethod);

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
                            il.EmitCall(this, diagnostic, ILOpCode.Call, this.Compilation.Context_Globals.GetMethod)
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

                    // ctx.Dispose
                    il.EmitLocalLoad(ctx_loc);
                    il.EmitOpCode(ILOpCode.Call, -1);
                    il.EmitToken(this.Compilation.CoreMethods.Context.Dispose.Symbol, null, diagnostic);

                    // return
                    il.EmitRet(true);
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
            var functions = GlobalSemantics.ResolveExtensionContainers(this.Compilation)
                .SelectMany(c => c.GetMembers().OfType<MethodSymbol>())
                .Where(GlobalSemantics.IsFunction);

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
        /// Emit body of enumeration of scripts Main function.
        /// </summary>
        internal void CreateEnumerateScriptsSymbol(DiagnosticBag diagnostic)
        {
            var method = this.ScriptType.EnumerateScriptsSymbol;
            var files = this.Compilation.SourceSymbolTables.GetFiles();

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
            // var consts = this.Compilation. ...

            // void (Action<string, RuntimeMethodHandle> callback)
            var body = MethodGenerator.GenerateMethodBody(this, method,
                (il) =>
                {
                    var action_string_method = method.Parameters[0].Type;
                    Debug.Assert(action_string_method.Name == "Action");
                    var invoke = action_string_method.DelegateInvokeMethod();
                    Debug.Assert(invoke != null);

                    // TODO:
                    //foreach (var c in consts)
                    //{
                    //    // callback.Invoke(c.Name, c.Value, c.IgnoreCase)
                    //    il.EmitLoadArgumentOpcode(0);
                    //    il.EmitStringConstant(c.Name);
                    //    // c.Value
                    //    // c.IgnoreCase
                    //    il.EmitCall(this, diagnostic, ILOpCode.Callvirt, invoke);
                    //}

                    //
                    il.EmitRet(true);
                },
                null, diagnostic, false);

            SetMethodBody(method, body);
        }
    }
}
