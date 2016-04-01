using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    /// <summary>
    /// Helper class resolving method candidates in compile time.
    /// </summary>
    partial class OverloadResolution
    {
        string _nameOpt;

        bool _isFinal;
        bool _isStatic;
        
        /// <summary>
        /// Gets value indicating the list of candidates is final and possible single candidate can be called statically.
        /// </summary>
        public bool IsFinal => _isFinal;

        /// <summary>
        /// Gets value indicating whether the overloads were resolved as static calls.
        /// </summary>
        public bool IsStaticCall => _isStatic;

        ImmutableArray<MethodSymbol> _candidates;

        public ImmutableArray<MethodSymbol> Candidates => _candidates;

        /// <summary>
        /// Gets reference to one and the only target routine.
        /// </summary>
        internal MethodSymbol SingleOrNothing => (_candidates.Length == 1 && _isFinal) ? _candidates[0] : null;

        void Filter(Func<MethodSymbol, bool> predicate)
        {
            _candidates = _candidates.WhereAsArray(predicate);
        }

        public OverloadResolution(IEnumerable<MethodSymbol> candidates)
        {
            _candidates = candidates.AsImmutableOrEmpty();

            var kind = IsMethodKindConsistent();
            _isFinal |= (kind == MethodKind.Constructor || kind == MethodKind.StaticConstructor);
        }

        public OverloadResolution Clone()
        {
            return new OverloadResolution(_candidates)
            {
                _isFinal = _isFinal,
                _nameOpt = _nameOpt,
            };
        }

        /// <summary>
        /// Include only methods with given name.
        /// </summary>
        public void WithName(string name)
        {
            _nameOpt = name;
            Filter(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Prefer static methods with known containing type (same as global methods).
        /// </summary>
        public void WithStaticCall()
        {
            _isStatic = true;
            _isFinal = true;   // static calls are final (no more overloads possible)
            Filter(s => s.IsStatic);
        }

        /// <summary>
        /// Prefer instance methods.
        /// </summary>
        public void WithInstanceType(TypeSymbol t)
        {
            _isStatic = false;
            _isFinal |= (t.IsSealed);   // type is sealed -> no more possible overrides
        }

        /// <summary>
        /// Prefer methods having at least given amount of mandatory parameters count.
        /// </summary>
        /// <param name="count">Passed arguments count.</param>
        public void WithParametersCount(int count)
        {
            var better = _candidates.WhereAsArray(s => s.MandatoryParamsCount == count || (s.MandatoryParamsCount < count && s.IsParams()));
            if (better.Length != 0)
            {
                _candidates = better;
            }

            //Filter(s => s.MandatoryParamsCount <= count); // not provided mandatory parameter has default value in PHP
        }

        /// <summary>
        /// Prefer methods having i-th parameter of given type.
        /// </summary>
        public void WithParameterType(TypeSymbol pt, int pi)
        {
            _candidates = CandidatesWithParameterType(pt, pi);
        }
    }
}
