using System;
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
    public interface IPhpConfigurationService
    {
        /// <summary>
        /// Gets collection of options.
        /// </summary>
        /// <typeparam name="TOptions">Interface defining the options.</typeparam>
        /// <returns>The instance of options or <c>null</c> if such options were not added.</returns>
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
        /// Creates the configuration copy.
        /// </summary>
        /// <returns>New instance of <c>this</c> with copied values.</returns>
        IPhpConfiguration Copy();
    }

    public sealed class PhpCoreConfiguration : IPhpConfiguration
    {
        #region Construction

        internal PhpCoreConfiguration()
        {

        }

        public IPhpConfiguration Copy() => (PhpCoreConfiguration)this.MemberwiseClone();

        #endregion

        #region Fields & Properties



        #endregion
    }

    #endregion

    partial class Context
    {
        #region StaticPhpConfigurationService, PhpConfigurationService

        class StaticPhpConfigurationService : IPhpConfigurationService
        {
            public static readonly StaticPhpConfigurationService Instance = new StaticPhpConfigurationService();

            public PhpCoreConfiguration Core => Get<PhpCoreConfiguration>();

            public IPhpConfigurationService Parent => null;

            public TOptions Get<TOptions>() where TOptions : class, IPhpConfiguration
            {
                IPhpConfiguration value;
                return _defaultConfigs.TryGetValue(typeof(TOptions), out value) ? (TOptions)value : null;
            }
        }

        protected class PhpConfigurationService : IPhpConfigurationService
        {
            readonly Dictionary<Type, IPhpConfiguration> _configs;

            public PhpConfigurationService()
            {
                // clone parent configuration

                _configs = new Dictionary<Type, IPhpConfiguration>(_defaultConfigs.Count);
                foreach (var cfg in _defaultConfigs)
                {
                    var newinst = cfg.Value.Copy();
                    Debug.Assert(newinst != null && cfg.Key.GetTypeInfo().IsAssignableFrom(newinst.GetType()));
                    _configs[cfg.Key] = newinst;
                }
            }

            public PhpCoreConfiguration Core => Get<PhpCoreConfiguration>();

            public IPhpConfigurationService Parent => StaticPhpConfigurationService.Instance;

            public virtual TOptions Get<TOptions>() where TOptions : class, IPhpConfiguration
            {
                var key = typeof(TOptions);

                IPhpConfiguration value;
                if (!_configs.TryGetValue(key, out value))
                {
                    if (_defaultConfigs.TryGetValue(key, out value))
                    {
                        // config was registered after the context was created
                        _configs[key] = value = value.Copy();
                    }
                }

                //
                return (TOptions)value;
            }
        }

        #endregion

        /// <summary>
        /// Gets a service providing access to current runtime configuration.
        /// </summary>
        public virtual IPhpConfigurationService Configuration => _configuration;
        readonly PhpConfigurationService _configuration = new PhpConfigurationService();

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

            _defaultConfigs.Add(typeof(TOptions), defaults);
        }

        /// <summary>
        /// Set of registered configurations and their default values.
        /// </summary>
        static readonly Dictionary<Type, IPhpConfiguration> _defaultConfigs = new Dictionary<Type, IPhpConfiguration>();
    }
}
