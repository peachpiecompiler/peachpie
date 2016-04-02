using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// List of overloads for a function call.
    /// </summary>
    internal class OverloadsList
    {
        /// <summary>
        /// Name of the functions, as a string literal or an expression in case of indirect function call.
        /// </summary>
        BoundExpression _nameExpr;

        /// <summary>
        /// In case of a direct call, gets the function name. Otherwise gets <c>null</c>.
        /// </summary>
        public string DirectName
        {
            get
            {
                if (_nameExpr.ConstantValue.HasValue)
                {
                    var value = _nameExpr.ConstantValue.Value;
                    if (value is string) return (string)value;
                    throw new NotImplementedException();    // TODO: conversion object -> string
                }

                return null;
            }
            set
            {
                Contract.ThrowIfNull(value);
                _nameExpr = new BoundLiteral(value);
            }
        }

        /// <summary>
        /// The name of called function as an expression in case of indirect function call.
        /// </summary>
        public BoundExpression NameExpr => _nameExpr;

        /// <summary>
        /// Gets or sets value indicating the list of overloads is final and cannot be extended in runtime.
        /// </summary>
        public bool IsFinal { get; set; }

        /// <summary>
        /// List of function call candidates.
        /// </summary>
        public ImmutableArray<MethodSymbol> Candidates => _candidates;

        ImmutableArray<MethodSymbol> _candidates;

        public OverloadsList(string name, IEnumerable<MethodSymbol> candidates)
        {
            this.DirectName = name;
            _candidates = candidates.AsImmutableOrEmpty();

            var kind = IsMethodKindConsistent();
            this.IsFinal |= (kind == MethodKind.Constructor || kind == MethodKind.StaticConstructor || kind == MethodKind.BuiltinOperator);
        }

        /// <summary>
        /// Prefer instance methods.
        /// </summary>
        public void WithInstanceCall(TypeSymbol t)
        {
            IsFinal |= (t.IsSealed);   // type is sealed -> no more possible overrides or overloads
        }

        /// <summary>
        /// Prefer instance methods.
        /// </summary>
        public void WithInstanceCall(TypeRefContext ctx, TypeRefMask tmask)
        {
            if (!tmask.IsAnyType)
            {
                IsFinal |= (!tmask.IncludesSubclasses);    // instance type does not include subclasses -> there won't be any other overrides or overloads
            }
        }

        /// <summary>
        /// Prefer methods having at least given amount of mandatory parameters count.
        /// </summary>
        /// <param name="count">Passed arguments count.</param>
        public void WithParametersCount(int count)
        {
            var better = _candidates.WhereAsArray(s => s.MandatoryParamsCount == count || (s.IsParams() && s.ParameterCount - 1 <= count));
            if (better.Length != 0)
            {
                _candidates = better;
            }

            //Filter(s => s.MandatoryParamsCount <= count); // not provided mandatory parameter has default value in PHP
        }

        public void WithParametersType(TypeRefContext ctx, TypeRefMask[] ptypes)
        {
            WithParametersCount(ptypes.Length);

            // TODO: remove candidates that cannot be called with given arguments (an argument is not convertible to a parameter type)

            // TODO: filter candidates which parameters are convertible from provided types
            // prefer single candidate matching types perfectly

            // var expected = s.GetExpectedParamType(ctx, index);
            // { tmask is convertible to expected} ?
            // any candidate with perfect match ?
        }

        /// <summary>
        /// Checks all candidates expect or do not expect <c>this</c> parameter on evaluation stack.
        /// </summary>
        /// <returns></returns>
        internal bool IsStaticIsConsistent()
        {
            if (_candidates.Length != 0)
            {
                return _candidates.All(c => c.IsStatic == _candidates[0].IsStatic);
            }

            return true;
        }

        internal bool IsImplicitlyDeclaredParameter(int index)
        {
            return _candidates.Length != 0 && _candidates.All(c => c.ParameterCount > index && c.Parameters[index].IsImplicitlyDeclared);
        }

        internal TypeSymbol ConsistentParameterType(int index)
        {
            if (_candidates.Length != 0)
            {
                var first = _candidates[0];
                var t = (first.ParameterCount > index) ? first.Parameters[index].Type : null;
                if (_candidates.Length == 1 || _candidates.All(c => c.ParameterCount > index && c.Parameters[index].Type == t))
                    return t;
            }

            return null;
        }

        internal MethodKind IsMethodKindConsistent()
        {
            if (_candidates.Length != 0)
            {
                var kind = _candidates[0].MethodKind;
                if (_candidates.Length == 1 || _candidates.All(c => c.MethodKind == kind))
                    return kind;
            }

            return (MethodKind)(-1);
        }

        /// <summary>
        /// Find overloads that can be called with given arguments loaded on evaluation stack.
        /// </summary>
        internal IEnumerable<MethodSymbol> FindByLoadedArgs(IList<TypeSymbol> emittedArgs)
        {
            foreach (var c in _candidates)
            {
                var ps = c.ParametersType();
                var matches = true;
                for (int i = 0; i < ps.Length && i < emittedArgs.Count; i++)
                {
                    if (!emittedArgs[i].IsOfType(ps[i]))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    yield return c;
            }
        }
    }
}
