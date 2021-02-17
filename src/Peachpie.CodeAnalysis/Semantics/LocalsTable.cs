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

        readonly Dictionary<VariableName, LocalVariableReference>/*!*/_dict = new Dictionary<VariableName, LocalVariableReference>();

        /// <summary>
        /// Enumeration of direct local variables.
        /// </summary>
        public IEnumerable<LocalVariableReference> Variables => _dict.Values;

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
                _dict[new VariableName(p.Name)] = new ParameterReference(p, Routine);
            }

            // $this
            if (_routine.GetPhpThisVariablePlace() != null) // NOTE: even global code has $this  => routine.HasThis may be false
            {
                _dict[VariableName.ThisVariableName] = new ThisVariableReference(_routine);
            }
        }

        LocalVariableReference CreateAutoGlobal(VariableName name, TextSpan span)
        {
            return new SuperglobalVariableReference(name, Routine);
        }

        LocalVariableReference CreateLocal(VariableName name, VariableKind kind, TextSpan span)
        {
            Debug.Assert(!name.IsAutoGlobal);
            return new LocalVariableReference(kind, Routine, new SourceLocalSymbol(Routine, name.Value, span), new BoundVariableName(name));
        }

        #region Public methods

        public bool TryGetVariable(VariableName varname, out LocalVariableReference variable) => _dict.TryGetValue(varname, out variable);

        IVariableReference BindVariable(VariableName varname, TextSpan span, Func<VariableName, TextSpan, LocalVariableReference> factory)
        {
            if (!_dict.TryGetValue(varname, out var value))
            {
                _dict[varname] = value = factory(varname, span);
            }

            //
            Debug.Assert(value != null);
            return value;
        }

        /// <summary>
        /// Gets local variable or create local if not yet.
        /// </summary>
        public IVariableReference BindLocalVariable(VariableName varname, TextSpan span) => BindVariable(varname, span, (name, span) => CreateLocal(name, VariableKind.LocalVariable, span));

        public IVariableReference BindTemporalVariable(VariableName varname) => BindVariable(varname, default, (name, span) => CreateLocal(name, VariableKind.LocalTemporalVariable, span));

        public IVariableReference BindAutoGlobalVariable(VariableName varname) => BindVariable(varname, default, (name, span) => CreateAutoGlobal(name, span));

        #endregion
    }
}
