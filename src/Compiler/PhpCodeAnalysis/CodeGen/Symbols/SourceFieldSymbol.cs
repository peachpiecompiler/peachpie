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
        /// Whteher the field initializer requires Context.
        /// </summary>
        internal bool InitializerRequiresContext
        {
            get
            {
                return (this.Initializer != null && !(this.Initializer is BoundLiteral));
            }
        }

        internal void EmitInit(CodeGenerator cg)
        {
            var fldplace = new FieldPlace(IsStatic ? null : new ArgPlace(_type, 0), this);
            var type = fldplace.TypeOpt;

            if (this.Initializer != null)
            {
                // fld = <initializer>
                fldplace.EmitStorePrepare(cg.Builder);
                cg.EmitConvert(this.Initializer, type);
                fldplace.EmitStore(cg.Builder);
            }
            else
            {
                switch (type.SpecialType)
                {
                    case Microsoft.CodeAnalysis.SpecialType.System_Boolean:
                    case Microsoft.CodeAnalysis.SpecialType.System_Int32:
                    case Microsoft.CodeAnalysis.SpecialType.System_Int64:
                    case Microsoft.CodeAnalysis.SpecialType.System_Double:
                    case Microsoft.CodeAnalysis.SpecialType.System_Object:
                        break;

                    default:
                        // fld = default(T)
                        fldplace.EmitStorePrepare(cg.Builder);
                        cg.EmitLoadDefaultValue(type, 0);
                        fldplace.EmitStore(cg.Builder);
                        break;
                }
            }
        }
    }
}
