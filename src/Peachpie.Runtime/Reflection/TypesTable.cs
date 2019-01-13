using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Pchp.Core.Reflection
{
    [DebuggerNonUserCode]
    internal class TypesTable
    {
        #region AppContext

        /// <summary>
        /// Map of function names to their slot index.
        /// Negative number is an application-wide function,
        /// Positive number is context-wide function.
        /// Zero is not used.
        /// </summary>
        static readonly Dictionary<string, int> _nameToIndex = new Dictionary<string, int>(512, StringComparer.OrdinalIgnoreCase);
        static readonly List<PhpTypeInfo> _appTypes = new List<PhpTypeInfo>(256);
        static readonly TypesTable.TypesCount _contextTypesCounter = new TypesTable.TypesCount();
        static readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Adds referenced symbol into the map.
        /// In case of redeclaration, an exception is thrown.
        /// </summary>
        public static void DeclareAppType<T>()
        {
            DeclareAppType(TypeInfoHolder<T>.TypeInfo);
        }

        public static void DeclareAppType(PhpTypeInfo info)
        {
            if (info.Index == 0)
            {
                var name = info.Name;
                int index;
                if (_nameToIndex.TryGetValue(name, out index))
                {
                    throw new ArgumentException($"Type '{name}' already declared!");  // redeclaration
                }
                else
                {
                    index = -_appTypes.Count - 1;
                    _appTypes.Add(info);
                    _nameToIndex[name] = index;
                }

                info.Index = index;
            }
        }

        #endregion

        sealed class TypesCount
        {
            int _count;

            /// <summary>
            /// Returns new indexes indexed from <c>0</c>.
            /// </summary>
            /// <returns></returns>
            public int GetNewIndex()
            {
                lock (this)
                {
                    return _count++;
                }
            }

            /// <summary>
            /// Gets count of returned indexes.
            /// </summary>
            public int Count => _count;
        }

        PhpTypeInfo[] _contextTypes;

        readonly Action<PhpTypeInfo> _redeclarationCallback;

        internal TypesTable(Action<PhpTypeInfo> redeclarationCallback)
        {
            _contextTypes = new PhpTypeInfo[_contextTypesCounter.Count];
            _redeclarationCallback = redeclarationCallback;
        }

        int EnsureTypeIndex(PhpTypeInfo info)
        {
            if (info.Index == 0)
            {
                _rwLock.EnterWriteLock();
                try
                {
                    // double checked lock
                    if (info.Index == 0)
                    {
                        int index;
                        var name = info.Name;
                        if (_nameToIndex.TryGetValue(name, out index))
                        {
                            if (index < 0)  // redeclaring over an app context type
                            {
                                _redeclarationCallback(info);
                            }

                            info.Index = index;
                        }
                        else
                        {
                            index = _contextTypesCounter.GetNewIndex() + 1;
                            _nameToIndex[name] = index;
                        }

                        info.Index = index;
                    }
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }

            Debug.Assert(info.Index != 0);

            return info.Index;
        }

        public void DeclareType<T>()
        {
            DeclareType(TypeInfoHolder<T>.TypeInfo);
        }

        public void DeclareType(PhpTypeInfo info)
        {
            var index = EnsureTypeIndex(info);
            Debug.Assert(index > 0);

            //
            if (_contextTypes.Length < index)
            {
                Array.Resize(ref _contextTypes, index * 2);
            }

            DeclareType(ref _contextTypes[index - 1], info);
        }

        public void DeclareTypeAlias(PhpTypeInfo info, string name)
        {
            int index;
            if (_nameToIndex.TryGetValue(name, out index))
            {
                if (index < 0)  // redeclaring over an app context type
                {
                    _redeclarationCallback(info);
                }
            }
            else
            {
                index = _contextTypesCounter.GetNewIndex() + 1;
                _nameToIndex[name] = index;
            }

            //
            if (_contextTypes.Length < index)
            {
                Array.Resize(ref _contextTypes, index * 2);
            }

            DeclareType(ref _contextTypes[index - 1], info);
        }

        void DeclareType(ref PhpTypeInfo slot, PhpTypeInfo type)
        {
            if (ReferenceEquals(slot, null))
            {
                slot = type;
            }
            else
            {
                if (!ReferenceEquals(slot, type))
                {
                    _redeclarationCallback(type);
                }
            }
        }

        /// <summary>
        /// Gets runtime type information in current context.
        /// </summary>
        /// <param name="name">Name of the type. Case insensitive.</param>
        /// <returns><see cref="PhpTypeInfo"/> instance or <c>null</c> if type with given name is not declared.</returns>
        public PhpTypeInfo GetDeclaredType(string name)
        {
            Debug.Assert(name != null);

            if (name.Length != 0 && name[0] == '\\')
            {
                name = name.Substring(1);
            }

            //

            int index;
            _rwLock.EnterReadLock();
            try
            {
                _nameToIndex.TryGetValue(name, out index);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            if (index > 0)
            {
                var types = _contextTypes;
                if (index <= types.Length)
                {
                    return types[index - 1];
                }
            }
            else if (index < 0)
            {
                return _appTypes[-index - 1];
            }

            return null;
        }

        /// <summary>
        /// Gets enumeration of types visible in current context.
        /// </summary>
        public IEnumerable<PhpTypeInfo> GetDeclaredTypes() => _appTypes.Concat(_contextTypes.WhereNotNull());

        /// <summary>
        /// Checkd the given user type is declared in the current state.
        /// </summary>
        internal bool IsDeclared(PhpTypeInfo/*!*/type)
        {
            Debug.Assert(type != null);
            Debug.Assert(type.IsUserType/*user type*/ || type.Index == 0 /*not declared yet*/, "Only handles user types.");

            var slot = type.Index - 1;
            return slot >= 0 && slot < _contextTypes.Length && ReferenceEquals(_contextTypes[slot], type);
        }
    }
}
