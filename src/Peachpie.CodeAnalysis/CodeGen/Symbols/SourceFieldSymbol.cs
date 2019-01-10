using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        void IPhpPropertySymbol.EmitInit(CodeGenerator cg)
        {
            cg.TypeRefContext = EnsureTypeRefContext();

            //

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

            //
            cg.TypeRefContext = null;
        }
    }

    partial class SynthesizedTraitFieldSymbol
    {
        void IPhpPropertySymbol.EmitInit(CodeGenerator cg)
        {
            // this.{FIELD} = {ORIGINAL_FIELD}

            var fldplace = new FieldPlace(IsStatic ? null : new ArgPlace(ContainingType, 0), this, cg.Module);

            fldplace.EmitStorePrepare(cg.Builder);
            cg.EmitConvert(EmitLoadSourceValue(cg), 0, this.Type);
            fldplace.EmitStore(cg.Builder);
        }

        TypeSymbol EmitLoadSourceValue(CodeGenerator cg)
        {
            if (_traitmember is FieldSymbol f)
            {
                TypeSymbol instanceType;
                var phpf = (IPhpPropertySymbol)f;
                if (phpf.FieldKind == PhpPropertyKind.InstanceField)
                {
                    // Template: LOAD <>trait_T
                    Debug.Assert(_traitInstanceField != null);
                    instanceType = _traitInstanceField.EmitLoad(cg, cg.ThisPlaceOpt);
                }
                else
                {
                    instanceType = null;
                }

                // 
                VariableReferenceExtensions.EmitReceiver(cg, f, instanceType);

                // LOAD {FIELD}
                cg.Builder.EmitOpCode(ILOpCode.Ldfld);
                cg.EmitSymbolToken(f, null);
                return f.Type;
            }

            throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(_traitmember);
        }
    }
}
