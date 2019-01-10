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

                    // AddScriptReference<Script>()
                    var AddScriptReferenceMethod = (MethodSymbol)methods.Context.AddScriptReference_TScript.Symbol.Construct(this.ScriptType);
                    il.EmitCall(this, diagnostic, ILOpCode.Call, AddScriptReferenceMethod);

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
                        // CreateConsole(mainscript, args)
                        MethodSymbol create_method = types.Context.Symbol.LookupMember<MethodSymbol>("CreateConsole");
                        Debug.Assert(create_method != null);
                        Debug.Assert(create_method.ParameterCount == 2);
                        Debug.Assert(create_method.Parameters[0].Type == types.String);
                        Debug.Assert(create_method.Parameters[1].Type == args_place.Type);

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

        /// <summary>
        /// Emit body of enumeration of referenced functions.
        /// </summary>
        internal void CreateEnumerateReferencedFunctions(DiagnosticBag diagnostic)
        {
            var method = this.ScriptType.EnumerateBuiltinFunctionsSymbol;

            // void (Action<string, RuntimeMethodHandle> callback)
            var body = MethodGenerator.GenerateMethodBody(this, method,
                (il) =>
                {
                    var functions = this.Compilation.GlobalSemantics
                        .ExportedFunctions
                        .SelectMany(pair => pair.Value)
                        .Where(m => !m.ContainingType.IsPhpSourceFile()) // not PHP source file containers
                        ;

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
        internal void CreateBuiltinTypes(DiagnosticBag diagnostic)
        {
            var method = this.ScriptType.EnumerateBuiltinTypesSymbol;
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
                        if (t.IsPhpUserType())
                        {
                            continue;   // the type is a PHP user type, do not export as app type
                        }

                        // callback.Invoke(t)
                        il.EmitLoadArgumentOpcode(0);
                        il.EmitCall(this, diagnostic, ILOpCode.Call, Compilation.CoreMethods.Dynamic.GetPhpTypeInfo_T.Symbol.Construct(t));
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

            // void (Action<string, MainDelegate> callback)
            var body = MethodGenerator.GenerateMethodBody(this, method,
                (il) =>
                {
                    var action_string_delegate = method.Parameters[0].Type;
                    Debug.Assert(action_string_delegate.Name == "Action");
                    var invoke = action_string_delegate.DelegateInvokeMethod();
                    Debug.Assert(invoke != null);

                    var ct = this.Compilation.CoreTypes;
                    var maindelegate_ctor = ct.MainDelegate.Ctor(ct.Object, ct.IntPtr);
                    var files = this.Compilation.GlobalSemantics.ExportedScripts;
                    foreach (var f in files)
                    {
                        if (f is SourcePharEntrySymbol)
                        {
                            // PHAR entries are reflected by runtime lazily
                            continue;
                        }

                        // MainDelegate ~ PhpValue <Main>(...)
                        var main =
                            (f.MainMethod as SourceGlobalMethodSymbol)?._mainMethod0 // wrapper that returns PhpValue
                            ?? f.GetMembers(WellKnownPchpNames.GlobalRoutineName + "`0")
                                .OfType<MethodSymbol>()
                                .SingleOrDefault() // wrapper that returns PhpValue in PE
                            ?? f.MainMethod; // compiled main method, returns PhpValue is there is no wrapper

                        Debug.Assert(main != null);
                        Debug.Assert(main.ReturnType.Equals(ct.PhpValue.Symbol));

                        // callback.Invoke(f.Name, new MainDelegate(null, main)):

                        // LOAD callback
                        il.EmitLoadArgumentOpcode(0);

                        // path : string
                        il.EmitStringConstant(f.RelativeFilePath);

                        // new MainDelegate(null, main)
                        il.EmitNullConstant();
                        il.EmitOpCode(ILOpCode.Ldftn);
                        il.EmitSymbolToken(this, diagnostic, (MethodSymbol)main, null); // main
                        il.EmitCall(this, diagnostic, ILOpCode.Newobj, maindelegate_ctor);

                        //
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
            var consts = this.Compilation.GlobalSemantics.GetExportedConstants();

            // void (Action<string, RuntimeMethodHandle> callback)
            var body = MethodGenerator.GenerateMethodBody(this, method,
                (il) =>
                {
                    var cg = new CodeGenerator(il, this, diagnostic, this.Compilation.Options.OptimizationLevel, false, this.ScriptType, null, null);

                    // interface IConstantsComposition
                    var constantscomposition = method.Parameters[0].Type;
                    Debug.Assert(constantscomposition.Name == "IConstantsComposition");

                    // Define(string, ...)
                    var define_xxx = constantscomposition
                    .GetMembers("Define")
                    .OfType<MethodSymbol>()
                    .Where(m => m.ParameterCount == 2)
                    .ToArray();

                    var define_string = define_xxx.Single(m => m.Parameters[1].Type.SpecialType == SpecialType.System_String);
                    var define_long = define_xxx.Single(m => m.Parameters[1].Type.SpecialType == SpecialType.System_Int64);
                    var define_double = define_xxx.Single(m => m.Parameters[1].Type.SpecialType == SpecialType.System_Double);
                    var define_func = define_xxx.Single(m => m.Parameters[1].Type.Name == "Func"); // Func<PhpValue>
                    var define_value = define_xxx.Single(m => m.Parameters[1].Type == cg.CoreTypes.PhpValue);

                    foreach (var c in consts.OfType<Symbol>())
                    {
                        // composer.Define(c.Name, c.Value)
                        il.EmitLoadArgumentOpcode(0);

                        // string : name
                        il.EmitStringConstant(c.MetadataName);

                        if (c is FieldSymbol fld)
                        {
                            // PhpValue : value
                            TypeSymbol consttype;

                            var constvalue = fld.GetConstantValue(false);
                            if (constvalue != null)
                            {
                                consttype = cg.EmitLoadConstant(constvalue.Value, cg.CoreTypes.PhpValue);
                            }
                            else
                            {
                                consttype = new FieldPlace(null, fld, this).EmitLoad(il);
                            }

                            // Define(...)
                            if (consttype.SpecialType == SpecialType.System_Int32)
                            {
                                // i4 -> i8
                                il.EmitOpCode(ILOpCode.Conv_i8);
                                consttype = cg.CoreTypes.Long;
                            }

                            MethodSymbol define_method = null;

                            if (consttype.SpecialType == SpecialType.System_String) { define_method = define_string; }
                            else if (consttype.SpecialType == SpecialType.System_Int64) { define_method = define_long; }
                            else if (consttype.SpecialType == SpecialType.System_Double) { define_method = define_double; }
                            else
                            {
                                cg.EmitConvertToPhpValue(consttype, 0);
                                define_method = define_value;
                            }

                            il.EmitCall(this, diagnostic, ILOpCode.Callvirt, define_method);
                        }
                        else if (c is PropertySymbol prop)
                        {
                            MethodSymbol getter_func;
                            // Func<PhpValue>
                            if (prop.Type == cg.CoreTypes.PhpValue)
                            {
                                getter_func = prop.GetMethod;
                            }
                            else
                            {
                                // static PhpValue get_XXX => get_prop();
                                getter_func = new SynthesizedMethodSymbol(ScriptType, "get_" + prop.Name, true, false, cg.CoreTypes.PhpValue, Accessibility.Internal);
                                SynthesizedManager.AddMethod(ScriptType, getter_func);
                                SetMethodBody(getter_func, MethodGenerator.GenerateMethodBody(this, getter_func, _il =>
                                {
                                    var _cg = new CodeGenerator(_il, this, diagnostic, this.Compilation.Options.OptimizationLevel, false, ScriptType, null, null);

                                    _cg.EmitRet(_cg.EmitConvertToPhpValue(_cg.EmitForwardCall(prop.GetMethod, getter_func, callvirt: false), 0));

                                }, null, diagnostic, false));
                            }

                            // new Func<PhpValue>(object @object = null, IntPtr method = getter_func)
                            cg.Builder.EmitNullConstant(); // null
                            cg.EmitOpCode(ILOpCode.Ldftn); // method
                            cg.EmitSymbolToken(getter_func, null);
                            cg.EmitCall(ILOpCode.Newobj, Compilation.GetWellKnownType(WellKnownType.System_Func_T).Construct(cg.CoreTypes.PhpValue).InstanceConstructors[0]);

                            //
                            il.EmitCall(this, diagnostic, ILOpCode.Callvirt, define_func);
                        }
                        else
                        {
                            throw ExceptionUtilities.UnexpectedValue(c);
                        }
                    }

                    //
                    il.EmitRet(true);
                },
                null, diagnostic, false);

            SetMethodBody(method, body);
        }
    }
}
