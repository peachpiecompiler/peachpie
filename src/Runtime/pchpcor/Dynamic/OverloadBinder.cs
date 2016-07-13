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
        abstract class ArgumentsBinder
        {
            #region CostOfKey, CostOfValue

            /// <summary>
            /// Describes what cost is computed.
            /// </summary>
            [DebuggerDisplay("{VariableName,nq}")]
            class CostOfKey : IEquatable<CostOfKey>
            {
                public int ArgIndex;
                public Type TargetType;
                public bool IsMandatory;

                public virtual string VariableName => $"costof{ArgIndex}_To_{TargetType.Name}" + (IsMandatory ? "" : "_Optional");

                #region IEquatable

                public bool Equals(CostOfKey other) => other.ArgIndex == ArgIndex && this.GetType() == other.GetType() && other.TargetType == TargetType && other.IsMandatory == IsMandatory;

                public override int GetHashCode() => ArgIndex ^ (IsMandatory ? 0 : 0x1000) ^ TargetType.GetHashCode();

                public override bool Equals(object obj) => obj is CostOfKey && Equals((CostOfKey)obj);

                #endregion
            }

            class CostOfTooManyArgsKey : CostOfKey
            {
                public override string VariableName => $"toomanyargs{ArgIndex}cost";

                public override int GetHashCode() => 0x12345678 ^ ArgIndex;
            }

            /// <summary>
            /// Bound cost of conversion operation.
            /// </summary>
            class CostOfValue
            {
                /// <summary>
                /// The expression to use. Can be constant or variable.
                /// </summary>
                public Expression CostOfExpression;

                /// <summary>
                /// In case of variable, this is its initializer (actual costof expression).
                /// </summary>
                public Expression CostOfInitializer;
            }

            #endregion

            #region Fields

            /// <summary>
            /// Block of code that initializes variables created by subsequent calls to binding methods.
            /// </summary>
            readonly protected List<Expression> _lazyInitBlock = new List<Expression>();

            /// <summary>
            /// Cache of resolved costs.
            /// Contained expression expects the argument ArgIndex exists.
            /// </summary>
            readonly Dictionary<CostOfKey, CostOfValue> _costs = new Dictionary<CostOfKey, CostOfValue>();

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

                    foreach (var c in _costs.Values
                        .Select(x => x.CostOfExpression as ConstantExpression)
                        .Where(x => x != null)
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
                var key = new CostOfKey() { ArgIndex = srcarg, TargetType = targetparam.ParameterType, IsMandatory = ismandatory };

                // lookup cache
                CostOfValue costvalue;
                if (!_costs.TryGetValue(key, out costvalue))
                {
                    // bind cost expression
                    costvalue = new CostOfValue();

                    var expr_cost = ConvertExpression.BindCost(BindArgument(srcarg), targetparam.ParameterType);

                    if (expr_cost is ConstantExpression)
                    {
                        costvalue.CostOfExpression = expr_cost;
                    }
                    else
                    {
                        costvalue.CostOfInitializer = expr_cost;
                        costvalue.CostOfExpression = Expression.Variable(typeof(ConversionCost), key.VariableName);
                    }

                    _costs[key] = costvalue;
                }

                //
                return costvalue.CostOfExpression;
            }

            /// <summary>
            /// Gets expression resulting in <see cref="ConversionCost.Pass"/> or <see cref="ConversionCost.TooManyArgs"/> based on actual arguments count.
            /// </summary>
            public virtual Expression BindCostOfTooManyArgs(int expectedargs)
            {
                var key = new CostOfTooManyArgsKey() { ArgIndex = expectedargs };

                // lookup cache
                CostOfValue costvalue;
                if (!_costs.TryGetValue(key, out costvalue))
                {
                    // bind cost expression
                    costvalue = new CostOfValue();

                    var expr_argc = BindArgsCount();
                    if (expr_argc is ConstantExpression)
                    {
                        var argc = (int)((ConstantExpression)expr_argc).Value;
                        costvalue.CostOfExpression = Expression.Constant((argc > expectedargs) ? ConversionCost.TooManyArgs : ConversionCost.Pass);
                    }
                    else
                    {
                        // toomanyargsXcost = TooManyArgs;  in case we endup in this condition
                        costvalue.CostOfInitializer = Expression.Constant(ConversionCost.TooManyArgs);
                        costvalue.CostOfExpression = Expression.Variable(typeof(ConversionCost), key.VariableName);
                    }

                    _costs[key] = costvalue;
                }

                //
                return costvalue.CostOfExpression;
            }

            /// <summary>
            /// After all bindings, creates an expression that initializes the call.
            /// </summary>
            public virtual Expression CreatePreamble(List<ParameterExpression> variables)
            {
                Debug.Assert(!_created);
                _created = true;

                // create costof variables
                var expr_argc = BindArgsCount();
                int? argc_opt = (expr_argc is ConstantExpression) ? (int?)((ConstantExpression)expr_argc).Value : null;

                foreach (var cg in _costs.Where(x => x.Value.CostOfInitializer != null).GroupBy(x => x.Key.ArgIndex).OrderBy(x => x.Key))
                {
                    var cost_init = new List<Expression>();
                    var cost_missing = new List<Expression>();

                    // IF (argc > INDEX) cost1 = ..
                    // ELSE cost1 = Missing

                    foreach (var c in cg)
                    {
                        // define variable
                        Debug.Assert(c.Value.CostOfExpression is ParameterExpression);
                        variables.Add((ParameterExpression)c.Value.CostOfExpression);

                        // initializer expression
                        cost_init.Add(Expression.Assign(c.Value.CostOfExpression, c.Value.CostOfInitializer));
                        cost_missing.Add(Expression.Assign(c.Value.CostOfExpression, Expression.Constant(c.Key.IsMandatory ? ConversionCost.MissingArgs : ConversionCost.DefaultValue)));
                    }

                    if (argc_opt.HasValue)
                    {
                        // we know in compile time whether the argument is missing or we can safely compute the cost
                        _lazyInitBlock.AddRange((argc_opt.Value > cg.Key) ? cost_init : cost_missing);
                    }
                    else
                    {
                        // check in compile time whether the argument is missing
                        _lazyInitBlock.Add(Expression.IfThenElse(Expression.GreaterThan(expr_argc, Expression.Constant(cg.Key)), // argc > INDEX
                            Expression.Block(cost_init),
                            Expression.Block(cost_missing)));
                    }
                }

                //
                return Expression.Block(_lazyInitBlock);
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
                    var expr_arg = Expression.ArrayIndex(_argsarray, Expression.Constant(srcarg));

                    return (targetparam == null)
                        ? expr_arg
                        : ConvertExpression.Bind(expr_arg, targetparam.ParameterType);
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

        static Expression BinaryOrCosts(IList<Expression> ops) => BinaryOr<ConversionCost>(ops, CostOf.Or, typeof(CostOf).GetMethod("Or", typeof(ConversionCost), typeof(ConversionCost)));

        /// <summary>
        /// Creates expression that computes cost of the method call with given arguments.
        /// </summary>
        /// <param name="method">Method to calculate the cost.</param>
        /// <param name="argsarray">Expression representing array of arguments to be passed to method.</param>
        /// <returns></returns>
        private static Expression BindCostOf(MethodBase method, ArgumentsBinder args)
        {
            if (method == null || args == null)
                throw new ArgumentNullException();

            var ps = method.GetParameters();

            // method( {implicit}, {mandatory}, {optional} )

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

            var body = new List<Expression>();

            var var_result = Expression.Variable(typeof(ConversionCost), "result");
            var expr_argc = args.BindArgsCount();
            int? argc_opt = (expr_argc is ConstantExpression) ? (int?)((ConstantExpression)expr_argc).Value : null;

            // mandatory parameters cost
            var block_cost = new List<Expression>();
            var expr_costs = new List<Expression>();
            for (int im = 0; im < nmandatory; im++)
            {
                expr_costs.Add(args.BindCostOf(im, ps[nimplicit + im]));
            }

            var hasparams = noptional != 0 && ps.Last().IsParamsParameter();
            if (!hasparams)
            {
                // (argc > expectedargs) ? TooManyArgs : Pass
                expr_costs.Add(args.BindCostOfTooManyArgs(nmandatory + noptional));
            }

            var expr_combined_cost = BinaryOrCosts(expr_costs);
            block_cost.Add(Expression.Assign(var_result, expr_combined_cost));  // result = CostOf1 | .. | CostOfN;

            /* if (argc >= 0) {
             *   result | CostOf(argv[0], t0);
             *   if (argc >= 1) {
             *     result |= CostOf(argv[1], t1);
             *     if ...
             *   }
             */
            Expression expr_optcheck = null;
            for (int io = noptional - 1; io >= 0; io--)
            {
                var argi = nmandatory + io;
                var p = ps[nimplicit + io];

                if (p.IsParamsParameter())
                {
                    Debug.Assert(expr_optcheck == null && hasparams);    // last parameter

                    hasparams = true;

                    // for (int o = io + nmandatory; o < argc; o++) result |= CostOf(argv[o], p.ElementType)
                    throw new NotImplementedException();
                }
                else
                {
                    // IF (argc >= argi) { result |= CostOf(argv[argi], p); expr_optcheck)

                    var check = (Expression)Expression.OrAssign(var_result, args.BindCostOf(argi, p));    // result |= CostOf

                    expr_optcheck = Expression.IfThen(Expression.GreaterThanOrEqual(expr_argc, Expression.Constant(argi)), // (argc >= argi)
                        (expr_optcheck == null)
                        ? check
                        : Expression.Block(check, expr_optcheck)
                        );
                }
            }

            if (expr_optcheck != null)
            {
                block_cost.Add(expr_optcheck);
            }

            //
            body.AddRange(block_cost);


            //
            body.Add(var_result);  // result

            // return Block { ... ; result; }
            return Expression.Block(typeof(ConversionCost), new[] { var_result }, body);
        }

        private static Expression BindCostOf(MethodBase method, Expression[] args)
        {
            throw new NotImplementedException();
        }

        public static Expression BindOverloadCall<TReturn>(MethodBase[] methods, Expression ctx, Expression argsarray)
        {
            if (methods == null || argsarray == null)
                throw new ArgumentNullException();

            Debug.Assert(argsarray.Type.IsArray);

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
            var args = new ArgumentsBinder.ArgsArrayBinder(argsarray);

            Expression invoke = Expression.Default(typeof(TReturn));

            //
            if (methods.Length == 0)
            {
                throw new ArgumentException();    // no method to call
                // invoke = ERR
            }
            if (methods.Length == 1)
            {
                // just this piece of code is enough:
                invoke = ConvertExpression.Bind(BinderHelpers.BindToCall(null, methods[0], ctx, argsarray, args.BindArgsCount()), typeof(TReturn));
            }
            else
            {
                var mlist = new List<MethodBase>(); // methods to call
                var clist = new List<ParameterExpression>(); // methods computed cost

                // costX = CostOf(mX)
                foreach (var m in methods)
                {
                    //ConversionCost bestcost;
                    var expr_cost = BindCostOf(m, args);
                    //if (bestcost >= ConversionCost.NoConversion)
                    //    continue;   // we don't have to try this overload // won't happen in dynamic call

                    var cost_var = Expression.Variable(typeof(ConversionCost), "cost" + mlist.Count);

                    body.Add(Expression.Assign(cost_var, expr_cost));   // costX = CostOf(m)

                    mlist.Add(m);
                    clist.Add(cost_var);
                }

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

                    var mcall = ConvertExpression.Bind(BinderHelpers.BindToCall(null, mlist[i], ctx, argsarray, args.BindArgsCount()), typeof(TReturn));
                    invoke = Expression.Condition(Expression.Equal(expr_best, clist[i]), mcall, invoke);
                }
            }

            //
            body.Insert(0, args.CreatePreamble(locals));

            //
            body.Add(invoke);

            // return Block { ... ; invoke; }
            return Expression.Block(typeof(TReturn), locals, body);
        }
    }
}
