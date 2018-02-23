using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Runtime table of application PHP functions.
    /// </summary>
    [DebuggerNonUserCode]
    internal class RoutinesTable
    {
        #region AppContext

        /// <summary>
        /// Map of function names to their slot index.
        /// Negative number is an application-wide function,
        /// Positive number is context-wide function.
        /// Zero is not used.
        /// </summary>
        static readonly Dictionary<string, int> _nameToIndex = new Dictionary<string, int>(2048, StringComparer.CurrentCultureIgnoreCase);
        static readonly List<RoutineInfo> _appRoutines = new List<RoutineInfo>(2048);
        static readonly RoutinesCount _contextRoutinesCounter = new RoutinesTable.RoutinesCount();
        static readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Adds referenced symbol into the map.
        /// In case of redeclaration, the handle is added to the list.
        /// </summary>
        public static void DeclareAppRoutine(string name, RuntimeMethodHandle handle)
        {
            // TODO: W lock

            int index;
            if (_nameToIndex.TryGetValue(name, out index))
            {
                Debug.Assert(index != 0);

                if (index > 0)  // already taken by user routine
                {
                    throw new InvalidOperationException();
                }

                ((ClrRoutineInfo)_appRoutines[-index - 1]).AddOverload(handle);
            }
            else
            {
                index = -_appRoutines.Count - 1;
                var routine = new ClrRoutineInfo(index, name, handle);
                _appRoutines.Add(routine);
                _nameToIndex[name] = index;

                // register the routine within the extensions table
                ExtensionsAppContext.ExtensionsTable.AddRoutine(routine);
            }
        }

        #endregion

        sealed class RoutinesCount
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

        RoutineInfo[] _contextRoutines = new RoutineInfo[_contextRoutinesCounter.Count];

        readonly Action<RoutineInfo> _redeclarationCallback;

        internal RoutinesTable(Action<RoutineInfo> redeclarationCallback)
        {
            _redeclarationCallback = redeclarationCallback;
        }

        /// <summary>
        /// Gets enumeration of all routines declared within the context.
        /// </summary>
        public IEnumerable<RoutineInfo> EnumerateRoutines() => _appRoutines.Concat(_contextRoutines.WhereNotNull());

        /// <summary>
        /// Declare a user PHP function.
        /// </summary>
        /// <param name="routine">Routine to be declared. Its index is initialized lazily.</param>
        public void DeclarePhpRoutine(RoutineInfo routine)
        {
            Debug.Assert(routine != null);

            int index = routine.Index;
            if (index == 0)
            {
                lock (_nameToIndex)
                {
                    if (_nameToIndex.TryGetValue(routine.Name, out index))
                    {
                        if (index < 0)  // redeclaring over an app context function
                        {
                            _redeclarationCallback(routine);
                        }
                    }
                    else
                    {
                        index = _contextRoutinesCounter.GetNewIndex() + 1;
                        _nameToIndex[routine.Name] = index;
                    }
                }

                //
                routine.Index = index;
            }

            Debug.Assert(routine.Index > 0);

            //
            if (_contextRoutines.Length < index)
            {
                Array.Resize(ref _contextRoutines, index * 2);                
            }

            DeclarePhpRoutine(ref _contextRoutines[index - 1], routine);
        }

        void DeclarePhpRoutine(ref RoutineInfo slot, RoutineInfo routine)
        {
            if (object.ReferenceEquals(slot, null))
            {
                slot = routine;
            }
            else
            {
                if (!object.ReferenceEquals(slot, routine))
                {
                    _redeclarationCallback(routine);
                }
            }
        }

        /// <summary>
        /// Gets routine information in current context.
        /// </summary>
        /// <param name="name">Name of the routine.</param>
        /// <returns><see cref="RoutineInfo"/> instance or <c>null</c> if routine with given name is not declared.</returns>
        public RoutineInfo GetDeclaredRoutine(string name)
        {
            int index;
            if (_nameToIndex.TryGetValue(name, out index))
            {
                if (index > 0)
                {
                    var routines = _contextRoutines;
                    if (index <= routines.Length)
                    {
                        return routines[index - 1];
                    }
                }
                else
                {
                    Debug.Assert(index != 0);
                    return _appRoutines[-index - 1];
                }
            }

            return null;
        }

        internal RoutineInfo GetDeclaredRoutine(int ctxslot)
        {
            Debug.Assert(ctxslot >= 0);
            var routines = _contextRoutines;
            return ctxslot <= routines.Length ? routines[ctxslot] : null;
        }

        /// <summary>
        /// Checks whether given routine is declared in current context.
        /// </summary>
        internal bool IsDeclared(RoutineInfo routine)
        {
            return (routine.Index > 0 && routine.Index <= _contextRoutines.Length && _contextRoutines[routine.Index - 1] == routine);
        }
    }
}
