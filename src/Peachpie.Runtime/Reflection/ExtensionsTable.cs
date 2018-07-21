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
            public readonly string Name;
            public readonly ICollection<ClrRoutineInfo> Routines = new HashSet<ClrRoutineInfo>();
            public readonly ICollection<PhpTypeInfo> Types = new HashSet<PhpTypeInfo>();

            public ExtensionInfo(string name)
            {
                Debug.Assert(!string.IsNullOrEmpty(name));
                this.Name = name;
            }
        }

        /// <summary>
        /// Table of known extensions and their declarations.
        /// Filled lazily. Cannot be <c>null</c>.
        /// </summary>
        readonly Dictionary<string, ExtensionInfo>/*!*/_extensions = new Dictionary<string, ExtensionInfo>(StringComparer.Ordinal);

        /// <summary>
        /// Set of <see cref="PhpExtensionAttribute.Registrator"/> values being instantiated to avoid repetitious initializations.
        /// </summary>
        readonly HashSet<RuntimeTypeHandle>/*!*/_touchedRegistrators = new HashSet<RuntimeTypeHandle>();

        /// <summary>
        /// Assemblies already processed.
        /// </summary>
        readonly HashSet<string>/*!*/_touchedAssemblies = new HashSet<string>(StringComparer.Ordinal);

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
            if (extension != null && _extensions.TryGetValue(extension, out var info))
            {
                return info.Routines;
            }
            else
            {
                return Array.Empty<ClrRoutineInfo>();
            }
        }

        /// <summary>
        /// Gets enumeration of types associated with specified extension.
        /// </summary>
        public ICollection<PhpTypeInfo> GetTypesByExtension(string extension)
        {
            if (extension != null && _extensions.TryGetValue(extension, out var info))
            {
                return info.Types;
            }
            else
            {
                return Array.Empty<PhpTypeInfo>();
            }
        }

        /// <summary>
        /// Gets value indicating that given extension was loaded.
        /// </summary>
        public bool ContainsExtension(string extension)
        {
            return extension != null && _extensions.ContainsKey(extension);
        }

        bool AddAssembly(AssemblyName assname)
        {
            lock (_touchedAssemblies)
            {
                return _touchedAssemblies.Add(assname.FullName);
            }
        }

        /// <summary>
        /// Adds extensions specified within the assembly attribute into the table.
        /// </summary>
        /// <param name="ass">The assembly to be added.</param>
        /// <returns>The same value as provided in <paramref name="ass"/>.</returns>
        public Assembly AddAssembly(Assembly ass)
        {
            if (AddAssembly(ass.GetName()))
            {
                var attrs = ass.GetCustomAttributes<PhpExtensionAttribute>();
                if (attrs != null)
                {
                    foreach (var attr in attrs)
                    {
                        EnsureExtensions(attr.Extensions);
                        VisitExtensionAttribute(attr);
                    }
                }
            }

            return ass;
        }

        /// <summary>
        /// Adds routine within its associated extension.
        /// </summary>
        /// <param name="routine">Library routine to be included in the table.</param>
        public void AddRoutine(ClrRoutineInfo routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            foreach (var m in routine.Methods)
            {
                var tinfo = m.DeclaringType.GetTypeInfo();

                //
                var extinfo = tinfo.GetCustomAttribute<PhpExtensionAttribute>();
                if (extinfo != null)
                {
                    AddRoutine(extinfo, routine);
                    VisitExtensionAttribute(extinfo);
                }

                //
                AddAssembly(tinfo.Assembly);
            }
        }

        public void AddType(PhpTypeInfo type)
        {
            Debug.Assert(type != null);

            var extensions = type.Extensions;
            if (extensions != null)
            {
                for (int i = 0; i < extensions.Length; i++)
                {
                    EnsureExtension(extensions[i]).Types.Add(type);
                }
            }
        }

        void AddRoutine(PhpExtensionAttribute extension, ClrRoutineInfo routine)
        {
            var extensions = extension.Extensions;
            for (int i = 0; i < extensions.Length; i++)
            {
                EnsureExtension(extensions[i]).Routines.Add(routine);
            }
        }

        void EnsureExtensions(string[] extensions)
        {
            if (extensions == null || extensions.Length == 0)
            {
                return;
            }

            lock (_extensions)
            {
                for (int i = 0; i < extensions.Length; i++)
                {
                    var extension = extensions[i];
                    if (!_extensions.ContainsKey(extension))
                    {
                        _extensions[extension] = new ExtensionInfo(extension);
                    }
                }
            }
        }

        ExtensionInfo EnsureExtension(string extension)
        {
            ExtensionInfo info;

            lock (_extensions)
            {
                if (!_extensions.TryGetValue(extension, out info))
                {
                    _extensions[extension] = info = new ExtensionInfo(extension);
                }
            }

            return info;
        }

        void VisitExtensionAttribute(PhpExtensionAttribute attr)
        {
            if (attr.Registrator != null && _touchedRegistrators.Add(attr.Registrator.TypeHandle))
            {
                Activator.CreateInstance(attr.Registrator);
                Debug.WriteLine($"Object of type {attr.Registrator.FullName} has been activated.");
            }
        }
    }
}
