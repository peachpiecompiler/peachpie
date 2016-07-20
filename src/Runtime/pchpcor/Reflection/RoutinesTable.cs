using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    internal static class RoutinesAppContext
    {
        public static readonly Dictionary<string, int> NameToIndex = new Dictionary<string, int>();
        public static readonly List<RoutineInfo> AppRoutines = new List<RoutineInfo>();
        public static readonly RoutinesTable.RoutinesCount ContextRoutinesCounter = new RoutinesTable.RoutinesCount();

        /// <summary>
        /// Adds referenced symbol into the map.
        /// In case of redeclaration, the handle is added to the list.
        /// </summary>
        public static void DeclareClrRoutine(string name, RuntimeMethodHandle handle)
        {
            // TODO: W lock

            int index;
            if (NameToIndex.TryGetValue(name, out index))
            {
                Debug.Assert(index != 0);

                if (index > 0)
                {
                    throw new InvalidOperationException();
                }

                ((ClrRoutineInfo)AppRoutines[-index - 1]).AddOverload(handle);
            }
            else
            {
                index = -AppRoutines.Count - 1;
                AppRoutines.Add(new ClrRoutineInfo(index, name, handle));
                NameToIndex[name] = index;
            }
        }
    }

    /// <summary>
    /// Runtime table of application PHP functions.
    /// </summary>
    internal class RoutinesTable
    {
        public class RoutinesCount
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

        readonly List<RoutineInfo> _appRoutines;

        readonly RoutinesCount _contextRoutinesCounter;

        RoutineInfo[] _contextRoutines;

        readonly Action<RoutineInfo> _redeclarationCallback;

        internal RoutinesTable(Dictionary<string, int> nameToIndex, List<RoutineInfo> appRoutines, RoutinesCount counter, Action<RoutineInfo> redeclarationCallback)
        {
            _nameToIndex = nameToIndex;
            _appRoutines = appRoutines;
            _contextRoutinesCounter = counter;
            _contextRoutines = new RoutineInfo[counter.Count];
            _redeclarationCallback = redeclarationCallback;
        }

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

        /// <summary>
        /// Check PHP routine at <paramref name="index"/> is declared.
        /// </summary>
        internal bool IsDeclared(int index, RuntimeMethodHandle expected)
        {
            var routines = _contextRoutines;
            return index <= routines.Length && ((PhpRoutineInfo)routines[index - 1])?.Handle == expected;
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
