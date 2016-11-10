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
        internal bool InitializerRequiresContext
        {
            get
            {
                if (this.Initializer != null && !this.Initializer.ConstantObject.HasValue)
                {
                    return this.Initializer.RequiresContext;
                }

                return false;
            }
        }

        internal void EmitInit(CodeGenerator cg)
        {
            var fldplace = new FieldPlace(IsStatic ? null : new ArgPlace(_type, 0), this);
            
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
