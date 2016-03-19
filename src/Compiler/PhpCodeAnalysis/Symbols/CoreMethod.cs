using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    #region CoreMethod, CoreConstructor, CoreOperator

    /// <summary>
    /// Descriptor of a well-known method.
    /// </summary>
    [DebuggerDisplay("CoreMethod {DebuggerDisplay,nq}")]
    class CoreMethod
    {
        #region Fields

        /// <summary>
        /// Lazily associated symbol.
        /// </summary>
        MethodSymbol _lazySymbol;

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

        #endregion

        public CoreMethod(CoreType declaringClass, string methodName, params CoreType[] ptypes)
        {
            Contract.ThrowIfNull(declaringClass);
            Contract.ThrowIfNull(methodName);

            this.DeclaringClass = declaringClass;
            this.MethodName = methodName;

            _ptypes = ptypes;
        }

        /// <summary>
        /// Gets associated symbol.
        /// </summary>
        public MethodSymbol Symbol
        {
            get
            {
                var symbol = _lazySymbol;
                if (symbol == null)
                {
                    symbol = ResolveSymbol();
                    Contract.ThrowIfNull(symbol);

                    Interlocked.CompareExchange(ref _lazySymbol, symbol, null);
                }
                return symbol;
            }
        }

        string DebuggerDisplay => DeclaringClass.FullName + "." + MethodName;

        /// <summary>
        /// Implicit cast to method symbol.
        /// </summary>
        public static implicit operator MethodSymbol(CoreMethod m) => m.Symbol;

        #region ResolveSymbol

        /// <summary>
        /// Resolves <see cref="MethodSymbol"/> of this descriptor.
        /// </summary>
        protected virtual MethodSymbol ResolveSymbol()
        {
            var type = this.DeclaringClass.Symbol;
            if (type == null)
                throw new InvalidOperationException();

            var methods = type.GetMembers(MethodName);
            return methods.OfType<MethodSymbol>().First(MatchesSignature);
        }

        protected bool MatchesSignature(MethodSymbol m)
        {
            var ps = m.Parameters;
            if (ps.Length != _ptypes.Length)
                return false;

            for (int i = 0; i < ps.Length; i++)
                if (_ptypes[i] != ps[i].Type)
                    return false;

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Descriptor of a well-known constructor.
    /// </summary>
    class CoreConstructor : CoreMethod
    {
        public CoreConstructor(CoreType declaringClass, params CoreType[] ptypes)
            :base(declaringClass, ".ctor", ptypes)
        {

        }

        protected override MethodSymbol ResolveSymbol()
        {
            var type = this.DeclaringClass.Symbol;
            if (type == null)
                throw new InvalidOperationException();

            var methods = type.InstanceConstructors;
            return methods.First(MatchesSignature);
        }
    }

    /// <summary>
    /// Descriptor of a well-known operator method.
    /// </summary>
    class CoreOperator : CoreMethod
    {
        /// <summary>
        /// Creates the descriptor.
        /// </summary>
        /// <param name="declaringClass">Containing class.</param>
        /// <param name="name">Operator name, without <c>op_</c> prefix.</param>
        /// <param name="ptypes">CLR parameters.</param>
        public CoreOperator(CoreType declaringClass, string name, params CoreType[] ptypes)
            :base(declaringClass, "op_" + name, ptypes)
        {

        }

        protected override MethodSymbol ResolveSymbol()
        {
            var type = this.DeclaringClass.Symbol;
            if (type == null)
                throw new InvalidOperationException();

            var methods = type.GetMembers(this.MethodName);
            return methods.OfType<MethodSymbol>()
                .Where(m => m.HasSpecialName)
                .First(MatchesSignature);
        }
    }

    #endregion

    /// <summary>
    /// Set of well-known methods declared in a core library.
    /// </summary>
    class CoreMethods
    {
        public readonly PhpValueHolder PhpValue;
        public readonly PhpNumberHolder PhpNumber;
        public readonly OperatorsHolder Operators;
        public readonly ConstructorsHolder Ctors;

        public CoreMethods(CoreTypes types)
        {
            Contract.ThrowIfNull(types);

            PhpValue = new PhpValueHolder(types);
            PhpNumber = new PhpNumberHolder(types);
            Operators = new OperatorsHolder(types);
            Ctors = new ConstructorsHolder(types);
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

        public struct PhpValueHolder
        {
            public PhpValueHolder(CoreTypes ct)
            {
                ToBoolean = ct.PhpValue.Method("ToBoolean");
                ToLong = ct.PhpValue.Method("ToLong");
                ToDouble = ct.PhpValue.Method("ToDouble");
                ToString_Context = ct.PhpValue.Method("ToString", ct.Context);

                Create_Boolean = ct.PhpValue.Method("Create", ct.Boolean);
                Create_Long = ct.PhpValue.Method("Create", ct.Long);
                Create_Double = ct.PhpValue.Method("Create", ct.Double);
                Create_PhpNumber = ct.PhpValue.Method("Create", ct.PhpNumber);
            }

            public readonly CoreMethod
                ToLong, ToDouble, ToBoolean, ToString_Context,
                Create_Boolean, Create_Long, Create_Double, Create_PhpNumber;
        }

        public struct PhpNumberHolder
        {
            public PhpNumberHolder(CoreTypes ct)
            {
                ToBoolean = ct.PhpNumber.Method("ToBoolean");
                ToLong = ct.PhpNumber.Method("ToLong");
                ToDouble = ct.PhpNumber.Method("ToDouble");
                ToString_Context = ct.PhpNumber.Method("ToString", ct.Context);

                CompareTo_number = ct.PhpNumber.Method("CompareTo", ct.PhpNumber);
                CompareTo_long = ct.PhpNumber.Method("CompareTo", ct.Long);
                CompareTo_double = ct.PhpNumber.Method("CompareTo", ct.Double);

                Create_Long = ct.PhpNumber.Method("Create", ct.Long);
                Create_Double = ct.PhpNumber.Method("Create", ct.Double);

                get_Long = ct.PhpNumber.Method("get_Long");   // TODO: special name, property
                get_Double = ct.PhpNumber.Method("get_Double");   // TODO: special name, property

                Eq_number_number = ct.PhpNumber.Operator("Equality", ct.PhpNumber, ct.PhpNumber);
                Ineq_number_number = ct.PhpNumber.Operator("Inequality", ct.PhpNumber, ct.PhpNumber);

                Add_number_number = ct.PhpNumber.Operator("Addition", ct.PhpNumber, ct.PhpNumber);
                Add_number_long = ct.PhpNumber.Operator("Addition", ct.PhpNumber, ct.Long);
                Add_long_number = ct.PhpNumber.Operator("Addition", ct.Long, ct.PhpNumber);
                Add_double_number = ct.PhpNumber.Method("Add", ct.Double, ct.PhpNumber);
                Add_number_double = ct.PhpNumber.Method("Add", ct.PhpNumber, ct.Double);
                Add_long_long = ct.PhpNumber.Method("Add", ct.Long, ct.Long);
                Add_long_double = ct.PhpNumber.Method("Add", ct.Long, ct.Double);

                Subtract_long_long = ct.PhpNumber.Method("Sub", ct.Long, ct.Long);
                Subtract_number_double = ct.PhpNumber.Method("Sub", ct.PhpNumber, ct.Double);
                Subtract_long_double = ct.PhpNumber.Method("Sub", ct.Long, ct.Double);
                Subtract_number_number = ct.PhpNumber.Operator("Subtraction", ct.PhpNumber, ct.PhpNumber);
                Subtract_long_number = ct.PhpNumber.Operator("Subtraction", ct.Long, ct.PhpNumber);
                Subtract_number_long = ct.PhpNumber.Operator("Subtraction", ct.PhpNumber, ct.Long);
                Negation = ct.PhpNumber.Operator("UnaryNegation", ct.PhpNumber);
                Negation_long = ct.PhpNumber.Method("Minus", ct.Long);

                Division_number_number = ct.PhpNumber.Operator("Division", ct.PhpNumber, ct.PhpNumber);
                Division_long_number = ct.PhpNumber.Operator("Division", ct.Long, ct.PhpNumber);

                Mul_number_number = ct.PhpNumber.Operator("Multiply", ct.PhpNumber, ct.PhpNumber);
                Mul_number_double = ct.PhpNumber.Operator("Multiply", ct.PhpNumber, ct.Double);
                Mul_number_long = ct.PhpNumber.Operator("Multiply", ct.PhpNumber, ct.Long);
                Mul_long_long = ct.PhpNumber.Method("Multiply", ct.Long, ct.Long);

                gt_number_number = ct.PhpNumber.Operator("GreaterThan", ct.PhpNumber, ct.PhpNumber);
                gt_number_long = ct.PhpNumber.Operator("GreaterThan", ct.PhpNumber, ct.Long);
                gt_number_double = ct.PhpNumber.Operator("GreaterThan", ct.PhpNumber, ct.Double);
                lt_number_number = ct.PhpNumber.Operator("LessThan", ct.PhpNumber, ct.PhpNumber);
                lt_number_long = ct.PhpNumber.Operator("LessThan", ct.PhpNumber, ct.Long);
                lt_number_double = ct.PhpNumber.Operator("LessThan", ct.PhpNumber, ct.Double);
            }

            public readonly CoreMethod
                ToLong, ToDouble, ToBoolean, ToString_Context,
                CompareTo_number, CompareTo_long, CompareTo_double,
                Add_long_long, Add_long_double, Add_number_double, Add_double_number,
                Subtract_long_long, Subtract_number_double, Subtract_long_double,
                Negation_long,
                get_Long, get_Double,
                Mul_long_long,
                Create_Long, Create_Double;

            public readonly CoreOperator
                Eq_number_number, Ineq_number_number,
                Add_number_number, Add_number_long, Add_long_number,
                Subtract_number_number, Subtract_long_number, Subtract_number_long,
                Division_number_number, Division_long_number,
                Mul_number_number, Mul_number_double, Mul_number_long,
                gt_number_number, gt_number_long, gt_number_double,
                lt_number_number, lt_number_long, lt_number_double,
                Negation;
        }

        public struct ConstructorsHolder
        {
            public ConstructorsHolder(CoreTypes ct)
            {
                PhpAlias_PhpValue_int = ct.PhpAlias.Ctor(ct.PhpValue, ct.Int32);
            }

            public readonly CoreConstructor
                PhpAlias_PhpValue_int;
        }
    }
}
