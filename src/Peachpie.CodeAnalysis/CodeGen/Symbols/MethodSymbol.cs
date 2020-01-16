using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract partial class MethodSymbol
    {
        static TypeSymbol EmitCreateRoutine(CodeGenerator cg, MethodSymbol method)
        {
            var il = cg.Builder;

            if (method.IsStatic)
            {
                var overloads = ImmutableArray<MethodSymbol>.Empty;
                if (method is AmbiguousMethodSymbol a)
                {
                    Debug.Assert(a.IsOverloadable);
                    method = a.Ambiguities[0];
                    overloads = a.Ambiguities.RemoveAt(0);
                }

                // CreateUserRoutine("method", ldtoken method, new[] { ldtoken methodOverload1, ... })
                il.EmitStringConstant(method.MetadataName);
                cg.EmitLoadToken(method, null);
                cg.Emit_NewArray(cg.CoreTypes.RuntimeMethodHandle, overloads, m => cg.EmitLoadToken(m, null));

                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Reflection.CreateUserRoutine_string_RuntimeMethodHandle_RuntimeMethodHandleArr);
            }
            else
            {
                ImmutableArray<MethodSymbol> overloads;
                if (method is AmbiguousMethodSymbol a)
                {
                    Debug.Assert(a.IsOverloadable);
                    overloads = a.Ambiguities;
                }
                else
                {
                    overloads = ImmutableArray.Create(method);
                }

                // CreateUserRoutine("method", new[] { (MethodInfo)MethodBase.GetMethodFromHandle(ldtoken method), ... })
                il.EmitStringConstant(method.MetadataName);
                var methodInfoType = cg.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Reflection_MethodInfo);
                cg.Emit_NewArray(methodInfoType, overloads, m =>
                {
                    cg.EmitLoadToken(m, null);
                    cg.EmitCall(ILOpCode.Call, (MethodSymbol)cg.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle));
                    cg.EmitCastClass(methodInfoType);
                    return methodInfoType;
                });

                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Reflection.CreateUserRoutine_String_MethodInfoArray);
            }
        }

        /// <summary>
        /// Emits load of cached <c>RoutineInfo</c> corresponding to this method.
        /// </summary>
        /// <returns>Type symbol of <c>RoutineInfo</c>.</returns>
        internal virtual TypeSymbol EmitLoadRoutineInfo(CodeGenerator cg)
        {
            var name = MetadataName;
            var type = this.ContainingType;
            name = (type != null ? type.GetFullName() : "?") + "." + name; 

            // cache the instance of RoutineInfo
            var tmpfld = cg.Module.SynthesizedManager.GetOrCreateSynthesizedField(
                cg.Module.ScriptType,
                cg.CoreTypes.RoutineInfo,
                "<>" + name,
                Accessibility.Internal,
                isstatic: true,
                @readonly: false);

            // Template: (tmpfld ?? tmpfld = CreateUserRoutine)
            var tmpplace = new FieldPlace(null, tmpfld, cg.Module);
            tmpplace.EmitLoad(cg.Builder);
            cg.EmitNullCoalescing((cg_) =>
            {
                // TODO: Interlocked(ref fld, CreateRoutine, null)
                tmpplace.EmitStorePrepare(cg_.Builder);
                EmitCreateRoutine(cg_, this);

                cg_.Builder.EmitOpCode(ILOpCode.Dup);
                tmpplace.EmitStore(cg_.Builder);
            });

            //
            return tmpfld.Type
                .Expect(cg.CoreTypes.RoutineInfo);
        }
    }
}
