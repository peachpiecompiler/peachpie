using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    internal static class ExtensionsAppContext
    {
        public static readonly ExtensionsTable ExtensionsTable = new ExtensionsTable();
    }

    /// <summary>
    /// Table of loaded extensions.
    /// </summary>
    internal class ExtensionsTable
    {
        /// <summary>
        /// Reflected information about a single extensions.
        /// </summary>
        [DebuggerDisplay("{Name} (functions: {Routines.Count}, types: {Types.Count})")]
        sealed class ExtensionInfo
        {
            /// <summary>
            /// The extension name.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Collection of routines within the extensions. Cannot be <c>null</c>.
            /// </summary>
            public ICollection<ClrRoutineInfo> Routines { get; } = new HashSet<ClrRoutineInfo>();

            /// <summary>
            /// Collection of classes and interface within the extension. Cannot be <c>null</c>.
            /// </summary>
            public ICollection<PhpTypeInfo> Types { get; } = new HashSet<PhpTypeInfo>();

            public ExtensionInfo(string name)
            {
                Debug.Assert(!string.IsNullOrEmpty(name));
                this.Name = name ?? throw new ArgumentNullException(nameof(name));
            }
        }

        /// <summary>
        /// Table of known extensions and their declarations.
        /// Filled lazily. Cannot be <c>null</c>.
        /// </summary>
        readonly Dictionary<string, ExtensionInfo>/*!*/_extensions = new Dictionary<string, ExtensionInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Already processed assemblies, or types.
        /// </summary>
        readonly HashSet<object>/*!*/_processed = new HashSet<object>();

        /// <summary>
        /// Gets known extension names.
        /// </summary>
        /// <returns>Collection of extensions. Cannot be <c>null</c>.</returns>
        public ICollection<string> GetExtensions() => _extensions.Keys;

        /// <summary>
        /// Gets routines associated with specified extension.
        /// </summary>
        /// <param name="extension">Extension name.</param>
        /// <returns>Enumeration of routines associated with given extension.</returns>
        public ICollection<ClrRoutineInfo> GetRoutinesByExtension(string extension)
        {
            return extension != null && _extensions.TryGetValue(extension, out var info)
                ? info.Routines
                : Array.Empty<ClrRoutineInfo>();
        }

        /// <summary>
        /// Gets enumeration of types associated with specified extension.
        /// </summary>
        public ICollection<PhpTypeInfo> GetTypesByExtension(string extension)
        {
            return extension != null && _extensions.TryGetValue(extension, out var info)
                ? info.Types
                : Array.Empty<PhpTypeInfo>();
        }

        /// <summary>
        /// Gets value indicating that given extension was loaded.
        /// </summary>
        public bool ContainsExtension(string extension) => extension != null && _extensions.ContainsKey(extension);

        /// <summary>
        /// Adds routine within its associated extension(s).
        /// </summary>
        /// <param name="attr">Corresponding <see cref="PhpExtensionAttribute"/> of the containing class. Can be <c>null</c>.</param>
        /// <param name="routine">Library routine to be included into the table.</param>
        internal void AddRoutine(PhpExtensionAttribute attr, ClrRoutineInfo/*!*/routine)
        {
            if (attr == null)
            {
                return;
            }

            var extensions = attr.Extensions;
            for (int i = 0; i < extensions.Length; i++)
            {
                EnsureExtensionInfo(extensions[i]).Routines.Add(routine);
            }
        }

        public void AddType(PhpTypeInfo type)
        {
            Debug.Assert(type != null);

            var extensionName = type.ExtensionName;
            if (extensionName != null)
            {
                EnsureExtensionInfo(extensionName).Types.Add(type);
            }
        }

        ExtensionInfo/*!*/EnsureExtensionInfo(string extension)
        {
            if (!_extensions.TryGetValue(extension, out var info))
            {
                _extensions[extension] = info = new ExtensionInfo(extension);
            }

            return info;
        }

        /// <summary>
        /// Reflects <see cref="PhpExtensionAttribute"/> of given type once.
        /// </summary>
        /// <returns>Value indicating the type was visited for the first time.</returns>
        internal bool VisitFunctionsContainer(Type/*!*/container, out PhpExtensionAttribute attr)
        {
            if (_processed.Add(container))
            {
                VisitAssembly(container.Assembly);

                VisitAttribute(attr = container.GetCustomAttribute<PhpExtensionAttribute>());

                return true;
            }
            else
            {
                attr = null;
                return false;
            }
        }

        /// <summary>
        /// Adds extensions specified within the assembly attribute into the table.
        /// </summary>
        /// <param name="ass">The assembly to be added.</param>
        /// <returns>Value indicating the assembly was visited for the first time.</returns>
        internal bool VisitAssembly(Assembly ass)
        {
            if (_processed.Add(ass))
            {
                var attrs = ass.GetCustomAttributes<PhpExtensionAttribute>();
                foreach (var attr in attrs)
                {
                    VisitAttribute(attr);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        void VisitAttribute(PhpExtensionAttribute attr)
        {
            if (attr == null)
            {
                return;
            }

            var extensions = attr.Extensions;
            for (int i = 0; i < extensions.Length; i++)
            {
                EnsureExtensionInfo(extensions[i]);
            }

            var registrator = attr.Registrator;
            if (registrator != null)
            {
                Debug.WriteLine($"Creating '{registrator.FullName}' ...");
                Activator.CreateInstance(registrator); // CONSIDER: dependency injection, global service provider
            }
        }
    }
}
