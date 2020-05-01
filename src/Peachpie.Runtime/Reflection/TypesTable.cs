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
    sealed class TypesTable
    {
        #region AppContext

        /// <summary>
        /// Map of type names to their slot index.
        /// Negative number is an app type,
        /// Positive number is a context function.
        /// Zero is not used.
        /// </summary>
        static readonly Dictionary<string, int> s_nameToIndex = new Dictionary<string, int>(512, StringComparer.OrdinalIgnoreCase);
        static readonly List<PhpTypeInfo> s_appTypes = new List<PhpTypeInfo>(256);
        static readonly TypesCount s_contextTypesCounter = new TypesCount();
        static readonly ReaderWriterLockSlim s_rwLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Adds referenced symbol into the map.
        /// In case of redeclaration, an exception is thrown.
        /// </summary>
        public static void DeclareAppType(PhpTypeInfo info)
        {
            if (info.Index == 0)
            {
                if (s_nameToIndex.TryGetValue(info.Name, out var index))
                {
                    RedeclarationError(info);
                }
                else
                {
                    index = -s_appTypes.Count - 1;
                    s_appTypes.Add(info);
                    s_nameToIndex[info.Name] = index;
                }

                info.Index = index;
            }
        }

        #endregion

        sealed class TypesCount
        {
            int _count;

            /// <summary>
            /// Returns new indexes indexed from <c>1</c>.
            /// </summary>
            /// <returns></returns>
            public int GetNewIndex() => Interlocked.Increment(ref _count);

            /// <summary>
            /// Gets count of returned indexes.
            /// </summary>
            public int Count => _count;
        }

        PhpTypeInfo[] _contextTypes;

        public TypesTable()
        {
            _contextTypes = new PhpTypeInfo[s_contextTypesCounter.Count];
        }

        /// <summary>
        /// Invoked when a type is redeclared.
        /// </summary>
        /// <param name="type">The type being declared, but another with the same name is already declared in context.</param>
        static void RedeclarationError(PhpTypeInfo type)
        {
            // TODO: ErrCode & throw, Log
            throw new InvalidOperationException($"Type {type.Name} redeclared!");
        }

        int EnsureTypeIndex(PhpTypeInfo info)
        {
            if (info.Index == 0)
            {
                s_rwLock.EnterWriteLock();
                try
                {
                    // double checked lock
                    if (info.Index == 0)
                    {
                        if (s_nameToIndex.TryGetValue(info.Name, out var index))
                        {
                            if (index < 0)  // redeclaring over an app context type
                            {
                                RedeclarationError(info);
                            }

                            info.Index = index;
                        }
                        else
                        {
                            index = s_contextTypesCounter.GetNewIndex();
                            s_nameToIndex[info.Name] = index;
                        }

                        info.Index = index;
                    }
                }
                finally
                {
                    s_rwLock.ExitWriteLock();
                }
            }

            Debug.Assert(info.Index != 0);

            return info.Index;
        }

        public void DeclareType(PhpTypeInfo info)
        {
            var index = EnsureTypeIndex(info);
            if (index > 0)
            {
                if (_contextTypes.Length < index)
                {
                    Array.Resize(ref _contextTypes, index * 2);
                }

                DeclareType(ref _contextTypes[index - 1], info);
            }
        }

        public void DeclareTypeAlias(PhpTypeInfo info, string name)
        {
            if (s_nameToIndex.TryGetValue(name, out var index))
            {
                if (index < 0)  // redeclaring over an app context type
                {
                    RedeclarationError(info);
                }
            }
            else
            {
                index = s_contextTypesCounter.GetNewIndex();
                s_nameToIndex[name] = index;
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
                    RedeclarationError(type);
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
            s_rwLock.EnterReadLock();
            try
            {
                s_nameToIndex.TryGetValue(name, out index);
            }
            finally
            {
                s_rwLock.ExitReadLock();
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
                return s_appTypes[-index - 1];
            }

            return null;
        }

        /// <summary>
        /// Gets enumeration of types visible in current context.
        /// </summary>
        public IEnumerable<PhpTypeInfo> GetDeclaredTypes() => s_appTypes.Concat(_contextTypes.WhereNotNull());

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
