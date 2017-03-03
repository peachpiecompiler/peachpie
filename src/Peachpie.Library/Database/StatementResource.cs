using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Pchp.Library.Database
{
    /// <summary>
	/// Represents a parameterized SQL statement.
	/// </summary>
	[PhpHidden]
    public abstract class StatementResource : PhpResource
    {
        #region Enum: ParameterType

        /// <summary>
        /// PHP type of the parameter. Parameter value will be converted accordign to this value.
        /// </summary>
        public enum ParameterType
        {
            Invalid = 0,
            String = 1,
            Double = 2,
            Integer = 3,
            Null = 4,
            Infer = 5
        }

        #endregion

        #region Bindings

        private struct Binding
        {
            public PhpAlias/*!*/ Variable;
            public IDataParameter/*!*/ Parameter;
            public ParameterType Type;

            public Binding(PhpAlias/*!*/ variable, IDataParameter/*!*/ parameter, ParameterType type)
            {
                Debug.Assert(variable != null && parameter != null && type != ParameterType.Invalid);

                this.Variable = variable;
                this.Parameter = parameter;
                this.Type = type;
            }
        }

        private Dictionary<string, Binding> Bindings
        {
            get
            {
                if (_bindings == null)
                {
                    _bindings = new Dictionary<string, Binding>(StringComparer.OrdinalIgnoreCase);
                }

                return _bindings;
            }
        }
        private Dictionary<string, Binding> _bindings;

        private bool BindingsDefined { get { return _bindings != null; } }

        #endregion

        /// <summary>
        /// Connection resource associated with the statement.
        /// </summary>
        public ConnectionResource/*!*/ Connection { get { return connection; } }
        protected ConnectionResource/*!*/ connection;

        /// <summary>
        /// Creates an instance of parameterized statement.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="connection">Database connection resource.</param>
        public StatementResource(string/*!*/ resourceName, ConnectionResource/*!*/ connection)
            : base(resourceName)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            this.connection = connection;
        }

        /// <summary>
        /// Adds a parameter to variable binding.
        /// </summary>
        /// <param name="parameter">SQL parameter.</param>
        /// <param name="variable">Passed PHP variable.</param>
        /// <param name="type">Parameter type specified by user.</param>
        /// <returns><B>true</B> if the binding succeeded.</returns>
        public bool AddBinding(IDataParameter/*!*/ parameter, PhpAlias/*!*/ variable, ParameterType type)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            if (type < ParameterType.String || type > ParameterType.Infer)
                throw new ArgumentOutOfRangeException(nameof(type));

            if (Bindings.ContainsKey(parameter.ParameterName))
                return false;

            Bindings.Add(parameter.ParameterName, new Binding(variable, parameter, type));
            return true;
        }

        /// <summary>
        /// Loads data from bound variables to the respective parameters.
        /// </summary>
        /// <returns>An array of parameters with loaded values.</returns>
        public IDataParameter[] PrepareParameters()
        {
            if (!BindingsDefined) return new IDataParameter[0];

            IDataParameter[] parameters = new IDataParameter[Bindings.Count];

            int i = 0;
            foreach (Binding binding in Bindings.Values)
            {
                if (binding.Parameter.Direction == ParameterDirection.InputOutput || binding.Parameter.Direction == ParameterDirection.Input)
                {
                    switch (binding.Type)
                    {
                        case ParameterType.Double: binding.Parameter.Value = binding.Variable.ToDouble(); break;
                        case ParameterType.String: binding.Parameter.Value = binding.Variable.ToString(); break;
                        case ParameterType.Integer: binding.Parameter.Value = (int)binding.Variable.ToLong(); break;
                        case ParameterType.Null: binding.Parameter.Value = DBNull.Value; break;
                        case ParameterType.Infer: binding.Parameter.Value = binding.Variable.Value.ToClr(); break;
                        default: Debug.Fail(null); break;
                    }
                }

                parameters[i++] = binding.Parameter;
            }

            return parameters;
        }

        /// <summary>
        /// Writes parameter values back to the bound variables.
        /// </summary>
        public void WriteParametersBack()
        {
            if (!BindingsDefined) return;

            foreach (Binding binding in Bindings.Values)
            {
                if (binding.Parameter.Direction != ParameterDirection.Input)
                {
                    switch (binding.Type)
                    {
                        case ParameterType.Double: // binding.Variable.Value = Core.Convert.ObjectToDouble(binding.Parameter.Value); break;
                        case ParameterType.String: // binding.Variable.Value = Core.Convert.ObjectToString(binding.Parameter.Value); break;
                        case ParameterType.Integer: // binding.Variable.Value = Core.Convert.ObjectToInteger(binding.Parameter.Value); break;
                        case ParameterType.Null:
                        case ParameterType.Infer: binding.Variable.Value = PhpValue.FromClr(binding.Parameter.Value); ; break;
                        default: Debug.Fail(null); break;
                    }
                }
            }
        }
    }
}
