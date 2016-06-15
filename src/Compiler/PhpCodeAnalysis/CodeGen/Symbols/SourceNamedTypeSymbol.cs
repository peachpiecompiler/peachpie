using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class NamedTypeSymbol
    {
        /// <summary>
        /// Gets special <c>_statics</c> nested class holding static fields bound to context.
        /// </summary>
        /// <returns></returns>
        internal TypeSymbol TryGetStatics()
            => GetTypeMembers(WellKnownPchpNames.StaticsHolderClassName)
                .Where(t => t.Arity == 0 && t.DeclaredAccessibility == Accessibility.Public && !t.IsStatic)
                .SingleOrDefault();

        /// <summary>
        /// Tries to find field with given name that can be used as a static field.
        /// Lookups through the class inheritance.
        /// Does not handle member visibility.
        /// </summary>
        internal FieldSymbol ResolveStaticField(string name)
        {
            FieldSymbol field = null;

            for (var t = this; t != null && field == null; t = t.BaseType)
            {
                field = t.GetMembers(name).OfType<FieldSymbol>().SingleOrDefault();
                if (field == null)
                {
                    var statics = t.TryGetStatics();
                    if (statics != null)
                    {
                        field = statics.GetMembers(name).OfType<FieldSymbol>().SingleOrDefault();
                    }
                }
            }

            return field;
        }

        /// <summary>
        /// Emits load of statics holder.
        /// </summary>
        internal TypeSymbol EmitLoadStatics(CodeGenerator cg)
        {
            var statics = TryGetStatics();

            if (statics != null && statics.GetMembers().OfType<IFieldSymbol>().Any())
            {
                // Template: <ctx>.GetStatics<_statics>()
                cg.EmitLoadContext();
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(statics))
                    .Expect(statics);
            }

            return null;
        }
    }
}
