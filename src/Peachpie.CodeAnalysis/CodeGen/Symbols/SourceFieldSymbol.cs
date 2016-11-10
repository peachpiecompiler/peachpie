using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceFieldSymbol
    {
        /// <summary>
        /// Whteher the field initializer requires a reference to current <see cref="Pchp.Core.Context"/>.
        /// </summary>
        internal bool RequiresContext
        {
            get
            {
                if (this.Initializer != null && !this.Initializer.ConstantValue.HasValue)
                {
                    return this.Initializer.RequiresContext;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets value indicating whether the field has to be contained in <see cref="SynthesizedStaticFieldsHolder"/>.
        /// </summary>
        internal bool RequiresHolder
        {
            get
            {
                switch (this.FieldKind)
                {
                    case KindEnum.AppStaticField: return false;
                    case KindEnum.StaticField: return true; // PHP static field is bound to Context and has to be instantiated within holder class
                    case KindEnum.InstanceField: return false;
                    case KindEnum.ClassConstant: return this.GetConstantValue(false) == null;   // if constant has to be evaluated in runtime, we have to evaluate its value for each context separatelly within holder
                    default:
                        throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
                }
            }
        }

        internal void EmitInit(CodeGenerator cg)
        {
            var fldplace = new FieldPlace(IsStatic ? null : new ArgPlace(ContainingType, 0), this);
            
            if (this.Initializer != null)
            {
                // fld = <initializer>
                fldplace.EmitStorePrepare(cg.Builder);
                cg.EmitConvert(this.Initializer, this.Type);
                fldplace.EmitStore(cg.Builder);
            }
            else
            {
                // fld = default(type)
                cg.EmitInitializePlace(fldplace);
            }
        }
    }
}
