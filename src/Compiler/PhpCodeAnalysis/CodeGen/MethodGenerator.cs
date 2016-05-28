using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.CodeGen
{
    internal sealed class MethodGenerator
    {
        private static Cci.DebugSourceDocument CreateDebugDocumentForFile(string normalizedPath)
        {
            return new Cci.DebugSourceDocument(normalizedPath, Constants.CorSymLanguageTypePeachpie);
        }

        internal static MethodBody GenerateMethodBody(
            PEModuleBuilder moduleBuilder,
            SourceRoutineSymbol routine,
            int methodOrdinal,
            //ImmutableArray<LambdaDebugInfo> lambdaDebugInfo,
            //ImmutableArray<ClosureDebugInfo> closureDebugInfo,
            //StateMachineTypeSymbol stateMachineTypeOpt,
            VariableSlotAllocator variableSlotAllocatorOpt,
            DiagnosticBag diagnostics,
            //ImportChain importChainOpt,
            bool emittingPdb)
        {
            return GenerateMethodBody(moduleBuilder, routine, (builder) =>
            {
                DiagnosticBag diagnosticsForThisMethod = DiagnosticBag.GetInstance();
                var optimization = moduleBuilder.Compilation.Options.OptimizationLevel;
                var codeGen = new CodeGenerator(routine, builder, moduleBuilder, diagnosticsForThisMethod, optimization, emittingPdb);

                //if (diagnosticsForThisMethod.HasAnyErrors())
                //{
                //    // we are done here. Since there were errors we should not emit anything.
                //    return null;
                //}

                // We need to save additional debugging information for MoveNext of an async state machine.
                //var stateMachineMethod = method as SynthesizedStateMachineMethod;
                //bool isStateMachineMoveNextMethod = stateMachineMethod != null && method.Name == WellKnownMemberNames.MoveNextMethodName;

                //if (isStateMachineMoveNextMethod && stateMachineMethod.StateMachineType.KickoffMethod.IsAsync)
                //{
                //    int asyncCatchHandlerOffset;
                //    ImmutableArray<int> asyncYieldPoints;
                //    ImmutableArray<int> asyncResumePoints;
                //    codeGen.Generate(out asyncCatchHandlerOffset, out asyncYieldPoints, out asyncResumePoints);

                //    var kickoffMethod = stateMachineMethod.StateMachineType.KickoffMethod;

                //    // The exception handler IL offset is used by the debugger to treat exceptions caught by the marked catch block as "user unhandled".
                //    // This is important for async void because async void exceptions generally result in the process being terminated,
                //    // but without anything useful on the call stack. Async Task methods on the other hand return exceptions as the result of the Task.
                //    // So it is undesirable to consider these exceptions "user unhandled" since there may well be user code that is awaiting the task.
                //    // This is a heuristic since it's possible that there is no user code awaiting the task.
                //    asyncDebugInfo = new Cci.AsyncMethodBodyDebugInfo(kickoffMethod, kickoffMethod.ReturnsVoid ? asyncCatchHandlerOffset : -1, asyncYieldPoints, asyncResumePoints);
                //}
                //else
                {
                    codeGen.Generate();
                }
            }, variableSlotAllocatorOpt, diagnostics, emittingPdb);
        }

        /// <summary>
        /// Generates method body that calls another method.
        /// Used for wrapping a method call into a method, e.g. an entry point.
        /// </summary>
        internal static MethodBody GenerateMethodBody(
            PEModuleBuilder moduleBuilder,
            MethodSymbol routine,
            Action<ILBuilder> builder,
            VariableSlotAllocator variableSlotAllocatorOpt,
            DiagnosticBag diagnostics,
            bool emittingPdb)
        {
            var compilation = moduleBuilder.Compilation;
            var localSlotManager = new LocalSlotManager(variableSlotAllocatorOpt);
            var optimizations = compilation.Options.OptimizationLevel;

            DebugDocumentProvider debugDocumentProvider = null;

            if (emittingPdb)
            {
                debugDocumentProvider = (path, basePath) => moduleBuilder.GetOrAddDebugDocument(path, basePath, CreateDebugDocumentForFile);
            }

            ILBuilder il = new ILBuilder(moduleBuilder, localSlotManager, optimizations);
            try
            {
                Cci.AsyncMethodBodyDebugInfo asyncDebugInfo = null;

                builder(il);

                //
                il.Realize();

                //
                var localVariables = il.LocalSlotManager.LocalsInOrder();

                if (localVariables.Length > 0xFFFE)
                {
                    //diagnosticsForThisMethod.Add(ErrorCode.ERR_TooManyLocals, method.Locations.First());
                }

                //if (diagnosticsForThisMethod.HasAnyErrors())
                //{
                //    // we are done here. Since there were errors we should not emit anything.
                //    return null;
                //}

                //// We will only save the IL builders when running tests.
                //if (moduleBuilder.SaveTestData)
                //{
                //    moduleBuilder.SetMethodTestData(method, builder.GetSnapshot());
                //}

                // Only compiler-generated MoveNext methods have iterator scopes.  See if this is one.
                var stateMachineHoistedLocalScopes = default(ImmutableArray<Cci.StateMachineHoistedLocalScope>);
                //if (isStateMachineMoveNextMethod)
                //{
                //    stateMachineHoistedLocalScopes = builder.GetHoistedLocalScopes();
                //}

                var stateMachineHoistedLocalSlots = default(ImmutableArray<EncHoistedLocalInfo>);
                var stateMachineAwaiterSlots = default(ImmutableArray<Cci.ITypeReference>);
                //if (optimizations == OptimizationLevel.Debug && stateMachineTypeOpt != null)
                //{
                //    Debug.Assert(method.IsAsync || method.IsIterator);
                //    GetStateMachineSlotDebugInfo(moduleBuilder, moduleBuilder.GetSynthesizedFields(stateMachineTypeOpt), variableSlotAllocatorOpt, diagnosticsForThisMethod, out stateMachineHoistedLocalSlots, out stateMachineAwaiterSlots);
                //    Debug.Assert(!diagnostics.HasAnyErrors());
                //}

                return new MethodBody(
                    il.RealizedIL,
                    il.MaxStack,
                    (Cci.IMethodDefinition)routine.PartialDefinitionPart ?? routine,
                    variableSlotAllocatorOpt?.MethodId ?? new DebugId(0, moduleBuilder.CurrentGenerationOrdinal),
                    localVariables,
                    il.RealizedSequencePoints,
                    debugDocumentProvider,
                    il.RealizedExceptionHandlers,
                    il.GetAllScopes(),
                    il.HasDynamicLocal,
                    null, // importScopeOpt,
                    ImmutableArray<LambdaDebugInfo>.Empty, // lambdaDebugInfo,
                    ImmutableArray<ClosureDebugInfo>.Empty, // closureDebugInfo,
                    null, //stateMachineTypeOpt?.Name,
                    stateMachineHoistedLocalScopes,
                    stateMachineHoistedLocalSlots,
                    stateMachineAwaiterSlots,
                    asyncDebugInfo);
            }
            finally
            {
                // Basic blocks contain poolable builders for IL and sequence points. Free those back
                // to their pools.
                il.FreeBasicBlocks();

                //// Remember diagnostics.
                //diagnostics.AddRange(diagnosticsForThisMethod);
                //diagnosticsForThisMethod.Free();
            }
        }

        internal static void EmitCtorBody(
            PEModuleBuilder moduleBuilder,
            SynthesizedCtorWrapperSymbol routine,
            DiagnosticBag diagnostics,
            bool emittingPdb)
        {
            Debug.Assert(!routine.IsStatic);

            var compilation = moduleBuilder.Compilation;
            var parameters = routine.Parameters;

            var il = new ILBuilder(moduleBuilder, new LocalSlotManager(null), compilation.Options.OptimizationLevel);

            Debug.Assert(parameters[0].Type == compilation.CoreTypes.Context);    // first parameter always <ctx>

            var type = (SourceNamedTypeSymbol)routine.ContainingType;
            var realctor = routine.RealCtorMethod;  // .ctor or __construct to be called
            var ctxField = type.ContextField;       // .<ctx>

            Debug.Assert(!type.IsStatic);

            // initialize <ctx> field
            if (ctxField != null)
            {
                // if field is declared within this type or
                // realctor is __construct
                if (object.ReferenceEquals(ctxField.ContainingType, type) || object.ReferenceEquals(realctor?.ContainingType, type))
                {
                    // TODO: emit debug.assert(<ctx> != null)

                    // <this>.<ctx> = <ctx>
                    il.EmitLoadArgumentOpcode(0);   // this
                    il.EmitLoadArgumentOpcode(1);   // <ctx>
                    il.EmitOpCode(ILOpCode.Stfld);
                    il.EmitToken(ctxField, null, diagnostics);
                }
            }

            // initialize class fields,
            // default(PhpValue) is not a valid value, its TypeTable must not be null
            foreach (var fld in type.GetFieldsToEmit().OfType<SourceFieldSymbol>().Where(fld => !fld.IsStatic))
            {
                // TODO: extract following code as a field initializer
                // TODO: init static fields as well in .cctor
                if (fld.Type == compilation.CoreTypes.PhpValue)
                {
                    // this.fld = PhpValue.Void
                    il.EmitLoadArgumentOpcode(0);   // this
                    il.EmitOpCode(ILOpCode.Ldsfld);
                    il.EmitSymbolToken(moduleBuilder, diagnostics, compilation.CoreMethods.PhpValue.Void, null);
                    il.EmitOpCode(ILOpCode.Stfld);
                    il.EmitSymbolToken(moduleBuilder, diagnostics, fld, null);
                }
            }

            // call __construct method
            if (realctor != null)
            {
                Debug.Assert(!realctor.IsStatic);
                var ps = realctor.Parameters;
                bool returnsValue = !realctor.ReturnsVoid;

                // call realctor
                il.EmitLoadArgumentOpcode(0);    // this

                // TODO: bind arguments using overload resolution, following is temporary:

                int source = 2; // skip <ctx>

                for (int i = 0; i < ps.Length; i++)
                {
                    var targetp = ps[i];
                    if (targetp.IsImplicitlyDeclared)
                    {
                        if (ps[i].Type == compilation.CoreTypes.Context)
                        {
                            // load <ctx>
                            il.EmitLoadArgumentOpcode(1);
                            continue;
                        }
                    }

                    var srcp = parameters[source - 1];
                    if (targetp.Type == srcp.Type)
                    {
                        il.EmitLoadArgumentOpcode(source);
                        source++;
                    }
                    else
                    {
                        throw new InvalidOperationException("Signature does not match.");
                    }
                }

                il.EmitOpCode(ILOpCode.Call, -1 - ps.Length + (returnsValue ? 1 : 0));   // - this - ps
                il.EmitToken(realctor, null, diagnostics);

                if (returnsValue)
                    il.EmitOpCode(ILOpCode.Pop);

                //
                il.EmitRet(true);

                //
                var body = CreateSynthesizedBody(moduleBuilder, routine, il);
                moduleBuilder.SetMethodBody(routine, body);
            }
        }

        internal static MethodBody CreateSynthesizedBody(PEModuleBuilder moduleBuilder, IMethodSymbol routine, ILBuilder il)
        {
            var compilation = moduleBuilder.Compilation;
            var localSlotManager = il.LocalSlotManager;
            var optimizations = compilation.Options.OptimizationLevel;

            try
            {
                il.Realize();

                //
                var localVariables = il.LocalSlotManager.LocalsInOrder();

                if (localVariables.Length > 0xFFFE)
                {
                    //diagnosticsForThisMethod.Add(ErrorCode.ERR_TooManyLocals, method.Locations.First());
                }

                //if (diagnosticsForThisMethod.HasAnyErrors())
                //{
                //    // we are done here. Since there were errors we should not emit anything.
                //    return null;
                //}

                //// We will only save the IL builders when running tests.
                //if (moduleBuilder.SaveTestData)
                //{
                //    moduleBuilder.SetMethodTestData(method, builder.GetSnapshot());
                //}

                // Only compiler-generated MoveNext methods have iterator scopes.  See if this is one.
                var stateMachineHoistedLocalScopes = default(ImmutableArray<Cci.StateMachineHoistedLocalScope>);
                //if (isStateMachineMoveNextMethod)
                //{
                //    stateMachineHoistedLocalScopes = builder.GetHoistedLocalScopes();
                //}

                var stateMachineHoistedLocalSlots = default(ImmutableArray<EncHoistedLocalInfo>);
                var stateMachineAwaiterSlots = default(ImmutableArray<Cci.ITypeReference>);
                //if (optimizations == OptimizationLevel.Debug && stateMachineTypeOpt != null)
                //{
                //    Debug.Assert(method.IsAsync || method.IsIterator);
                //    GetStateMachineSlotDebugInfo(moduleBuilder, moduleBuilder.GetSynthesizedFields(stateMachineTypeOpt), variableSlotAllocatorOpt, diagnosticsForThisMethod, out stateMachineHoistedLocalSlots, out stateMachineAwaiterSlots);
                //    Debug.Assert(!diagnostics.HasAnyErrors());
                //}

                return new MethodBody(
                    il.RealizedIL,
                    il.MaxStack,
                    (Cci.IMethodDefinition)routine,
                    new DebugId(0, moduleBuilder.CurrentGenerationOrdinal),
                    localVariables,
                    il.RealizedSequencePoints,
                    null,
                    il.RealizedExceptionHandlers,
                    il.GetAllScopes(),
                    il.HasDynamicLocal,
                    null, // importScopeOpt,
                    ImmutableArray<LambdaDebugInfo>.Empty, // lambdaDebugInfo,
                    ImmutableArray<ClosureDebugInfo>.Empty, // closureDebugInfo,
                    null, //stateMachineTypeOpt?.Name,
                    stateMachineHoistedLocalScopes,
                    stateMachineHoistedLocalSlots,
                    stateMachineAwaiterSlots,
                    null);
            }
            finally
            {
                // Basic blocks contain poolable builders for IL and sequence points. Free those back
                // to their pools.
                il.FreeBasicBlocks();

                //// Remember diagnostics.
                //diagnostics.AddRange(diagnosticsForThisMethod);
                //diagnosticsForThisMethod.Free();
            }
        }
    }
}
