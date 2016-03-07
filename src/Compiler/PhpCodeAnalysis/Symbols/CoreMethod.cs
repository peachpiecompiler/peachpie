using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Descriptor of a well-known method declared in PchpCor library.
    /// </summary>
    [DebuggerDisplay("CoreMethod {DebuggerDisplay,nq}")]
    class CoreMethod
    {
        /// <summary>
        /// Lazily associated symbol.
        /// </summary>
        private MethodSymbol _symbol;

        /// <summary>
        /// Parametyer types.
        /// </summary>
        readonly SpecialType[] _ptypes;

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

        public CoreMethod(CoreType declaringClass, string methodName, params SpecialType[] ptypes)
        {
            Contract.ThrowIfNull(declaringClass);
            Contract.ThrowIfNull(methodName);

            this.DeclaringClass = declaringClass;
            this.MethodName = methodName;

            _ptypes = ptypes;
        }

        private void Update(MethodSymbol symbol)
        {
            Contract.ThrowIfNull(symbol);
            _symbol = symbol;
        }

        /// <summary>
        /// Resolves <see cref="MethodSymbol"/> of this descriptor.
        /// </summary>
        private void ResolveSymbol()
        {
            var type = this.DeclaringClass.Symbol;
            if (type == null)
                throw new InvalidOperationException();

            var methods = type.GetMembers(MethodName);
            Update(methods.OfType<MethodSymbol>().First(MatchesSignature));
        }

        private bool MatchesSignature(MethodSymbol m)
        {
            var ps = m.Parameters;
            if (ps.Length != _ptypes.Length)
                return false;

            for (int i = 0; i < ps.Length; i++)
                if (ps[i].Type.SpecialType != _ptypes[i])
                    return false;

            return true;
        }
    }

    /// <summary>
    /// Set of well-known methods declared in PchpCor library.
    /// </summary>
    static class CoreMethods
    {
        public struct Operators
        {
            public static readonly CoreMethod Equal_Object_Object = CoreTypes.Operators.Method(
                "Equal", SpecialType.System_Object, SpecialType.System_Object);
        }
    }
}
