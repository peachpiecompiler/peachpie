using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    internal static class TypesAppContext
    {
        public static readonly Dictionary<string, int> NameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public static readonly List<TypeInfo> AppTypes = new List<TypeInfo>();
        public static readonly TypesTable.TypesCount ContextTypesCounter = new TypesTable.TypesCount();

        /// <summary>
        /// Adds referenced symbol into the map.
        /// In case of redeclaration, an exception is thrown.
        /// </summary>
        public static void DeclareType<T>()
        {
            // TODO: W lock

            var info = TypeInfoHolder<T>.TypeInfo;
            if (info.Index == 0)
            {
                var name = info.Name;
                int index;
                if (NameToIndex.TryGetValue(name, out index))
                {
                    throw new ArgumentException();  // redeclaration
                }
                else
                {
                    index = -AppTypes.Count - 1;
                    AppTypes.Add(info);
                    NameToIndex[name] = index;
                }

                info.Index = index;
            }
        }
    }

    internal class TypesTable
    {
        public class TypesCount
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

        /// <summary>
        /// Map of function names to their slot index.
        /// Negative number is an application-wide function,
        /// Positive number is context-wide function.
        /// Zero is not used.
        /// </summary>
        readonly Dictionary<string, int> _nameToIndex;

        readonly List<TypeInfo> _appTypes;

        readonly TypesCount _contextTypesCounter;

        TypeInfo[] _contextTypes;

        readonly Action<TypeInfo> _redeclarationCallback;

        internal TypesTable(Dictionary<string, int> nameToIndex, List<TypeInfo> appTypes, TypesCount counter, Action<TypeInfo> redeclarationCallback)
        {
            _nameToIndex = nameToIndex;
            _appTypes = appTypes;
            _contextTypesCounter = counter;
            _contextTypes = new TypeInfo[counter.Count];
            _redeclarationCallback = redeclarationCallback;
        }

        public void DeclareType<T>()
        {
            // TODO: W lock

            var info = TypeInfoHolder<T>.TypeInfo;
            var index = info.Index;
            if (index == 0)
            {
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

            Debug.Assert(info.Index > 0);

            //
            if (_contextTypes.Length < index)
            {
                Array.Resize(ref _contextTypes, index * 2);
            }

            DeclareType(ref _contextTypes[index - 1], info);
        }

        void DeclareType(ref TypeInfo slot, TypeInfo type)
        {
            if (object.ReferenceEquals(slot, null))
            {
                slot = type;
            }
            else
            {
                if (!object.ReferenceEquals(slot, type))
                {
                    _redeclarationCallback(type);
                }
            }
        }

        /// <summary>
        /// Gets runtime type information in current context.
        /// </summary>
        /// <param name="name">Name of the type. Case insensitive.</param>
        /// <returns><see cref="TypeInfo"/> instance or <c>null</c> if type with given name is not declared.</returns>
        public TypeInfo GetDeclaredType(string name)
        {
            int index;
            if (_nameToIndex.TryGetValue(name, out index))
            {
                if (index > 0)
                {
                    var types = _contextTypes;
                    if (index <= types.Length)
                    {
                        return types[index - 1];
                    }
                }
                else
                {
                    Debug.Assert(index != 0);
                    return _appTypes[-index - 1];
                }
            }

            return null;
        }

        internal bool IsDeclared(TypeInfo type)
        {
            return (type.Index > 0 && type.Index <= _contextTypes.Length && _contextTypes[type.Index - 1] == type);
        }
    }
}
