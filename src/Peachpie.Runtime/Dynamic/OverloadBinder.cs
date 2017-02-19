using System;
using System.Collections;
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
        #region Helpers

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

                expr1 = !object.Equals(const_value, default(T)) ? Expression.Constant(const_value) : null;
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

            return Expression.Or(expr1, expr2, or_method);
        }

        static Expression CombineCosts(IList<Expression> ops) => BinaryOr<ConversionCost>(ops, CostOf.Or, typeof(CostOf).GetMethod("Or", typeof(ConversionCost), typeof(ConversionCost)));

        /// <summary>
        /// Gets array of parameters indexes that have different type in provided methods.
        /// Implicitly declared parameters are skipped, indexes start with first PHP parameter.
        /// </summary>
        /// <remarks>
        /// This optimizes costof() operation for method overloads which have usually same leading parameters.</remarks>
        static BitArray DifferentArgumentTypeIndexes(MethodBase[] methods)
        {
            int maxlength = 0;

            // collect methods parameters
            var mps = new ParameterInfo[methods.Length][];
            for (int i = 0; i < methods.Length; i++)
            {
                var ps = methods[i].GetParameters();
                var skip = ImplicitParametersCount(ps);

                if (skip != 0)
                {
                    var newps = new ParameterInfo[ps.Length - skip];
                    Array.Copy(ps, skip, newps, 0, newps.Length);
                    ps = newps;
                }

                mps[i] = ps;
                maxlength = Math.Max(maxlength, ps.Length);
            }

            var result = new BitArray(maxlength);

            for (int i = 0; i < maxlength; i++)
            {
                // parameter {i}
                Type pt = null;

                for (int mi = 0; mi < mps.Length; mi++)
                {
                    var ps = mps[mi];
                    if (i < ps.Length)
                    {
                        if (pt == null)
                        {
                            pt = ps[i].ParameterType;
                        }
                        else if (pt != ps[i].ParameterType)
                        {
                            // not matching type
                            result[i] = true;
                            break;
                        }
                    }
                    else
                    {
                        result[i] = true;
                        break;
                    }
                }
            }

            //
            return result;
        }

        #endregion

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
            /// Context expression. Cannot be <c>null</c>.
            /// </summary>
            readonly protected Expression _ctx;

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

            protected ArgumentsBinder(Expression ctx)
            {
                Debug.Assert(ctx != null);
                _ctx = ctx;
            }

            #region ArgumentsBinder Methods

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
            /// Bind arguments to array of parameters.
            /// </summary>
            public abstract Expression BindParams(int fromarg, Type element_type);

            /// <summary>
            /// Gets expression representing cost of argument binding operation.
            /// The expression can be constant.
            /// </summary>
            public virtual Expression BindCostOf(int srcarg, Type ptype, bool ismandatory, bool ignorecost)
            {
                var key = new TmpVarKey() { Priority = 100, ArgIndex = srcarg, TargetTypeOpt = ptype, Prefix = "costOf" + (ismandatory ? "" : "Opt") };

                // lookup cache
                TmpVarValue value;
                if (!_tmpvars.TryGetValue(key, out value))
                {
                    // bind cost expression
                    value = new TmpVarValue();

                    var expr_cost = ignorecost ? Expression.Constant(ConversionCost.Pass) : ConvertExpression.BindCost(BindArgument(srcarg), ptype);
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

                public ArgsArrayBinder(Expression ctx, Expression argsarray)
                    : base(ctx)
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
                        value.FalseInitializer = ConvertExpression.BindDefault(value.TrueInitializer.Type); // ~ default(_argsarray.Type.GetElementType())
                        value.Expression = Expression.Variable(value.TrueInitializer.Type, "arg_" + srcarg);

                        //
                        _tmpvars[key] = value;
                    }

                    if (targetparam != null)
                    {
                        // create specialized variable with default value
                        if (targetparam.HasDefaultValue)
                        {
                            var @default = targetparam.DefaultValue;
                            var defaultValueExpr = Expression.Constant(@default);
                            var defaultValueStr = (@default != null) ? @default.ToString() : "NULL";

                            //
                            var key2 = new TmpVarKey() { Priority = 1 /*after key*/, ArgIndex = srcarg, Prefix = "arg(" + defaultValueStr + ")" };
                            TmpVarValue value2;
                            if (!_tmpvars.TryGetValue(key2, out value2))
                            {
                                value2 = new TmpVarValue();

                                value2.TrueInitializer = ConvertExpression.Bind(value.Expression, targetparam.ParameterType, _ctx);   // reuse the value already obtained from argv
                                value2.FalseInitializer = ConvertExpression.Bind(defaultValueExpr, value2.TrueInitializer.Type, _ctx); // ~ default(targetparam)
                                value2.Expression = Expression.Variable(value2.TrueInitializer.Type, "arg_" + srcarg + "_" + defaultValueStr);

                                //
                                _tmpvars[key2] = value2;
                            }

                            return value2.Expression;   // already converted to targetparam.ParameterType
                        }
                    }

                    return (targetparam == null)
                        ? value.Expression
                        : ConvertExpression.Bind(value.Expression, targetparam.ParameterType, _ctx);
                }

                public override Expression BindParams(int fromarg, Type element_type)
                {
                    /* 
                     * length = argc - fromarg;
                     * IF (length > 0)
                     *   Array.Copy(values, argv, fromarg)
                     * ELSE
                     *   Array.Empty()
                     */

                    /*
                     */

                    var var_length = Expression.Variable(typeof(int), "params_length");
                    var var_array = Expression.Variable(element_type.MakeArrayType(), "params_array");

                    //
                    Expression expr_emptyarr = Expression.NewArrayInit(element_type);
                    Expression expr_newarr = Expression.Assign(var_array, Expression.NewArrayBounds(element_type, var_length));  // array = new [length];

                    if (element_type == _argsarray.Type.GetElementType())
                    {
                        if (fromarg == 0)
                        {
                            // return argv;
                            return _argsarray;
                        }
                        else
                        {
                            // static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
                            var array_copy = typeof(Array).GetMethod("Copy", typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int)); // TODO: to cache

                            expr_newarr = Expression.Block(
                                expr_newarr,
                                Expression.Call(array_copy, _argsarray, Expression.Constant(fromarg), var_array, Expression.Constant(0), var_length),   // Array.Copy(argv, fromarg, array, 0, length)
                                var_array
                                );
                        }
                    }
                    else
                    {
                        /* newarr = new T[length];
                         * while (--length >= 0) newarr[length] = convert(argv[fromarg + length]);
                         * lblend: return newarr;
                         */
                        var lblend = Expression.Label("lblend");
                        expr_newarr = Expression.Block(var_array.Type,
                            expr_newarr,
                            Expression.Loop(
                                Expression.IfThenElse(
                                    Expression.GreaterThanOrEqual(Expression.PreDecrementAssign(var_length), Expression.Constant(0)),
                                    Expression.Assign(
                                        Expression.ArrayAccess(var_array, var_length),
                                        ConvertExpression.Bind(Expression.ArrayIndex(_argsarray, Expression.Add(Expression.Constant(fromarg), var_length)), element_type, _ctx)),
                                    Expression.Break(lblend)
                                    )),
                            Expression.Label(lblend),
                            var_array);
                    }

                    return Expression.Block(
                        var_array.Type,
                        new ParameterExpression[] { var_length, var_array },
                        Expression.Assign(var_length, Expression.Subtract(BindArgsCount(), Expression.Constant(fromarg))),  // length = argc - fromarg;
                        Expression.Condition(
                            Expression.GreaterThan(var_length, Expression.Constant(0)), // return (length > 0) ? newarr : emptyarr;
                            expr_newarr, expr_emptyarr));
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

                public ArgsBinder(Expression ctx, Expression[] args)
                    : base(ctx)
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
                            ? ConvertExpression.Bind(expr, targetparam.ParameterType, _ctx)
                            : expr;
                    }
                    else
                    {
                        if (targetparam != null)
                        {
                            if (targetparam.HasDefaultValue)
                            {
                                return ConvertExpression.Bind(Expression.Constant(targetparam.DefaultValue), targetparam.ParameterType, _ctx);
                            }

                            return ConvertExpression.BindDefault(targetparam.ParameterType);
                        }

                        return Expression.Constant(null, typeof(object));
                    }
                }

                public override Expression BindParams(int fromarg, Type element_type)
                {
                    var values = new List<Expression>();
                    for (int i = fromarg; i < _args.Length; i++)
                    {
                        values.Add(ConvertExpression.Bind(_args[i], element_type, _ctx));
                    }

                    if (values.Count == 0)
                    {
                        // TODO: return static singleton with empty array
                    }

                    return Expression.NewArrayInit(element_type, values);
                }

                public override Expression BindCostOf(int srcarg, Type ptype, bool ismandatory, bool ignorecost)
                {
                    if (srcarg < _args.Length)
                    {
                        return base.BindCostOf(srcarg, ptype, ismandatory, ignorecost);
                    }
                    else
                    {
                        return Expression.Constant(ismandatory ? ConversionCost.MissingArgs : ConversionCost.DefaultValue);
                    }
                }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Creates expression that computes cost of the method call with given arguments.
        /// </summary>
        /// <param name="method">Method to calculate the cost.</param>
        /// <param name="args">Arguments provider.</param>
        /// <param name="costofargs">Indexes of parameters which costof() have to be calculated.</param>
        /// <param name="minCost">Gets minimal compile-time cost of conversion.</param>
        /// <returns>Expression getting cost of conversion.</returns>
        static Expression BindCostOf(MethodBase method, ArgumentsBinder args, BitArray costofargs, out ConversionCost minCost)
        {
            if (method == null || args == null)
                throw new ArgumentNullException();

            var ps = method.GetParameters();

            minCost = ConversionCost.Pass;

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

                    var element_type = p.ParameterType.GetElementType();

                    // for (int o = io + nmandatory; o < argc; o++) result |= CostOf(argv[o], p.ElementType)
                    if (argc_opt.HasValue)
                    {
                        for (; im < argc_opt.Value; im++)
                        {
                            expr_costs.Add(args.BindCostOf(im, element_type, false, false));
                        }
                    }
                    else
                    {
                        // just return DefaultValue (which is greater than Warning), for performance reasons
                        expr_costs.Add(Expression.Constant(ConversionCost.DefaultValue));
                    }

                    break;
                }

                //
                expr_costs.Add(args.BindCostOf(im, p.ParameterType, im < nmandatory, costofargs[im] == false));
            }

            if (hasparams == false)
            {
                // (argc > expectedargs) ? TooManyArgs : Pass
                expr_costs.Add(args.BindCostOfTooManyArgs(nmandatory + noptional));
            }

            // collect known costs
            foreach (var cc in expr_costs.OfType<ConstantExpression>().Select(x => (ConversionCost)x.Value))
            {
                minCost |= cc;
            }

            //
            return CombineCosts(expr_costs);
        }

        public static Expression BindOverloadCall(Type treturn, Expression target, MethodBase[] methods, Expression ctx, Expression argsarray)
        {
            Expression result = null;

            while (result == null)
            {
                result = BindOverloadCall(treturn, target, ref methods, ctx, new ArgumentsBinder.ArgsArrayBinder(ctx, argsarray));
            }

            return result;
        }

        public static Expression BindOverloadCall(Type treturn, Expression target, MethodBase[] methods, Expression ctx, Expression[] args)
        {
            Expression result = null;

            while (result == null)
            {
                result = BindOverloadCall(treturn, target, ref methods, ctx, new ArgumentsBinder.ArgsBinder(ctx, args));
            }

            return result;
        }

        #region MethodCostInfo

        /// <summary>
        /// Helper struct containing result of method cost.
        /// </summary>
        struct MethodCostInfo
        {
            /// <summary>
            /// Expression determining the cost of method call.
            /// </summary>
            public Expression CostExpr;

            /// <summary>
            /// Method.
            /// </summary>
            public MethodBase Method;

            /// <summary>
            /// Optional. Resolved cost.
            /// </summary>
            public ConversionCost? ResolvedCost => (CostExpr is ConstantExpression) ? (ConversionCost?)(ConversionCost)((ConstantExpression)CostExpr).Value : null;

            /// <summary>
            /// Minimal cost known in compile time.
            /// </summary>
            public ConversionCost MinimalCost;
        }

        #endregion

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
                invoke = ConvertExpression.Bind(BindCastToFalse(BinderHelpers.BindToCall(target, methods[0], ctx, args), DoCastToFalse(methods[0], treturn)), treturn, ctx);
            }
            else
            {
                var list = new List<MethodCostInfo>();

                // collect arguments, that have same type across all provided methods => we don't have to check costof()
                var makeCostOf = DifferentArgumentTypeIndexes(methods); // parameters which costof() have to be calculated and compared to others

                // costX = CostOf(mX)
                foreach (var m in methods)
                {
                    ConversionCost mincost; // minimal cost resolved in compile time
                    var expr_cost = BindCostOf(m, args, makeCostOf, out mincost);
                    if (mincost >= ConversionCost.NoConversion)
                        continue;   // we don't have to try this overload

                    var const_cost = expr_cost as ConstantExpression;
                    var cost_var = Expression.Variable(typeof(ConversionCost), "cost" + list.Count);

                    if (const_cost == null)
                    {
                        body.Add(Expression.Assign(cost_var, expr_cost));   // costX = CostOf(m)
                    }

                    list.Add(new MethodCostInfo()
                    {
                        Method = m,
                        CostExpr = (Expression)const_cost ?? cost_var,
                        MinimalCost = mincost,
                    });
                }

                if (list.Count != 0)
                {
                    var minresolved = ConversionCost.Error;
                    foreach (var c in list.Where(c => c.ResolvedCost.HasValue))
                    {
                        minresolved = CostOf.Min(minresolved, c.ResolvedCost.Value);
                    }

                    if (list.All(c => c.ResolvedCost.HasValue))
                    {
                        for (int i = 1; i < list.Count; i++)
                        {
                            if (list[i].ResolvedCost.Value == minresolved)
                            {
                                list = new List<MethodCostInfo>() { list[i] };
                                break;
                            }
                        }
                    }
                    else
                    {
                        // if minimal cost is greater than some resolved cost, we can reduce it
                        if (minresolved < ConversionCost.Error)
                        {
                            for (int i = list.Count - 1; i >= 0; i--)
                            {
                                if (list[i].MinimalCost > minresolved)
                                {
                                    list.RemoveAt(i);
                                }
                            }
                        }
                    }
                }

                if (list.Count < methods.Length)
                {
                    // restart binding with reduced list
                    methods = list.Select(l => l.Method).ToArray();
                    return null;
                }

                Debug.Assert(list.Count != 0);

                // declare costI local variables
                locals.AddRange(list.Select(l => l.CostExpr).OfType<ParameterExpression>());

                // best = Min( cost1, .., costN )
                var expr_best = Expression.Variable(typeof(ConversionCost), "best");
                var min_cost_cost = typeof(CostOf).GetMethod("Min", typeof(ConversionCost), typeof(ConversionCost));
                Expression minexpr = list[0].CostExpr;
                for (int i = 1; i < list.Count; i++)
                {
                    minexpr = Expression.Call(min_cost_cost, list[i].CostExpr, minexpr);
                }
                body.Add(Expression.Assign(expr_best, minexpr));
                locals.Add(expr_best);

                // switch over method costs
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    // (best == costI) mI(...) : ...

                    var m = list[i].Method;
                    var mcall = ConvertExpression.Bind(BindCastToFalse(BinderHelpers.BindToCall(target, m, ctx, args), DoCastToFalse(m, treturn)), treturn, ctx);
                    invoke = Expression.Condition(Expression.Equal(expr_best, list[i].CostExpr), mcall, invoke);
                }
            }

            //
            body.Insert(0, args.CreatePreamble(locals));

            //
            body.Add(invoke);

            // return Block { ... ; invoke; }
            return Expression.Block(treturn, locals, body);
        }

        /// <summary>
        /// In case the method has [return: CastToFalse],
        /// the return value -1 or null is converted to false.
        /// </summary>
        static Expression BindCastToFalse(Expression expr, bool hasCastToFalse)
        {
            if (hasCastToFalse)
            {
                var x = Expression.Variable(expr.Type);
                var assign = Expression.Assign(x, expr);    // x = <expr>
                Expression test;

                if (expr.Type == typeof(int) || expr.Type == typeof(long))
                {
                    // Template: test = x >= 0
                    test = Expression.GreaterThanOrEqual(assign, Expression.Constant(0));
                }
                else if (expr.Type == typeof(double))
                {
                    // Template: test = x >= 0.0
                    test = Expression.GreaterThanOrEqual(assign, Expression.Constant(0.0));
                }
                else if (expr.Type.GetTypeInfo().IsValueType == false)  // reference type
                {
                    // Template: test = x != null
                    test = Expression.ReferenceNotEqual(assign, Expression.Constant(null, assign.Type));
                }
                else
                {
                    Debug.Fail($"[CastToFalse] for an unexpected type {expr.Type.ToString()}.");
                    return expr;
                }

                // Template: test ? (PhpValue)x : PhpValue.False
                expr = Expression.Condition(
                    test,
                    ConvertExpression.BindToValue(x),
                    Expression.Property(null, Cache.Properties.PhpValue_False));

                //
                return Expression.Block(
                    expr.Type,
                    new[] { x },
                    new[] { expr });
            }

            return expr;
        }

        static bool DoCastToFalse(MethodBase m, Type returntype)
        {
            return returntype != typeof(void) && m is MethodInfo && ((MethodInfo)m).ReturnTypeCustomAttributes.IsDefined(typeof(CastToFalse), false);
        }
    }
}
