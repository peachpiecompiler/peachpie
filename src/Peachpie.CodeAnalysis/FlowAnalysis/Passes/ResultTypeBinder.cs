using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    /// <summary>
    /// Walks all expressions and resolves their access, operator method, and result CLR type.
    /// </summary>
    internal class ResultTypeBinder : GraphExplorer<TypeSymbol>
    {
        public PhpCompilation/*!*/DeclaringCompilation { get; }

        #region Initialization

        public ResultTypeBinder(PhpCompilation compilation)
        {
            DeclaringCompilation = compilation ?? throw ExceptionUtilities.ArgumentNull(nameof(compilation));
        }

        public void Bind(SourceRoutineSymbol routine) => routine.ControlFlowGraph?.Accept(this);

        public void Bind(SourceParameterSymbol parameter) => parameter.Initializer?.Accept(this);

        public void Bind(SourceFieldSymbol field) => field.Initializer?.Accept(this);

        #endregion

        /// <summary>
        /// Resolves access operator and updates <see cref="BoundExpression.BoundConversion"/>.
        /// </summary>
        /// <param name="expression">The expression which access has to be resolved.</param>
        /// <param name="type">The value type.</param>
        /// <param name="hasref">Whether we have the value by ref (addr).</param>
        /// <returns>Resulting expression type.</returns>
        TypeSymbol BindAccess(BoundExpression expression, TypeSymbol type, bool hasref)
        {
            var access = expression.Access;

            string opName = null;

            if (access.IsReadRef)
            {
                if (type != DeclaringCompilation.CoreTypes.PhpAlias)    // keep PhpAlias as it is
                {
                    opName = "EnsureAlias";
                }
            }
            else if (access.EnsureObject)
            {
                if (!type.IsReferenceType || Conversions.IsSpecialReferenceType(type))   // keep Object as it is // TODO: just if it's safe (not NULL)
                {
                    opName = "EnsureObject";
                }
            }
            else if (access.EnsureArray)
            {
                // keep PhpArray, ArrayAccess, IPhpArray as it is   // TODO: only if it's safe (not NULL)
                if (!type.IsOfType(DeclaringCompilation.CoreTypes.IPhpArray) &&
                    !type.IsOfType(DeclaringCompilation.CoreTypes.ArrayAccess))
                {
                    opName = "EnsureArray";
                }
            }
            else
            {
                if (access.TargetType != null)
                {
                    expression.BoundConversion = DeclaringCompilation.Conversions.ClassifyConversion(type, access.TargetType, checkexplicit: false);
                    // TODO: check in diagnostics the conversion exists
                    type = access.TargetType;
                }
            }

            // resolve the operator
            if (opName != null)
            {
                var op = DeclaringCompilation.Conversions.ResolveOperator(type, hasref, new[] { opName }, new[] { DeclaringCompilation.CoreTypes.Operators.Symbol });
                if (op != null)
                {
                    expression.BoundConversion = new CommonConversion(true, false, false, false, true, op);
                    type = op.ReturnType;
                }
                else
                {
                    throw new NotImplementedException($"Accessing '{type}' as {access}.");
                }
            }

            //
            return expression.ResultType = type;
        }

        public override TypeSymbol VisitLiteral(BoundLiteral x)
        {
            Debug.Assert(x.ConstantValue.HasValue);

            var type = DeclaringCompilation.GetConstantValueType(x.ConstantValue.Value);

            return BindAccess(x, type, hasref: false);
        }

        //public override TypeSymbol VisitFieldRef(BoundFieldRef x)
        //{
        //    base.VisitFieldRef(x);

        //    //
        //    var type = x.BoundReference.TypeOpt;
        //    return BindAccess(x, type, hasref: true);
        //}

        //public override TypeSymbol VisitVariableRef(BoundVariableRef x)
        //{
        //    // visit name
        //    base.VisitVariableRef(x);

        //    //
        //    var place = x.Place();

        //    return place != null
        //        ? BindAccess(x, place.TypeOpt, true)
        //        : BindAccess(x, DeclaringCompilation.CoreTypes.PhpValue, true);
        //}

        //public override TypeSymbol VisitNew(BoundNewEx x)
        //{
        //    base.VisitNew(x);

        //    //
        //    var type = x.TypeRef.ResolveRuntimeType(DeclaringCompilation);

        //    return BindAccess(x, type, hasref: false);
        //}
    }
}
