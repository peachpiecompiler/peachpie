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
        readonly CoreType[] _ptypes;

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

        public CoreMethod(CoreType declaringClass, string methodName, params CoreType[] ptypes)
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
                if (_ptypes[i] != ps[i].Type)
                    return false;

            return true;
        }
    }

    /// <summary>
    /// Set of well-known methods declared in PchpCor library.
    /// </summary>
    class CoreMethods
    {
        public readonly OperatorsHolder Operators;

        public CoreMethods(CoreTypes types)
        {
            Contract.ThrowIfNull(types);

            Operators = new OperatorsHolder(types);
        }

        public struct OperatorsHolder
        {
            public OperatorsHolder(CoreTypes ct)
            {
                Equal_Object_Object = ct.Operators.Method("Equal", ct.Object, ct.Object);
                PhpAlias_GetValue = ct.PhpAlias.Method("get_Value");
                Echo_String = ct.Context.Method("Echo", ct.String);
                Echo_PhpNumber = ct.Context.Method("Echo", ct.PhpNumber);
                Echo_PhpValue = ct.Context.Method("Echo", ct.PhpValue);
                Echo_Object = ct.Context.Method("Echo", ct.Object);
                Echo_Double = ct.Context.Method("Echo", ct.Double);
                Echo_Long = ct.Context.Method("Echo", ct.Long);
            }

            public readonly CoreMethod
                Equal_Object_Object,
                PhpAlias_GetValue,
                Echo_Object, Echo_String, Echo_PhpNumber, Echo_PhpValue, Echo_Double, Echo_Long;
        }
    }
}
