using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using System.Diagnostics;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Helper class for method overload resolution and binding to an <see cref="Expression"/>.
    /// </summary>
    internal static class OverloadBinder
    {
        /// <summary>
        /// Gets count of implicit parameters.
        /// Such parameters are passed by runtime automatically and not read from given arguments.
        /// </summary>
        static int ImplicitParametersCount(ParameterInfo[] ps)
        {
            return ps.TakeWhile(BinderHelpers.IsImplicitParameter).Count();
        }

        static int MandatoryParametersCount(ParameterInfo[] ps, int from)
        {
            return ps.Skip(from).TakeWhile(BinderHelpers.IsMandatoryParameter).Count();
        }

        #region ArgumentsBinder

        /// <summary>
        /// Helper object managing access to arguments.
        /// Different for arguments passed as an array of values and arguments passed through callsite binder, where their count and types are known.
        /// </summary>
        public abstract class ArgumentsBinder
        {
            #region <TmpVarKey, TmpVarValue>

            /// <summary>
            /// Describes what is computed.
            /// </summary>
            [DebuggerDisplay("{VariableName,nq}")]
            class TmpVarKey : IEquatable<TmpVarKey>
            {
                /// <summary>
                /// Order in which values are initialized.
                /// </summary>
                public int Priority;

                /// <summary>
                /// Index of argument we check for. Index from <c>0</c>.
                /// </summary>
                public int ArgIndex;

                /// <summary>
                /// Optional target type in case of conversion or cost operation.
                /// </summary>
                public Type TargetTypeOpt;

                /// <summary>
                /// Textual prefix used for compare different keys and to create variable name.
                /// </summary>
                public string Prefix;

                public virtual string VariableName => $"{Prefix}{ArgIndex}" + ((TargetTypeOpt != null) ? $"_To_{TargetTypeOpt.Name}" : null);

                #region IEquatable

                public bool Equals(TmpVarKey other) => other.ArgIndex == ArgIndex && other.GetType() == GetType() && Prefix == other.Prefix && TargetTypeOpt == other.TargetTypeOpt;

                public override int GetHashCode() => ArgIndex ^ Prefix.GetHashCode() ^ ((TargetTypeOpt != null) ? TargetTypeOpt.GetHashCode() : 0);

                public override bool Equals(object obj) => obj is TmpVarKey && Equals((TmpVarKey)obj);

                #endregion
            }

            /// <summary>
            /// Bound expression to corresponding <see cref="TmpVarKey"/>.
            /// </summary>
            class TmpVarValue
            {
                /// <summary>
                /// The expression to use. Can be constant or variable.
                /// </summary>
                public Expression Expression;

                /// <summary>
                /// In case of variable, this is its initializer (actual expression).
                /// </summary>
                public Expression TrueInitializer;

                /// <summary>
                /// Initializer in case argument is not provided.
                /// </summary>
                public Expression FalseInitializer;
            }

            #endregion

            #region Fields

            /// <summary>
            /// Block of code that initializes temporary variables created by subsequent calls to binding methods.
            /// </summary>
            readonly protected List<Expression> _lazyInitBlock = new List<Expression>();

            /// <summary>
            /// Temporary variables created by binding operations.
            /// Each variable is bound to an argument index - the argument is provided or not.
            /// </summary>
            readonly Dictionary<TmpVarKey, TmpVarValue> _tmpvars = new Dictionary<TmpVarKey, TmpVarValue>();

            bool _created = false;

            #endregion

            #region ArgumentsBinder Methods

            /// <summary>
            /// Gets combined known conversion costs.
            /// </summary>
            public ConversionCost WorstConversionCost
            {
                get
                {
                    var result = ConversionCost.Pass;

                    foreach (var c in _tmpvars.Values
                        .Select(x => x.Expression as ConstantExpression)
                        .Where(x => x != null && x.Value is ConversionCost)
                        .Select(x => (ConversionCost)x.Value))
                    {
                        result |= c;
                    }

                    return result;
                }
            }

            /// <summary>
            /// Gets expression (dynamic or constant) representing arguments count.
            /// </summary>
            public abstract Expression BindArgsCount();

            /// <summary>
            /// Gets argument at index bound to given parameter.
            /// The expression can be constant in case of default argument value.
            /// </summary>
            public abstract Expression BindArgument(int srcarg, ParameterInfo targetparam = null);

            /// <summary>
            /// Gets expression representing cost of argument binding operation.
            /// The expression can be constant.
            /// </summary>
            public virtual Expression BindCostOf(int srcarg, ParameterInfo targetparam)
            {
                var ismandatory = targetparam.IsMandatoryParameter();
                var key = new TmpVarKey() { Priority = 100, ArgIndex = srcarg, TargetTypeOpt = targetparam.ParameterType, Prefix = "costOf" + (ismandatory ? "" : "Opt") };

                // lookup cache
                TmpVarValue value;
                if (!_tmpvars.TryGetValue(key, out value))
                {
                    // bind cost expression
                    value = new TmpVarValue();

                    var expr_cost = ConvertExpression.BindCost(BindArgument(srcarg), targetparam.ParameterType);
                    if (expr_cost is ConstantExpression)
                    {
                        value.Expression = expr_cost;
                    }
                    else
                    {
                        value.TrueInitializer = expr_cost;
                        value.FalseInitializer = Expression.Constant(ismandatory ? ConversionCost.MissingArgs : ConversionCost.DefaultValue);
                        value.Expression = Expression.Variable(typeof(ConversionCost), key.VariableName);
                    }

                    _tmpvars[key] = value;
                }

                //
                return value.Expression;
            }

            /// <summary>
            /// Gets expression resulting in <see cref="ConversionCost.Pass"/> or <see cref="ConversionCost.TooManyArgs"/> based on actual arguments count.
            /// </summary>
            public virtual Expression BindCostOfTooManyArgs(int expectedargs)
            {
                var key = new TmpVarKey() { Priority = 100, ArgIndex = expectedargs, Prefix = "istoomany" };

                // lookup cache
                TmpVarValue value;
                if (!_tmpvars.TryGetValue(key, out value))
                {
                    // bind cost expression
                    value = new TmpVarValue();

                    var expr_argc = BindArgsCount();
                    if (expr_argc is ConstantExpression)
                    {
                        var argc = (int)((ConstantExpression)expr_argc).Value;
                        value.Expression = Expression.Constant((argc > expectedargs) ? ConversionCost.TooManyArgs : ConversionCost.Pass);
                    }
                    else
                    {
                        // istoomanyX = TooManyArgs;  in case we endup in this condition
                        value.TrueInitializer = Expression.Constant(ConversionCost.TooManyArgs);
                        value.FalseInitializer = Expression.Constant(ConversionCost.Pass);
                        value.Expression = Expression.Variable(typeof(ConversionCost), key.VariableName);
                    }

                    _tmpvars[key] = value;
                }

                //
                return value.Expression;
            }

            /// <summary>
            /// After all bindings, creates an expression that initializes the call.
            /// </summary>
            public virtual Expression CreatePreamble(List<ParameterExpression> variables)
            {
                Debug.Assert(!_created);
                _created = true;

                // create arg variables
                var expr_argc = BindArgsCount();
                int? argc_opt = (expr_argc is ConstantExpression) ? (int?)((ConstantExpression)expr_argc).Value : null;

                foreach (var cg in _tmpvars.Where(x => x.Value.TrueInitializer != null).GroupBy(x => x.Key.ArgIndex).OrderBy(x => x.Key))
                {
                    var value_true = new List<Expression>();
                    var value_false = new List<Expression>();

                    // IF (argc > INDEX) cost1 = ..
                    // ELSE cost1 = Missing

                    foreach (var c in cg.OrderBy(x => x.Key.Priority))
                    {
                        // define variable
                        Debug.Assert(c.Value.Expression is ParameterExpression);
                        variables.Add((ParameterExpression)c.Value.Expression);

                        // initializer expression
                        value_true.Add(Expression.Assign(c.Value.Expression, c.Value.TrueInitializer));
                        value_false.Add(Expression.Assign(c.Value.Expression, c.Value.FalseInitializer));
                    }

                    if (argc_opt.HasValue)
                    {
                        // we know in compile time whether the argument is missing or we can safely compute the cost
                        _lazyInitBlock.AddRange((argc_opt.Value > cg.Key) ? value_true : value_false);
                    }
                    else
                    {
                        // check in compile time whether the argument is missing
                        _lazyInitBlock.Add(Expression.IfThenElse(Expression.GreaterThan(expr_argc, Expression.Constant(cg.Key)), // argc > INDEX
                            Expression.Block(value_true),
                            Expression.Block(value_false)));
                    }
                }

                //
                return (_lazyInitBlock.Count != 0) ? (Expression)Expression.Block(_lazyInitBlock) : Expression.Empty();
            }

            #endregion

            #region ArgsArrayBinder

            internal sealed class ArgsArrayBinder : ArgumentsBinder
            {
                /// <summary>
                /// Expression representing array of input arguments.
                /// </summary>
                readonly Expression _argsarray;

                /// <summary>
                /// Lazily initialized variable with arguments count.
                /// </summary>
                ParameterExpression _lazyArgc = null;

                public ArgsArrayBinder(Expression argsarray)
                {
                    if (argsarray == null) throw new ArgumentNullException();
                    if (!argsarray.Type.IsArray) throw new ArgumentException();

                    _argsarray = argsarray;
                }

                public override Expression BindArgsCount()
                {
                    if (_lazyArgc == null)
                    {
                        _lazyArgc = Expression.Variable(typeof(int), "argc");

                        // argc = argv.Length;
                        _lazyInitBlock.Add(Expression.Assign(_lazyArgc, Expression.ArrayLength(_argsarray)));
                    }

                    return _lazyArgc;
                }

                public override Expression BindArgument(int srcarg, ParameterInfo targetparam = null)
                {
                    Debug.Assert(srcarg >= 0);

                    // cache the argument value

                    var key = new TmpVarKey() { Priority = 0 /*first*/, ArgIndex = srcarg, Prefix = "arg" };
                    TmpVarValue value;
                    if (!_tmpvars.TryGetValue(key, out value))
                    {
                        value = new TmpVarValue();

                        value.TrueInitializer = Expression.ArrayIndex(_argsarray, Expression.Constant(srcarg));
                        value.FalseInitializer = Expression.Default(value.TrueInitializer.Type); // ~ default(_argsarray.Type.GetElementType())
                        value.Expression = Expression.Variable(value.TrueInitializer.Type);

                        //
                        _tmpvars[key] = value;
                    }

                    return (targetparam == null)
                        ? value.Expression
                        : ConvertExpression.Bind(value.Expression, targetparam.ParameterType);
                }

                public override Expression CreatePreamble(List<ParameterExpression> variables)
                {
                    var body = base.CreatePreamble(variables);

                    if (_lazyArgc != null)
                    {
                        variables.Add(_lazyArgc);
                    }

                    return body;
                }
            }

            #endregion

            #region ArgsBinder

            internal sealed class ArgsBinder : ArgumentsBinder
            {
                readonly Expression[] _args;

                public ArgsBinder(Expression[] args)
                {
                    _args = args;
                }

                public override Expression BindArgsCount() => Expression.Constant(_args.Length, typeof(int));

                public override Expression BindArgument(int srcarg, ParameterInfo targetparam = null)
                {
                    Debug.Assert(srcarg >= 0);

                    if (srcarg < _args.Length)
                    {
                        var expr = _args[srcarg];

                        return (targetparam != null)
                            ? ConvertExpression.Bind(expr, targetparam.ParameterType)
                            : expr;
                    }
                    else
                    {
                        if (targetparam != null)
                        {
                            if (targetparam.HasDefaultValue)
                                return Expression.Constant(targetparam.DefaultValue, targetparam.ParameterType);

                            return Expression.Default(targetparam.ParameterType);
                        }

                        return Expression.Default(typeof(PhpValue));
                    }
                }

                public override Expression BindCostOf(int srcarg, ParameterInfo targetparam)
                {
                    if (srcarg < _args.Length)
                    {
                        return base.BindCostOf(srcarg, targetparam);
                    }
                    else
                    {
                        return Expression.Constant(targetparam.IsMandatoryParameter() ? ConversionCost.MissingArgs : ConversionCost.DefaultValue);
                    }
                }
            }

            #endregion
        }

        #endregion

        static Expression BinaryOr<T>(IList<Expression> ops, Func<T, T, T> combine, MethodInfo or_method)
        {
            var const_ops = new List<Expression>();
            var expr_ops = new List<Expression>();

            Debug.Assert(ops.All(c => c.Type == typeof(T)));

            foreach (var op in ops)
            {
                ((op.NodeType == ExpressionType.Constant) ? const_ops : expr_ops).Add(op);
            }

            Expression expr1 = null;
            Expression expr2 = null;

            // combine constants
            if (const_ops.Count != 0)
            {
                T const_value = default(T);
                for (int i = 0; i < const_ops.Count; i++)
                {
                    const_value = combine(const_value, (T)((ConstantExpression)const_ops[i]).Value);
                }

                expr1 = Expression.Constant(const_value);
            }

            // combine expressions
            if (expr_ops.Count != 0)
            {
                expr2 = expr_ops[0];

                for (int i = 1; i < expr_ops.Count; i++)
                {
                    expr2 = Expression.Or(expr2, expr_ops[i], or_method);
                }
            }

            //
            if (expr1 == null && expr2 == null)
                return Expression.Constant(default(T));

            if (expr1 == null)
                return expr2;

            if (expr2 == null)
                return expr1;

            return Expression.Or(expr1, expr2);
        }

        static Expression CombineCosts(IList<Expression> ops) => BinaryOr<ConversionCost>(ops, CostOf.Or, typeof(CostOf).GetMethod("Or", typeof(ConversionCost), typeof(ConversionCost)));

        /// <summary>
        /// Creates expression that computes cost of the method call with given arguments.
        /// </summary>
        /// <param name="method">Method to calculate the cost.</param>
        /// <param name="argsarray">Expression representing array of arguments to be passed to method.</param>
        /// <returns></returns>
        static Expression BindCostOf(MethodBase method, ArgumentsBinder args, out ConversionCost worstCost)
        {
            if (method == null || args == null)
                throw new ArgumentNullException();

            var ps = method.GetParameters();

            worstCost = ConversionCost.Pass;

            // method( {implicit}, {mandatory}, {optional+params} )

            var nimplicit = ImplicitParametersCount(ps);
            var nmandatory = MandatoryParametersCount(ps, nimplicit);
            var noptional = ps.Length - nimplicit - nmandatory;

            /*
             * var result = ConversionCost.Pass; // == 0
             * 
             * result = CostOf(argv[0], T1) | CostOf(argv[1], T2) | ... CostOf(argv[nmandatory - 1], TN)    // ! BinaryOrCosts(...)
             * result |= (argc > expectedargs) ? TooManyArgs : Pass;
             * IF (noptional > 0) { ... }
             * 
             * return result;
             */

            var expr_argc = args.BindArgsCount();
            int? argc_opt = (expr_argc is ConstantExpression) ? (int?)((ConstantExpression)expr_argc).Value : null;

            // parameters cost
            var block_cost = new List<Expression>();
            var expr_costs = new List<Expression>();
            bool hasparams = false;
            for (int im = 0; im < nmandatory + noptional; im++)
            {
                var p = ps[nimplicit + im];
                if (noptional != 0 && p.Position == ps.Length - 1 && p.IsParamsParameter())
                {
                    hasparams = true;

                    // for (int o = io + nmandatory; o < argc; o++) result |= CostOf(argv[o], p.ElementType)
                    throw new NotImplementedException();
                }

                Debug.Assert(im < nmandatory || !p.IsMandatoryParameter());
                expr_costs.Add(args.BindCostOf(im, p));
            }

            if (hasparams == false)
            {
                // (argc > expectedargs) ? TooManyArgs : Pass
                expr_costs.Add(args.BindCostOfTooManyArgs(nmandatory + noptional));
            }

            // collect known costs
            foreach (var cc in expr_costs.OfType<ConstantExpression>().Select(x => (ConversionCost)x.Value))
            {
                worstCost |= cc;
            }

            //
            return CombineCosts(expr_costs);
        }

        public static Expression BindOverloadCall(Type treturn, Expression target, MethodBase[] methods, Expression ctx, Expression argsarray)
        {
            Expression result = null;

            while (result == null)
            {
                result = BindOverloadCall(treturn, target, ref methods, ctx, new ArgumentsBinder.ArgsArrayBinder(argsarray));
            }

            return result;
        }

        public static Expression BindOverloadCall(Type treturn, Expression target, MethodBase[] methods, Expression ctx, Expression[] args)
        {
            Expression result = null;

            while (result == null)
            {
                result = BindOverloadCall(treturn, target, ref methods, ctx, new ArgumentsBinder.ArgsBinder(args));
            }
            
            return result;
        }

        /// <summary>
        /// Creates expression with overload resolution.
        /// </summary>
        /// <param name="treturn">Expected return type.</param>
        /// <param name="target">Target instance in case of instance methods.</param>
        /// <param name="methods">List of methods to resolve overload.</param>
        /// <param name="ctx">Expression of current runtime context.</param>
        /// <param name="args">Provides arguments.</param>
        /// <returns>Expression representing overload call with resolution or <c>null</c> in case binding should be restarted with updated array of <paramref name="methods"/>.</returns>
        static Expression BindOverloadCall(Type treturn, Expression target, ref MethodBase[] methods, Expression ctx, ArgumentsBinder args)
        {
            if (methods == null || args == null)
                throw new ArgumentNullException();

            // overload resolution

            // order methods

            /*
             * cost1 = CostOf(m1)
             * cost2 = CostOf(m2)
             * ...
             * best = Min(cost1, .., costN)
             * if (cost1 == best) m1( ... )
             * ...
             * default(T) // unreachable
             */

            var locals = new List<ParameterExpression>();
            var body = new List<Expression>();

            Expression invoke = Expression.Default(treturn);

            //
            if (methods.Length == 0)
            {
                throw new ArgumentException();    // no method to call
                // invoke = ERR
            }
            if (methods.Length == 1)
            {
                // just this piece of code is enough:
                invoke = ConvertExpression.Bind(BinderHelpers.BindToCall(target, methods[0], ctx, args), treturn);
            }
            else
            {
                var mlist = new List<MethodBase>(); // methods to call
                var clist = new List<ParameterExpression>(); // methods computed cost
                var resolvedlist = new List<ConversionCost?>(); // methods computed and compile-time resolved costs

                // costX = CostOf(mX)
                foreach (var m in methods)
                {
                    ConversionCost worstcost;
                    var expr_cost = BindCostOf(m, args, out worstcost);
                    if (worstcost >= ConversionCost.NoConversion)
                        continue;   // we don't have to try this overload

                    var cost_var = Expression.Variable(typeof(ConversionCost), "cost" + mlist.Count);

                    body.Add(Expression.Assign(cost_var, expr_cost));   // costX = CostOf(m)

                    mlist.Add(m);
                    clist.Add(cost_var);
                    resolvedlist.Add((expr_cost is ConstantExpression) ? (ConversionCost?)((ConstantExpression)expr_cost).Value : null);
                }

                if (resolvedlist.Count != 0 && resolvedlist.All(c => c.HasValue))
                {
                    int best = 0;
                    for (int i = 1; i < resolvedlist.Count; i++)
                    {
                        if (resolvedlist[i].Value < resolvedlist[best].Value)
                        {
                            best = i;
                        }
                    }

                    mlist = new List<MethodBase>() { methods[best] };
                    //clist ignored
                }

                if (mlist.Count < methods.Length)
                {
                    // restart binding with reduced list
                    methods = mlist.ToArray();
                    return null;
                }

                Debug.Assert(mlist.Count != 0);

                // declare costI local variables
                locals.AddRange(clist);

                // best = Min( cost1, .., costN )
                var expr_best = Expression.Variable(typeof(ConversionCost), "best");
                var min_cost_cost = typeof(CostOf).GetMethod("Min", typeof(ConversionCost), typeof(ConversionCost));
                Expression minexpr = clist[0];
                for (int i = 1; i < clist.Count; i++)
                {
                    minexpr = Expression.Call(min_cost_cost, clist[i], minexpr);
                }
                body.Add(Expression.Assign(expr_best, minexpr));
                locals.Add(expr_best);

                // switch over method costs
                for (int i = mlist.Count - 1; i >= 0; i--)
                {
                    // (best == costI) mI(...) : ...

                    var mcall = ConvertExpression.Bind(BinderHelpers.BindToCall(target, mlist[i], ctx, args), treturn);
                    invoke = Expression.Condition(Expression.Equal(expr_best, clist[i]), mcall, invoke);
                }
            }

            //
            body.Insert(0, args.CreatePreamble(locals));

            //
            body.Add(invoke);

            // return Block { ... ; invoke; }
            return Expression.Block(treturn, locals, body);
        }
    }
}
