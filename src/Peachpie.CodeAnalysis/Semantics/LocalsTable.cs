using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Table of local variables used within routine.
    /// </summary>
    internal sealed partial class LocalsTable
    {
        #region Fields & Properties

        readonly Dictionary<VariableName, BoundVariable>/*!*/_dict = new Dictionary<VariableName, BoundVariable>();

        /// <summary>
        /// Enumeration of direct local variables.
        /// </summary>
        public IEnumerable<BoundVariable> Variables => _dict.Values;

        /// <summary>
        /// Count of local variables.
        /// </summary>
        public int Count => _dict.Count;

        /// <summary>
        /// Containing routine. Cannot be <c>null</c>.
        /// </summary>
        public SourceRoutineSymbol Routine => _routine;
        readonly SourceRoutineSymbol _routine;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes table of locals of given routine.
        /// </summary>
        public LocalsTable(SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(routine);

            _routine = routine;

            //
            PopulateParameters();
        }

        #endregion

        void PopulateParameters()
        {
            // parameters
            foreach (var p in _routine.SourceParameters)
            {
                _dict[new VariableName(p.Name)] = new BoundParameter(p, p.Initializer);
            }

            // $this
            if (_routine.ThisParameter != null) // NOTE: even global code has $this  => routine.HasThis may be false
            {
                _dict[VariableName.ThisVariableName] = new BoundThisParameter(_routine);
            }
        }

        BoundVariable CreateVariable(VariableName name, TextSpan span, bool isTemporal = false)
        {
            if (name.IsAutoGlobal)
            {
                Debug.Assert(!isTemporal);
                return new BoundSuperGlobalVariable(name);
            }
            else
            {
                return (isTemporal)
                    ? new BoundLocal(new SourceLocalSymbol(_routine, name.Value, span), VariableKind.LocalTemporalVariable)
                    : new BoundLocal(new SourceLocalSymbol(_routine, name.Value, span));
            }
        }

        #region Public methods

        /// <summary>
        /// Gets variables with given name.
        /// There might be more variables with same name of a different kind.
        /// </summary>
        public IEnumerable<BoundVariable> GetVariables(VariableName name)
        {
            foreach (var v in Variables)
            {
                if (v.Name != null && new VariableName(v.Name).Equals(name))
                {
                    yield return v;
                }
            }
        }

        /// <summary>
        /// Gets enumeration of local variables.
        /// </summary>
        public IEnumerable<BoundVariable> GetVariables()
        {
            return Variables;
        }

        /// <summary>
        /// Gets local variable or create local if not yet.
        /// </summary>
        public BoundVariable BindVariable(VariableName varname, TextSpan span, bool isTemporal = false)
        {
            BoundVariable value;

            if (!_dict.TryGetValue(varname, out value))
            {
                _dict[varname] = value = CreateVariable(varname, span, isTemporal);
            }

            //
            Debug.Assert(isTemporal ^ value.VariableKind != VariableKind.LocalTemporalVariable);
            Debug.Assert(value != null);
            return value;
        }

        #endregion
    }
}
