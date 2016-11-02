using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceRoutineSymbol
    {
        /// <summary>
        /// Gets place referring to <c>Pchp.Core.Context</c> object.
        /// </summary>
        internal virtual IPlace GetContextPlace()
        {
            Debug.Assert(_params[0] is SpecialParameterSymbol && _params[0].Name == SpecialParameterSymbol.ContextName);
            return new ParamPlace(_params[0]);  // <ctx>
        }

        internal virtual IPlace GetThisPlace()
        {
            var thisParameter = this.ThisParameter;
            return (thisParameter != null)
                ? new ReadOnlyPlace(new ParamPlace(thisParameter))
                : null;
        }

        /// <summary>
        /// Creates ghost stubs,
        /// i.e. methods with a different signature calling this routine to comply with CLR standards.
        /// </summary>
        internal virtual void SynthesizeGhostStubs(Emit.PEModuleBuilder module, DiagnosticBag diagnostic)
        {

        }
    }

    partial class SourceMethodSymbol
    {
        internal override IPlace GetContextPlace()
        {
            if (!IsStatic && this.ThisParameter != null)
            {
                // <this>.<ctx> in instance methods
                var t = (SourceTypeSymbol)this.ContainingType;
                return new FieldPlace(GetThisPlace(), t.ContextStore);
            }

            //
            return base.GetContextPlace();
        }
    }

    partial class SourceFunctionSymbol
    {
        internal void EmitInit(Emit.PEModuleBuilder module)
        {
            var cctor = module.GetStaticCtorBuilder(_file);
            var field = new FieldPlace(null, this.EnsureRoutineInfoField(module));

            // {RoutineInfoField} = RoutineInfo.CreateUserRoutine(name, handle)
            field.EmitStorePrepare(cctor);

            cctor.EmitStringConstant(this.QualifiedName.ToString());
            cctor.EmitLoadToken(module, DiagnosticBag.GetInstance(), this, null);
            cctor.EmitCall(module, DiagnosticBag.GetInstance(), System.Reflection.Metadata.ILOpCode.Call, module.Compilation.CoreMethods.Reflection.CreateUserRoutine_string_RuntimeMethodHandle);

            field.EmitStore(cctor);
        }
    }
}