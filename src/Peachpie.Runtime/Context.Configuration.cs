using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Core
{
    #region IPhpConfigurationService

    /// <summary>
    /// Interface providing access to configuration.
    /// </summary>
    public interface IPhpConfigurationService : IEnumerable<IPhpConfiguration>
    {
        /// <summary>
        /// Gets collection of options.
        /// </summary>
        /// <typeparam name="TOptions">Type providing the options.</typeparam>
        /// <returns>The instance of options or <c>null</c> if such options were not registered.</returns>
        TOptions Get<TOptions>() where TOptions : class, IPhpConfiguration;

        /// <summary>
        /// Gets parent service providing default values.
        /// </summary>
        IPhpConfigurationService Parent { get; }

        /// <summary>
        /// Gets core configuration.
        /// Alias to <see cref="Get{PhpCoreConfiguration}()"/>.
        /// </summary>
        PhpCoreConfiguration Core { get; }
    }

    #endregion

    #region IPhpConfiguration, PhpCoreConfiguration

    /// <summary>
    /// 
    /// </summary>
    public interface IPhpConfiguration
    {
        /// <summary>
        /// Gets the corresponding extension name.
        /// </summary>
        string ExtensionName { get; }

        /// <summary>
        /// Creates the configuration copy.
        /// </summary>
        /// <returns>New instance of <c>this</c> with copied values.</returns>
        IPhpConfiguration Copy();
    }

    public sealed class PhpCoreConfiguration : IPhpConfiguration
    {
        #region IPhpConfiguration

        internal PhpCoreConfiguration()
        {

        }

        public string ExtensionName => "Core";

        public IPhpConfiguration Copy() => (PhpCoreConfiguration)this.MemberwiseClone();

        #endregion

        /// <summary>
        /// The order in which global will be added to <c>$GLOBALS</c> and 
        /// <c>$_REQUEST</c> arrays. Can contain only a permutation of "EGPCS" string.
        /// </summary>
        public string RegisteringOrder
        {
            get
            {
                return _registeringOrder;
            }
            set
            {
                if (ValidateRegisteringOrder(value))
                {
                    _registeringOrder = value;
                }
            }
        }
        string _registeringOrder = "EGPCS";

        /// <summary>
        /// Checks whether a specified value is global valid variables registering order.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Whether <paramref name="value"/> contains a permutation of "EGPCS".</returns>
        public static bool ValidateRegisteringOrder(string value)
        {
            if (value == null || value.Length != 5) return false;

            int present = 0;
            for (int i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case 'E': if ((present & 1) != 0) return false; present |= 1; break;
                    case 'G': if ((present & 2) != 0) return false; present |= 2; break;
                    case 'P': if ((present & 4) != 0) return false; present |= 4; break;
                    case 'C': if ((present & 8) != 0) return false; present |= 8; break;
                    case 'S': if ((present & 16) != 0) return false; present |= 16; break;
                    default: return false;
                }
            }
            return true;
        }

        /// <summary>
        /// <c>variables_order</c> directive.
        /// </summary>
        public string VariablesOrder { get; set; } = "EGPCS";

        #region Request Control

        /// <summary>
        /// Execution timeout in seconds.
        /// </summary>
        public int ExecutionTimeout = 30;

        /// <summary>
        /// Whether not to abort on client disconnection.
        /// </summary>
        public bool IgnoreUserAbort = true;

        #endregion

        #region File System

        /// <summary>
        /// Whether file names can be specified as URL (and thus allows to use streams).
        /// </summary>
        public bool AllowUrlFopen = true;

        /// <summary>
        /// A user agent to send when communicating as client over HTTP.
        /// </summary>
        public string UserAgent = null;

        /// <summary>
        /// Default timeout for socket based streams.
        /// </summary>
        public int DefaultSocketTimeout = 60;

        /// <summary>
        /// A default file open mode used when it is not specified in <c>fopen</c> function explicitly. 
        /// You can specify either "b" for binary mode or "t" for text mode. Any other value is treated as
        /// if there is no default value.
        /// </summary>
        public string DefaultFileOpenMode = "b";

        /// <summary>
        /// A password used when logging to FTP server as an anonymous client.
        /// </summary>
        public string AnonymousFtpPassword = null;

        /// <summary>
        /// A list of semicolon-separated separated by ';' where file system functions and dynamic 
        /// inclusion constructs searches for files. A <B>null</B> or an empty string means empty list.
        /// </summary>
        public string IncludePaths = ".";

        #endregion

        #region Mailer

        public string SmtpServer = null;

        public int SmtpPort = 25;

        public bool AddXHeader = false;

        public string DefaultFromHeader = null;

        #endregion
    }

    #endregion

    partial class Context
    {
        #region DefaultPhpConfigurationService, PhpConfigurationService

        class DefaultPhpConfigurationService : IPhpConfigurationService
        {
            public static readonly DefaultPhpConfigurationService Instance = new DefaultPhpConfigurationService();

            public PhpCoreConfiguration Core => Get<PhpCoreConfiguration>();

            public IPhpConfigurationService Parent => null;

            public TOptions Get<TOptions>() where TOptions : class, IPhpConfiguration
            {
                IPhpConfiguration value;
                return _defaultConfigs.TryGetValue(typeof(TOptions), out value) ? (TOptions)value : null;
            }

            IEnumerator<IPhpConfiguration> IEnumerable<IPhpConfiguration>.GetEnumerator() => _defaultConfigs.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IPhpConfiguration>)this).GetEnumerator();
        }

        protected class PhpConfigurationService : IPhpConfigurationService
        {
            readonly Dictionary<Type, IPhpConfiguration> _configs;
            readonly PhpCoreConfiguration _core;

            public PhpConfigurationService()
            {
                // clone parent configuration
                _configs = new Dictionary<Type, IPhpConfiguration>(_defaultConfigs.Count);
                _core = new PhpCoreConfiguration(); // TODO: RegisterConfiguration<PhpCoreConfiguration>
            }

            public PhpCoreConfiguration Core => _core;

            public IPhpConfigurationService Parent => DefaultPhpConfigurationService.Instance;

            public virtual TOptions Get<TOptions>() where TOptions : class, IPhpConfiguration
            {
                var key = typeof(TOptions);

                IPhpConfiguration value;
                if (!_configs.TryGetValue(key, out value))
                {
                    if (_defaultConfigs.TryGetValue(key, out value))
                    {
                        // lazy clone default configuration
                        _configs[key] = value = value.Copy();
                    }
                }

                //
                return (TOptions)value;
            }

            IEnumerator<IPhpConfiguration> IEnumerable<IPhpConfiguration>.GetEnumerator()
            {
                // collect _configs & _defaultConfigs distinctly
                var seen = new HashSet<Type>();
                foreach (var pair in _configs.Concat(_defaultConfigs))
                {
                    if (seen.Add(pair.Key))
                    {
                        yield return pair.Value;
                    }
                }

                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IPhpConfiguration>)this).GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Gets a service providing access to current runtime configuration.
        /// </summary>
        public virtual IPhpConfigurationService Configuration => _configuration;
        readonly IPhpConfigurationService _configuration = new PhpConfigurationService();

        /// <summary>
        /// Registers a configuration to be accessed through <see cref="IPhpConfigurationService.Get{TOptions}"/> with default values.
        /// </summary>
        /// <typeparam name="TOptions">Type of the configuration interface.</typeparam>
        /// <param name="defaults">The instance providing default values.
        /// This instance is intended to be cloned for new <see cref="Context"/> instances.</param>
        public static void RegisterConfiguration<TOptions>(TOptions defaults) where TOptions : class, IPhpConfiguration
        {
            if (defaults == null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            _defaultConfigs[typeof(TOptions)] = defaults;
        }

        /// <summary>
        /// Set of registered configurations and their default values.
        /// </summary>
        static readonly Dictionary<Type, IPhpConfiguration> _defaultConfigs = new Dictionary<Type, IPhpConfiguration>();
    }
}
