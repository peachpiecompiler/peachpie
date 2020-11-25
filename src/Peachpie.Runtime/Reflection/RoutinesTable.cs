using Pchp.Core.Utilities;
using System;
using System.Collections.Concurrent;
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
        static readonly ConcurrentDictionary<string, int> s_nameToIndex = new ConcurrentDictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
        static readonly List<ClrRoutineInfo> s_appRoutines = new List<ClrRoutineInfo>(2048);
        static readonly RoutinesCount s_contextRoutinesCounter = new RoutinesCount();

        /// <summary>
        /// Adds referenced symbol into the map.
        /// In case of redeclaration, the handle is added to the list.
        /// </summary>
        /// <exception cref="PhpFatalErrorException">The routine is already defined as a user routine.</exception>
        public static ClrRoutineInfo/*!*/DeclareAppRoutine(string name, MethodInfo method)
        {
            ClrRoutineInfo routine;

            if (s_nameToIndex.TryGetValue(name, out var index))
            {
                Debug.Assert(index != 0);

                if (index > 0)  // already taken by user routine
                {
                    RedeclarationError(name);
                }

                (routine = s_appRoutines[-index - 1]).AddOverload(method);
            }
            else
            {
                index = -s_appRoutines.Count - 1;
                s_appRoutines.Add(routine = new ClrRoutineInfo(index, name, method));
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

        /// <exception cref="PhpFatalErrorException">The routine is already defined as a user routine.</exception>
        static void RedeclarationError(string name)
        {
            PhpException.Throw(PhpError.Error, Resources.ErrResources.function_redeclared, name);
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
                index = s_nameToIndex.GetOrAdd(routine.Name, newname =>
                {
                    return s_contextRoutinesCounter.GetNewIndex();
                });

                if (index < 0)
                {
                    // redeclaring over app context function
                    RedeclarationError(routine.Name);
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
                    RedeclarationError(routine.Name);
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

                if (s_nameToIndex.TryGetValue(name, out var index))
                {
                    Debug.Assert(index != 0);

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
