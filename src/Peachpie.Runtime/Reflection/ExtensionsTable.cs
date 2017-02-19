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
        /// Table of known extensions and their declared routines.
        /// Filled lazily. Cannot be <c>null</c>.
        /// </summary>
        readonly Dictionary<string, ICollection<ClrRoutineInfo>>/*!*/_routinesByExtension = new Dictionary<string, ICollection<ClrRoutineInfo>>(StringComparer.Ordinal);

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
        public ICollection<string> GetExtensions() => _routinesByExtension.Keys;

        /// <summary>
        /// Gets routines associated with specified extension.
        /// </summary>
        /// <param name="extension">Extension name.</param>
        /// <returns>Enumeration of routines associated with given extension.</returns>
        public ICollection<ClrRoutineInfo> GetRoutinesByExtension(string extension)
        {
            ICollection<ClrRoutineInfo> routines;
            if (extension != null && _routinesByExtension.TryGetValue(extension, out routines))
            {
                return routines;
            }
            else
            {
                return Array.Empty<ClrRoutineInfo>();
            }
        }

        /// <summary>
        /// Gets value indicating that given extension was loaded.
        /// </summary>
        public bool ContainsExtension(string extension)
        {
            return extension != null && _routinesByExtension.ContainsKey(extension);
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
        public void AddAssembly(Assembly ass)
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
                var attrs = tinfo.GetCustomAttributes<PhpExtensionAttribute>();
                if (attrs != null)
                {
                    foreach (var attr in attrs)
                    {
                        AddRoutine(attr, routine);
                        VisitExtensionAttribute(attr);
                    }
                }

                //
                AddAssembly(tinfo.Assembly);
            }
        }

        void AddRoutine(PhpExtensionAttribute extension, ClrRoutineInfo routine)
        {
            var extensions = extension.Extensions;
            for (int i = 0; i < extensions.Length; i++)
            {
                EnsureExtension(extensions[i]).Add(routine);
            }
        }

        void EnsureExtensions(string[] extensions)
        {
            if (extensions == null || extensions.Length == 0)
            {
                return;
            }

            lock (_routinesByExtension)
            {
                for (int i = 0; i < extensions.Length; i++)
                {
                    var extension = extensions[i];
                    if (!_routinesByExtension.ContainsKey(extension))
                    {
                        _routinesByExtension[extension] = new HashSet<ClrRoutineInfo>();
                    }
                }
            }
        }

        ICollection<ClrRoutineInfo> EnsureExtension(string extension)
        {
            ICollection<ClrRoutineInfo> routines;

            lock (_routinesByExtension)
            {
                if (!_routinesByExtension.TryGetValue(extension, out routines))
                {
                    _routinesByExtension[extension] = routines = new HashSet<ClrRoutineInfo>();
                }
            }

            return routines;
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
