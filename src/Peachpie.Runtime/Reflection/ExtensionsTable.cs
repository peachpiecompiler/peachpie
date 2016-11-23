using System;
using System.Collections.Generic;
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
        /// <returns>Enumeration of routines associated with given extension. Gets <c>null</c> if <paramref name="extension"/> was not loaded.</returns>
        public ICollection<ClrRoutineInfo> GetRoutinesByExtensionOrNull(string extension)
        {
            ICollection<ClrRoutineInfo> routines;
            if (extension != null && _routinesByExtension.TryGetValue(extension, out routines))
            {
                return routines;
            }
            else
            {
                return null;
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
                var attr = ass.GetCustomAttribute<PhpExtensionAttribute>();
                if (attr != null)
                {
                    EnsureExtensions(attr.Extensions);
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
                var attr = tinfo.GetCustomAttribute<PhpExtensionAttribute>();
                if (attr != null)
                {
                    AddRoutine(attr, routine);
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

        
    }
}
