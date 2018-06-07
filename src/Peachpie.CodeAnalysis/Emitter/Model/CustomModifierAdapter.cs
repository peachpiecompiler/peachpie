using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Symbols
{
    internal partial class CSharpCustomModifier : Cci.ICustomModifier
    {
        bool Cci.ICustomModifier.IsOptional => this.IsOptional;

        Cci.ITypeReference Cci.ICustomModifier.GetModifier(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(this.Modifier, context.SyntaxNodeOpt, context.Diagnostics);
        }
    }
}
