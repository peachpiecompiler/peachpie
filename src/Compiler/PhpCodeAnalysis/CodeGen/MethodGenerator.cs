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
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.CodeGen
{
    internal sealed class MethodGenerator
    {
        private static Cci.DebugSourceDocument CreateDebugDocumentForFile(string normalizedPath)
        {
            return new Cci.DebugSourceDocument(normalizedPath, Cci.DebugSourceDocument.CorSymLanguageTypeCSharp);
        }

        internal static MethodBody GenerateMethodBody(
            PEModuleBuilder moduleBuilder,
            SourceBaseMethodSymbol method,
            int methodOrdinal,
            //ImmutableArray<LambdaDebugInfo> lambdaDebugInfo,
            //ImmutableArray<ClosureDebugInfo> closureDebugInfo,
            //StateMachineTypeSymbol stateMachineTypeOpt,
            VariableSlotAllocator variableSlotAllocatorOpt,
            DiagnosticBag diagnostics,
            //ImportChain importChainOpt,
            bool emittingPdb)
        {
            // Note: don't call diagnostics.HasAnyErrors() in release; could be expensive if compilation has many warnings.
            Debug.Assert(!diagnostics.HasAnyErrors(), "Running code generator when errors exist might be dangerous; code generator not expecting errors");

            var compilation = moduleBuilder.Compilation;
            var localSlotManager = new LocalSlotManager(variableSlotAllocatorOpt);
            var optimizations = compilation.Options.OptimizationLevel;

            DebugDocumentProvider debugDocumentProvider = null;

            if (emittingPdb)
            {
                debugDocumentProvider = (path, basePath) => moduleBuilder.GetOrAddDebugDocument(path, basePath, CreateDebugDocumentForFile);
            }

            ILBuilder builder = new ILBuilder(moduleBuilder, localSlotManager, optimizations);
            //DiagnosticBag diagnosticsForThisMethod = DiagnosticBag.GetInstance();
            try
            {
                Cci.AsyncMethodBodyDebugInfo asyncDebugInfo = null;

                Debug.Assert(method.CFG.Length == 1, "Method is not a merged method or a sourceless method.");

                var block = method.CFG[0];
                //var codeGen = new CodeGenerator(method, block, builder, moduleBuilder, diagnosticsForThisMethod, optimizations, emittingPdb);

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
                //{
                //    codeGen.Generate();
                //}

                // DEBUG

                builder.EmitNullConstant();
                builder.EmitRet(false);

                builder.Realize();

                // DEBUG

                var localVariables = builder.LocalSlotManager.LocalsInOrder();

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
                    builder.RealizedIL,
                    builder.MaxStack,
                    (Cci.IMethodDefinition)method.PartialDefinitionPart ?? method,
                    variableSlotAllocatorOpt?.MethodId ?? new DebugId(methodOrdinal, moduleBuilder.CurrentGenerationOrdinal),
                    localVariables,
                    builder.RealizedSequencePoints,
                    debugDocumentProvider,
                    builder.RealizedExceptionHandlers,
                    builder.GetAllScopes(),
                    builder.HasDynamicLocal,
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
                builder.FreeBasicBlocks();

                //// Remember diagnostics.
                //diagnostics.AddRange(diagnosticsForThisMethod);
                //diagnosticsForThisMethod.Free();
            }
        }
    }
}
