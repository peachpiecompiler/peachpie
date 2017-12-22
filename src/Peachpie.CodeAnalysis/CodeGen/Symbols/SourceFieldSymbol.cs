using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceFieldSymbol
    {
        /// <summary>
        /// Whteher the field initializer requires a reference to current <c>Context</c>.
        /// </summary>
        internal bool RequiresContext => this.Initializer != null && this.Initializer.RequiresContext;

        /// <summary>
        /// Gets value indicating whether the field has to be contained in <see cref="SynthesizedStaticFieldsHolder"/>.
        /// </summary>
        internal bool RequiresHolder => PhpFieldSymbolExtension.RequiresHolder(this, this.FieldKind);

        internal void EmitInit(CodeGenerator cg)
        {
            var fldplace = new FieldPlace(IsStatic ? null : new ArgPlace(ContainingType, 0), this, cg.Module);

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

    partial class SynthesizedTraitFieldSymbol
    {
        public bool RequiresContext => !IsConst;

        internal void EmitInit(CodeGenerator cg)
        {
            // this.{FIELD} = {ORIGINAL_FIELD}

            var fldplace = new FieldPlace(IsStatic ? null : new ArgPlace(ContainingType, 0), this, cg.Module);

            fldplace.EmitStorePrepare(cg.Builder);
            cg.EmitConvert(EmitLoadSourceValue(cg), 0, this.Type);
            fldplace.EmitStore(cg.Builder);
        }

        TypeSymbol EmitLoadSourceValue(CodeGenerator cg)
        {
            if (_traitmember.Symbol is FieldSymbol f)
            {
                var __statics = f.TryGetStaticsContainer();
                if (__statics != null)
                {
                    // Template: <ctx>.GetStatics<_statics>()
                    cg.EmitLoadContext();
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(__statics));

                    // LOAD {FIELD}
                    cg.Builder.EmitOpCode(ILOpCode.Ldfld);
                    cg.EmitSymbolToken(f, null);
                    return f.Type;
                }
                else
                {
                    // Template: LOAD <>trait_T.{FIELD}
                    var traitPlace = new FieldPlace(cg.ThisPlaceOpt, _traitInstanceField, cg.Module);
                    var srcplace = new FieldPlace(traitPlace, f, cg.Module);
                    return srcplace.EmitLoad(cg.Builder);
                }
            }

            throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(_traitmember.Symbol);
        }
    }
}
