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
        public static Expression BindCostOf(MethodBase method, Expression argsarray, Expression expr_argc, out ConversionCost bestcost)
        {
            if (method == null || argsarray == null)
                throw new ArgumentNullException();

            bestcost = ConversionCost.Pass;

            Debug.Assert(argsarray.Type.IsArray);

            // argv = argsarray
            // argc = argsarray.Length

            var ps = method.GetParameters();

            // method( {implicit}, {mandatory}, {optional} )

            var nimplicit = ImplicitParametersCount(ps);
            var nmandatory = MandatoryParametersCount(ps, nimplicit);
            var noptional = ps.Length - nimplicit - nmandatory;

            /*
             * var result = ConversionCost.Pass; // == 0
             * 
             * IF (argc < nmandatory) {
             *     result = ConversionCost.MissingArgs;
             * }
             * ELSE { // argc >= nmandatory // IF (nmandatory > 0)
             *     result = CostOf(argv[0], T1) | CostOf(argv[1], T2) | ... CostOf(argv[nmandatory - 1], TN)    // ! BinaryOrCosts(...)
             *     IF (noptional > 0) { ... }
             *     }
             *     IF (noptional == 0 && argc > nmandatory) result |= ConversionCost.TooManyArgs;
             * }
             * 
             * return result;
             */

            var body = new List<Expression>();

            var var_result = Expression.Variable(typeof(ConversionCost), "result");
            Expression expr_result = var_result;

            // mandatory parameters cost
            var block_cost = new List<Expression>();
            var expr_costs = new List<Expression>();
            for (int im = 0; im < nmandatory; im++)
            {
                expr_costs.Add(ConvertExpression.BindCost(Expression.ArrayIndex(argsarray, Expression.Constant(im)), ps[nimplicit + im].ParameterType));
            }

            var expr_combined_cost = BinaryOrCosts(expr_costs);
            block_cost.Add(Expression.Assign(expr_result, expr_combined_cost));  // result = CostOf1 | .. | CostOfN;

            if (expr_combined_cost.NodeType == ExpressionType.Constant) // cost resolved as a constant
            {
                bestcost = (ConversionCost)((ConstantExpression)expr_combined_cost).Value;
            }

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
                    Debug.Assert(expr_optcheck == null);    // last parameter

                    // for (int o = io + nmandatory; o < argc; o++) result |= CostOf(argv[o], p.ElementType)
                    throw new NotImplementedException();
                }
                else
                {
                    // IF (argc >= argi) { result |= CostOf(argv[argi], p); expr_optcheck)

                    var check = (Expression)Expression.OrAssign(    // result |= CostOf
                        expr_result,
                        ConvertExpression.BindCost(Expression.ArrayIndex(argsarray, Expression.Constant(argi)), p.ParameterType));

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

            if (noptional == 0)
            {
                var or_cost_cost = typeof(CostOf).GetMethod("Or", typeof(ConversionCost), typeof(ConversionCost));

                // IF(argc > nmandatory) result |= ConversionCost.TooManyArgs;
                var check = Expression.IfThen(Expression.GreaterThan(expr_argc, Expression.Constant(nmandatory)),
                    Expression.OrAssign(expr_result, Expression.Constant(ConversionCost.TooManyArgs), or_cost_cost));
                block_cost.Add(check);
            }

            //
            if (nmandatory != 0)
            {
                body.Add(Expression.IfThenElse(Expression.LessThan(expr_argc, Expression.Constant(nmandatory)), // IF (argc < nmandatory)
                    Expression.Assign(expr_result, Expression.Constant(ConversionCost.MissingArgs)),            //     result = ConversionCost.MissingArgs;
                    Expression.Block(block_cost)    // result = ...;
                ));
            }
            else
            {
                body.AddRange(block_cost);
            }


            //
            body.Add(expr_result);  // result

            // return Block { ... ; result; }
            return Expression.Block(typeof(ConversionCost), new[] { var_result }, body);
        }

        public static Expression BindCostOf(MethodBase method, Expression[] args)
        {
            throw new NotImplementedException();
        }

        public static Expression BindOverloadCall<TReturn>(MethodBase[] methods, Expression ctx, Expression argsarray)
        {
            if (methods == null || argsarray == null)
                throw new ArgumentNullException();

            Debug.Assert(argsarray.Type.IsArray);

            var mlist = new List<MethodBase>(); // methods to call
            var clist = new List<ParameterExpression>(); // methods computed cost

            // overload resolution

            // order methods

            /*
             * cost1 = CostOf(m1)
             * cost2 = CostOf(m2)
             * ...
             * best = Min(cost1, .., costN)
             * if (cost1 == best) m1( ... )
             * ...
             * throw unreachable;
             */

            var locals = new List<ParameterExpression>();
            var expr_argc = Expression.Variable(typeof(int), "argc");
            var body = new List<Expression>();

            // declare argc local variable
            locals.Add(expr_argc);

            // argc = argv.Length;
            var expr_argcassignment = Expression.Assign(expr_argc, Expression.ArrayLength(argsarray));
            body.Add(expr_argcassignment);

            // costX = CostOf(mX)
            foreach (var m in methods)
            {
                ConversionCost bestcost;
                var expr_cost = BindCostOf(m, argsarray, expr_argc, out bestcost);
                if (bestcost >= ConversionCost.NoConversion)
                    continue;   // we don't have to try this overload

                var cost_var = Expression.Variable(typeof(ConversionCost), "cost" + mlist.Count);
                
                body.Add(Expression.Assign(cost_var, expr_cost));   // costX = CostOf(m)

                mlist.Add(m);
                clist.Add(cost_var);
            }

            //
            if (mlist.Count == 0)
            {
                throw new NotImplementedException();    // no method to call
            }
            if (mlist.Count == 1)
            {
                // just this piece of code is enough:
                return Expression.Block(typeof(TReturn), locals,
                    expr_argcassignment,
                    ConvertExpression.Bind(BinderHelpers.BindToCall(null, mlist[0], ctx, argsarray, expr_argc), typeof(TReturn)));
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
            Expression invoke = Expression.Default(typeof(TReturn));
            for (int i = mlist.Count - 1; i >= 0; i--)
            {
                // (best == costI) mI(...) : ...

                var mcall = ConvertExpression.Bind(BinderHelpers.BindToCall(null, mlist[i], ctx, argsarray, expr_argc), typeof(TReturn));
                invoke = Expression.Condition(Expression.Equal(expr_best, clist[i]), mcall, invoke);
            }

            body.Add(invoke);

            // return Block { ... ; invoke; }
            return Expression.Block(typeof(TReturn), locals, body);
        }
    }
}
