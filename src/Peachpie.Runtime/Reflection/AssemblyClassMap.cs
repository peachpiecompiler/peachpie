using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Object keeping track of PHP user types.
    /// </summary>
    sealed class AssemblyClassMap
    {
        readonly Queue<Assembly> _assemblies = new Queue<Assembly>();
        readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Lazily initialized map of types.
        /// </summary>
        Dictionary<string, PhpTypeInfo[]> _typesMap = new Dictionary<string, PhpTypeInfo[]>(StringComparer.Ordinal);

        public void AddPhpAssembly(Assembly ass)
        {
            _rwLock.EnterWriteLock();
            try
            {
                _assemblies.Enqueue(ass);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public PhpTypeInfo[] LookupTypes(string phpTypeName)
        {
            if (string.IsNullOrEmpty(phpTypeName))
            {
                return null;
            }

            while (_assemblies.Count != 0)
            {
                // update map:
                _rwLock.EnterWriteLock();
                try
                {
                    UpdateMapNoLock(_assemblies.Dequeue());
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }

            // cache lookup:
            _rwLock.EnterReadLock();
            try
            {
                _typesMap.TryGetValue(phpTypeName, out var result);
                return result;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Reads types from given assembly,
        /// reflects PHP user types and stores them into <see cref="_typesMap"/>.
        /// </summary>
        void UpdateMapNoLock(Assembly ass)
        {
            Debug.Assert(ass != null);

            foreach (var t in ass.GetExportedTypes())
            {
                if (!t.IsAbstract || !t.IsSealed) // => !IsStatic
                {
                    var tinfo = PhpTypeInfoExtension.GetPhpTypeInfo(t);
                    var rpath = tinfo.RelativePath;
                    if (rpath != null && !rpath.StartsWith("phar://")) // => PHP user type
                    {
                        if (_typesMap.TryGetValue(tinfo.Name, out var tinfos))
                        {
                            // very rare case
                            Array.Resize(ref tinfos, tinfos.Length + 1);
                            tinfos[tinfos.Length - 1] = tinfo;
                        }
                        else
                        {
                            tinfos = new[] { tinfo };
                        }

                        _typesMap[tinfo.Name] = tinfos;
                    }
                }
            }
        }
    }
}
