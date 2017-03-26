using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class MethodSymbolExtensions
    {
        public static bool IsParams(this MethodSymbol method)
        {
            return method.ParameterCount != 0 && method.Parameters[method.ParameterCount - 1].IsParams;
        }

        public static bool IsErrorMethod(this MethodSymbol method) => method == null || method is IErrorMethodSymbol;

        public static bool IsMissingMethod(this MethodSymbol method) =>
            (method == null) ||
            (method is IErrorMethodSymbol && ((IErrorMethodSymbol)method).ErrorKind == ErrorMethodKind.Missing);

        public static TypeSymbol[] ParametersType(this MethodSymbol method)
        {
            return method.Parameters.Select(p => p.Type).ToArray();
        }

        public static int GetCallStackBehavior(this MethodSymbol method)
        {
            int stack = 0;

            if (!method.ReturnsVoid)
            {
                // The call puts the return value on the stack.
                stack += 1;
            }

            if (!method.IsStatic)
            {
                // The call pops the receiver off the stack.
                stack -= 1;
            }

            // The call pops all the arguments.
            stack -= method.ParameterCount;

            //
            return stack;
        }

        /// <summary>
        /// Determines if given method is a special compiler generated constructor that only initializes fields.
        /// </summary>
        internal static bool IsFieldsOnlyConstructor(this MethodSymbol m)
        {
            // [PhpFieldsOnlyCtorAttribute] protected .ctor(...)
            if (m != null && m.MethodKind == MethodKind.Constructor && !m.IsStatic && m.DeclaredAccessibility == Accessibility.Protected)
            {
                var attrs = m.GetAttributes();
                return !attrs.IsEmpty && attrs.Any(attr => attr.AttributeClass.MetadataName == "PhpFieldsOnlyCtorAttribute");
            }

            return false;
        }
    }
}
