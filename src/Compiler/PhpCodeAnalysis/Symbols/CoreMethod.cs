using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// A well-known method declared in PchpCor library.
    /// </summary>
    [DebuggerDisplay("CoreMethod {DebuggerDisplay,nq}")]
    class CoreMethod
    {
        /// <summary>
        /// Lazily associated symbol.
        /// </summary>
        private MethodSymbol _symbol;

        /// <summary>
        /// Declaring class. Cannot be <c>null</c>.
        /// </summary>
        public readonly CoreType DeclaringClass;

        /// <summary>
        /// The method name.
        /// </summary>
        public readonly string MethodName;

        /// <summary>
        /// Gets associated symbol.
        /// </summary>
        public MethodSymbol Symbol
        {
            get
            {
                var symbol = _symbol;
                if (symbol == null)
                {
                    ResolveSymbol();
                    symbol = _symbol;
                }
                return symbol;
            }
        }

        string DebuggerDisplay => DeclaringClass.FullName + "." + MethodName;

        public CoreMethod(CoreType declaringClass, string methodName, params object[] ptypes)
        {
            this.DeclaringClass = declaringClass;
            this.MethodName = methodName;
            // TODO: ptypes
        }

        private void Update(MethodSymbol symbol)
        {
            Contract.ThrowIfNull(symbol);
            _symbol = symbol;
        }

        private void ResolveSymbol()
        {
            var type = this.DeclaringClass.Symbol;
            if (type == null)
                throw new InvalidOperationException();

            var methods = type.GetMembers(MethodName);
            // TODO: overload resolution
            Update(methods.OfType<MethodSymbol>().FirstOrDefault());
        }
    }

    /// <summary>
    /// Set of well-known methods declared in PchpCor library.
    /// </summary>
    static class CoreMethods
    {
        public struct Operators
        {
            public static readonly CoreMethod Equal_Object_Object = CoreTypes.Operators.Method("Equal"/*TODO: Object, Object*/);
        }
    }
}
