using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Interface providing access standard to PHP context options.
    /// </summary>
    public interface IPhpOptionService
    {
        /// <summary>
        /// Gets actual option value.
        /// </summary>
        PhpValue GetValue(string option);

        /// <summary>
        /// Sets new option value.
        /// </summary>
        void SetValue(string option, PhpValue value);

        /// <summary>
        /// Gets default option value.
        /// </summary>
        PhpValue GetDefaultValue(string option);

        #region Standard Options

        // TOOD: type safe standard options as properties

        #endregion
    }

    partial class Context
    {
        protected class DefaultPhpOptionService : IPhpOptionService
        {
            //[DebuggerDisplay("PhpOption [{name}: {value}]")]
            //public struct PhpOption
            //{
            //    public string name;
            //    public PhpValue value;
            //    public Func<PhpValue> Getter;
            //    public Action<PhpValue> Setter;

            //    public PhpOption(string name, PhpValue value)
            //    {
            //        this.name = name;
            //        this.value = value;
            //    }
            //}

            static readonly Dictionary<string, PhpValue> _defaults = new Dictionary<string, PhpValue>(StringComparer.Ordinal) {
                {"memory_limit", (PhpValue)"128M" }
            };

            Dictionary<string, PhpValue> _options = new Dictionary<string, PhpValue>(StringComparer.Ordinal);

            public PhpValue GetDefaultValue(string option)
            {
                PhpValue value;
                if (!_defaults.TryGetValue(option, out value))
                {
                    value = PhpValue.Null;
                }

                return value;
            }

            public PhpValue GetValue(string option)
            {
                PhpValue value;
                if (!_options.TryGetValue(option, out value) && !_defaults.TryGetValue(option, out value))
                {
                    value = PhpValue.Null;
                }

                return value;
            }

            public void SetValue(string option, PhpValue value)
            {
                _options[option] = value;
            }
        }

        DefaultPhpOptionService _options;

        /// <summary>
        /// Gets service providing access to standard PHP options.
        /// Can be a <c>null</c> reference.
        /// </summary>
        public virtual IPhpOptionService Options => _options ?? (_options = new DefaultPhpOptionService());
    }
}
