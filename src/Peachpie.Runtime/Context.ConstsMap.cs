using Pchp.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        #region ConstName

        [DebuggerDisplay("{Name,nq}")]
        struct ConstName : IEquatable<ConstName>
        {
            public class ConstNameComparer : IEqualityComparer<ConstName>
            {
                public bool Equals(ConstName x, ConstName y) => x.Equals(y);

                public int GetHashCode(ConstName obj) => obj.GetHashCode();
            }

            /// <summary>
            /// Constant name.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Whether the casing is ignored.
            /// <c>false</c> by default.
            /// </summary>
            public readonly bool CaseInsensitive;

            public ConstName(string name, bool caseInsensitive = false)
            {
                this.Name = name;
                this.CaseInsensitive = caseInsensitive;
            }

            public bool Equals(ConstName other)
            {
                return Name.Equals(other.Name,
                    (this.CaseInsensitive | other.CaseInsensitive)
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal);
            }

            public override bool Equals(object obj) => obj is ConstName && Equals((ConstName)obj);

            public override int GetHashCode() => Name.GetHashCode();
        }

        #endregion

        #region IConstantsComposition

        /// <summary>
        /// Interface for defining new constants.
        /// Used by compiler to provide definitions of app-wide constants.
        /// </summary>
        public interface IConstantsComposition
        {
            /// <summary>Defines constant, either case sensitive or insensitive.</summary>
            void Define(string name, PhpValue value, bool ignoreCase);

            /// <summary>Defines case sensitive constant.</summary>
            void Define(string name, PhpValue value);

            /// <summary>Defines case sensitive constant.</summary>
            void Define(string name, long value);

            /// <summary>Defines case sensitive constant.</summary>
            void Define(string name, double value);

            /// <summary>Defines case sensitive constant.</summary>
            void Define(string name, string value);

            /// <summary>
            /// Defines case sensitive constant using a getter method instead of a value.
            /// The getter is called every time the constant is used.
            /// </summary>
            void Define(string name, Func<PhpValue> getter);
        }

        /// <summary>
        /// Helper class that defines constants in app-context.
        /// </summary>
        sealed class AppConstantsComposition : IConstantsComposition
        {
            public void Define(string name, PhpValue value, bool ignoreCase) => ConstsMap.DefineAppConstant(name, value, ignoreCase);
            public void Define(string name, PhpValue value) => Define(name, value, ignoreCase: false);
            public void Define(string name, long value) => Define(name, (PhpValue)value);
            public void Define(string name, double value) => Define(name, (PhpValue)value);
            public void Define(string name, string value) => Define(name, (PhpValue)value);
            public void Define(string name, Func<PhpValue> getter) => Define(name, PhpValue.FromClass(getter));
        }

        #endregion

        class ConstsMap : IEnumerable<KeyValuePair<string, PhpValue>>
        {
            /// <summary>
            /// Maps of constant name to its ID.
            /// </summary>
            readonly static Dictionary<ConstName, int> _map = new Dictionary<ConstName, int>(1024, new ConstName.ConstNameComparer());

            /// <summary>
            /// Lock mechanism for accessing statics.
            /// </summary>
            readonly static ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            /// <summary>
            /// Maps constant ID to its actual value, accross all contexts (application wide).
            /// </summary>
            static PhpValue[] _valuesApp = new PhpValue[512];

            /// <summary>
            /// Actual count of defined constant names.
            /// </summary>
            static int _countApp, _countCtx;

            /// <summary>
            /// Maps constant ID to its actual value in current context.
            /// </summary>
            PhpValue[] _valuesCtx = new PhpValue[_countCtx];

            static void EnsureArray(ref PhpValue[] arr, int size)
            {
                if (arr.Length < size)
                {
                    Array.Resize(ref arr, size * 2 + 1);
                }
            }

            /// <summary>
            /// Ensures unique constant ID for given constant name.
            /// Gets positive ID for runtime constant, negative ID for application constant.
            /// IDs are indexed from <c>1</c>. Zero is invalid ID.
            /// </summary>
            static int RegisterConstantId(string name, bool ignorecase = false, bool appConstant = false)
            {
                var cname = new ConstName(name, ignorecase);
                int idx;

                _rwLock.EnterUpgradeableReadLock();
                try
                {
                    if (!_map.TryGetValue(cname, out idx))
                    {
                        _rwLock.EnterWriteLock();
                        try
                        {
                            // new constant ID, non zero
                            idx = appConstant
                                ? -(++_countApp)    // app constants are negative
                                : (++_countCtx);    //

                            _map.Add(cname, idx);
                        }
                        finally
                        {
                            _rwLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    _rwLock.ExitUpgradeableReadLock();
                }

                //
                return idx;
            }

            public static void DefineAppConstant(string name, PhpValue value, bool ignorecase = false)
            {
                Debug.Assert(value.IsScalar || value.Object is Func<PhpValue>);

                var idx = -RegisterConstantId(name, ignorecase, true);
                Debug.Assert(idx != 0);

                if (idx < 0)
                    throw new ArgumentException("runtime_constant_redefinition");   // runtime constant with this name was already defined

                // TODO: check redefinition
                EnsureArray(ref _valuesApp, idx);
                DefineConstant(ref _valuesApp[idx - 1], value);
            }

            public bool DefineConstant(string name, PhpValue value, bool ignorecase = false)
            {
                int idx = 0;
                return DefineConstant(name, value, ref idx, ignorecase);
            }

            public bool DefineConstant(string name, PhpValue value, ref int idx, bool ignorecase = false)
            {
                Debug.Assert(value.IsScalar);

                if (idx == 0)
                {
                    idx = RegisterConstantId(name, ignorecase, false);
                    Debug.Assert(idx != 0);
                }

                if (idx < 0)
                    throw new ArgumentException("app_constant_redefinition");   // app-wide constant with this name was already defined

                EnsureArray(ref _valuesCtx, idx);
                return DefineConstant(ref _valuesCtx[idx - 1], value);
            }

            /// <summary>
            /// Overwrites given slot with value in case the slot is not set yet.
            /// </summary>
            /// <param name="slot">Constant slot to be set.</param>
            /// <param name="value">Value to be set.</param>
            /// <returns>True if slot was set, otherwise false.</returns>
            static bool DefineConstant(ref PhpValue slot, PhpValue value)
            {
                if (slot.IsSet)
                    return false;

                slot = value;
                return true;
            }

            /// <summary>
            /// Gets constant value by its name. Uses cache variable to remember constants index.
            /// </summary>
            /// <param name="name">Variable name used if <paramref name="idx"/> is not provided.</param>
            /// <param name="idx">Variable containing cached constant index.</param>
            /// <returns>Constant value.</returns>
            public PhpValue GetConstant(string name, ref int idx)
            {
                if (idx == 0)
                {
                    _rwLock.EnterReadLock();
                    try
                    {
                        _map.TryGetValue(new ConstName(name), out idx);
                    }
                    finally
                    {
                        _rwLock.ExitReadLock();
                    }
                }

                return GetConstant(idx);
            }

            /// <summary>
            /// Gets constant value by its name.
            /// </summary>
            public PhpValue GetConstant(string name)
            {
                int idx;

                _rwLock.EnterReadLock();
                try
                {
                    _map.TryGetValue(new ConstName(name), out idx);
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }

                return GetConstant(idx);
            }

            /// <summary>
            /// Gets constant value by constant index.
            /// Negative numbers denotates app constants, positive numbers conrrespond to constants defined in runtime.
            /// </summary>
            PhpValue GetConstant(int idx) => idx > 0 ? GetConstant(idx - 1, _valuesCtx) : GetAppConstant(GetConstant(-idx - 1, _valuesApp));

            /// <summary>
            /// Safely returns constant from its index within array of constants.
            /// </summary>
            static PhpValue GetConstant(int idx, PhpValue[] values) => (idx >= 0 && idx < values.Length) ? values[idx] : PhpValue.Void;

            /// <summary>
            /// App constant can be either a value or a function getting a value.
            /// </summary>
            static PhpValue GetAppConstant(PhpValue slot) => slot.Object is Func<PhpValue> func ? func() : slot;

            /// <summary>
            /// Gets value indicating whether given constant is defined.
            /// </summary>
            public bool IsDefined(string name) => GetConstant(name).IsSet;

            /// <summary>
            /// Enumerates all defined constants available in the context (including app-wide constants).
            /// </summary>
            public IEnumerator<KeyValuePair<string, PhpValue>> GetEnumerator()
            {
                var list = new List<KeyValuePair<string, PhpValue>>(_map.Count);

                //
                _rwLock.EnterReadLock();
                try
                {
                    foreach (var pair in _map)
                    {
                        var value = GetConstant(pair.Value);
                        if (value.IsSet)
                        {
                            list.Add(new KeyValuePair<string, PhpValue>(pair.Key.Name, value));
                        }
                    }
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }

                //
                return list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
