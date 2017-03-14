using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
            PopuplateParameters();
        }

        #endregion

        void PopuplateParameters()
        {
            // parameters
            foreach (var p in _routine.SourceParameters)
            {
                _dict[new VariableName(p.Name)] = new BoundParameter(p, p.Initializer);
            }

            // $this
            if (_routine.ThisParameter != null) // NOTE: even global code has $this  => routine.HasThis may be false
            {
                _dict[VariableName.ThisVariableName] = new BoundParameter(_routine.ThisParameter, null)
                {
                    VariableKind = VariableKind.ThisParameter
                };
            }
        }

        BoundVariable CreateVariable(VariableName name, VariableKind kind, Func<BoundExpression> initializer)
        {
            switch (kind)
            {
                case VariableKind.LocalVariable:
                    Debug.Assert(initializer == null);
                    return new BoundLocal(new SourceLocalSymbol(_routine, name.Value, kind));

                case VariableKind.StaticVariable:
                    return new BoundStaticLocal(new SourceLocalSymbol(_routine, name.Value, kind), initializer?.Invoke());

                case VariableKind.GlobalVariable:
                    Debug.Assert(initializer == null);
                    return new BoundGlobalVariable(name);

                default:
                    Debug.Assert(initializer == null);
                    throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(kind);
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
        /// Gets kind of declared variable or <see cref="VariableKind.LocalVariable"/> by default.
        /// </summary>
        public VariableKind GetVariableKind(VariableName varname)
        {
            BoundVariable value;
            return _dict.TryGetValue(varname, out value)
                ? value.VariableKind
                : varname.IsAutoGlobal
                    ? VariableKind.GlobalVariable
                    : VariableKind.LocalVariable;
        }

        /// <summary>
        /// Gets local variable or create local if not yet.
        /// </summary>
        public BoundVariable BindVariable(VariableName varname, VariableKind kind, Func<BoundExpression> initializer = null)
        {
            BoundVariable value;

            if (_dict.TryGetValue(varname, out value))
            {
                if (value.VariableKind != kind)
                {
                    // variable redeclared with a different kind
                    throw new ArgumentException("", nameof(kind));
                }
            }
            else
            {
                _dict[varname] = value = CreateVariable(varname, kind, initializer);
            }

            //
            Debug.Assert(value != null);
            return value;
        }

        #endregion
    }
}
