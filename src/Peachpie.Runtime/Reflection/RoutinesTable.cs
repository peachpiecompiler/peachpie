using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Runtime table of application PHP functions.
    /// </summary>
    [DebuggerNonUserCode]
    sealed class RoutinesTable
    {
        #region AppContext

        /// <summary>
        /// Map of function names to their slot index.
        /// Negative number is an application-wide function,
        /// Positive number is context-wide function.
        /// Zero is not used.
        /// </summary>
        static readonly Dictionary<string, int> s_nameToIndex = new Dictionary<string, int>(2048, StringComparer.CurrentCultureIgnoreCase);
        static readonly List<RoutineInfo> s_appRoutines = new List<RoutineInfo>(2048);
        static readonly RoutinesCount s_contextRoutinesCounter = new RoutinesCount();

        /// <summary>
        /// Adds referenced symbol into the map.
        /// In case of redeclaration, the handle is added to the list.
        /// </summary>
        /// <exception cref="InvalidOperationException">The routine is already defined as a user routine.</exception>
        public static ClrRoutineInfo/*!*/DeclareAppRoutine(string name, MethodInfo method)
        {
            ClrRoutineInfo routine;

            // TODO: W lock

            if (s_nameToIndex.TryGetValue(name, out var index))
            {
                Debug.Assert(index != 0);

                if (index > 0)  // already taken by user routine
                {
                    throw new InvalidOperationException();
                }

                (routine = (ClrRoutineInfo)s_appRoutines[-index - 1]).AddOverload(method);
            }
            else
            {
                index = -s_appRoutines.Count - 1;
                routine = new ClrRoutineInfo(index, name, method);
                s_appRoutines.Add(routine);
                s_nameToIndex[name] = index;
            }

            //
            return routine;
        }

        #endregion

        sealed class RoutinesCount
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

        RoutineInfo[] _contextRoutines;

        public RoutinesTable()
        {
            _contextRoutines = new RoutineInfo[s_contextRoutinesCounter.Count];
        }

        static void RedeclarationError(RoutineInfo routine)
        {
            // TODO: ErrCode & throw
            throw new InvalidOperationException($"Function {routine.Name} redeclared!");
        }

        /// <summary>
        /// Gets enumeration of all routines declared within the context.
        /// </summary>
        public IEnumerable<RoutineInfo> EnumerateRoutines() => s_appRoutines.Concat(_contextRoutines.WhereNotNull());

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
                lock (s_nameToIndex)
                {
                    if (s_nameToIndex.TryGetValue(routine.Name, out index))
                    {
                        if (index < 0)  // redeclaring over an app context function
                        {
                            RedeclarationError(routine);
                        }
                    }
                    else
                    {
                        index = s_contextRoutinesCounter.GetNewIndex();
                        s_nameToIndex[routine.Name] = index;
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
            if (ReferenceEquals(slot, null))
            {
                slot = routine;
            }
            else
            {
                if (!ReferenceEquals(slot, routine))
                {
                    RedeclarationError(routine);
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
            if (!string.IsNullOrEmpty(name))
            {
                if (name[0] == '\\')
                {
                    name = name.Substring(1);
                }

                int index;
                if (s_nameToIndex.TryGetValue(name, out index))
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
                        return s_appRoutines[-index - 1];
                    }
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
