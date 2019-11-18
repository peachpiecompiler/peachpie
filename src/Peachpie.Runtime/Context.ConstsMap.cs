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
        #region ConstName, ConstData

        /// <summary>
        /// Constant name.
        /// </summary>
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
            public string Name { get; }

            /// <summary>
            /// Comparer used to compare the name.
            /// </summary>
            public StringComparer StringComparer { get; }

            public ConstName(string name, bool ignoreCase = false)
            {
                this.Name = name ?? throw new ArgumentNullException(nameof(name));
                this.StringComparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            }

            public bool Equals(ConstName other) => StringComparer.Equals(Name, other.Name);

            public override bool Equals(object obj) => obj is ConstName && Equals((ConstName)obj);

            public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name); // always ignore case when getting hash
        }

        /// <summary>
        /// Information about app constants.
        /// </summary>
        [DebuggerDisplay("Value={Value}, Extension={ExtensionName}")]
        struct ConstData
        {
            /// <summary>
            /// The constant value or value getter Func&lt;PhpValue&gt;.
            /// </summary>
            public PhpValue Data;

            /// <summary>
            /// Resolves the constant value.
            /// </summary>
            public PhpValue Value => Data.Object is Func<PhpValue> func ? func() : Data;

            /// <summary>
            /// Optional extension name that the constant belongs to.
            /// </summary>
            public string ExtensionName;
        }

        #endregion

        #region ConstantInfo // CONSIDER: move to Reflection

        /// <summary>
        /// Information about a global constant.
        /// </summary>
        [DebuggerDisplay("{Name,nq}, Value={Value}")]
        public struct ConstantInfo
        {
            public string Name { get; set; }
            public string ExtensionName { get; set; }
            public PhpValue Value { get; set; }
            public bool IsUser { get; set; }
        }

        #endregion

        class ConstsMap : IEnumerable<ConstantInfo>
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
            static ConstData[] _valuesApp = new ConstData[1200];   // there is ~1162 builtin constants

            /// <summary>
            /// Actual count of defined constant names.
            /// </summary>
            static int _countApp, _countCtx;

            /// <summary>
            /// Maps constant ID to its actual value in current context.
            /// </summary>
            PhpValue[] _valuesCtx = new PhpValue[_countCtx];

            static void EnsureArray<T>(ref T[] arr, int size)
            {
                if (arr.Length < size)
                {
                    Array.Resize(ref arr, size * 2 + 1);
                }
            }

            static Exception ConstantRedeclaredException(string name)
            {
                return new InvalidOperationException(string.Format(Resources.ErrResources.constant_redeclared, name));
            }

            /// <summary>
            /// Ensures unique constant ID for given constant name.
            /// Gets positive ID for runtime constant, negative ID for application constant.
            /// IDs are indexed from <c>1</c>. Zero is invalid ID.
            /// </summary>
            static int RegisterConstantId(string name, bool ignoreCase = false, bool appConstant = false)
            {
                var cname = new ConstName(name, ignoreCase);
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

            public static void DefineAppConstant(string name, PhpValue value, bool ignoreCase, string extensionName)
            {
                Debug.Assert(value.IsScalar || value.Object is Func<PhpValue>);

                var idx = -RegisterConstantId(name, ignoreCase, true);
                Debug.Assert(idx != 0);

                if (idx < 0)
                {
                    throw ConstantRedeclaredException(name);   // runtime constant with this name was already defined
                }

                EnsureArray(ref _valuesApp, idx);

                // fill in the app contant slot
                ref var slot = ref _valuesApp[idx - 1];

                if (SetValue(ref slot.Data, value))
                {
                    slot.ExtensionName = extensionName;
                }
                else
                {
                    Debug.Fail(string.Format(Resources.ErrResources.constant_redeclared, name));
                }
            }

            public bool DefineConstant(string name, PhpValue value, bool ignoreCase = false)
            {
                int idx = 0;
                return DefineConstant(name, value, ref idx, ignoreCase);
            }

            public bool DefineConstant(string name, PhpValue value, ref int idx, bool ignoreCase = false)
            {
                Debug.Assert(value.IsScalar || value.IsArray);

                if (idx == 0)
                {
                    idx = RegisterConstantId(name, ignoreCase, false);
                    Debug.Assert(idx != 0);
                }

                if (idx < 0) // app constant cannot be redeclared
                {
                    throw ConstantRedeclaredException(name);
                }

                EnsureArray(ref _valuesCtx, idx);
                return SetValue(ref _valuesCtx[idx - 1], value);
            }

            /// <summary>
            /// Overwrites given slot with value in case the slot is not set yet.
            /// </summary>
            /// <param name="slot">Constant slot to be set.</param>
            /// <param name="value">Value to be set.</param>
            /// <returns>True if slot was set, otherwise false.</returns>
            static bool SetValue(ref PhpValue slot, PhpValue value)
            {
                if (slot.IsSet)
                {
                    return false;
                }
                else
                {
                    slot = value;
                    return true;
                }
            }

            /// <summary>
            /// Gets constant value by its name.
            /// </summary>
            public bool TryGetConstant(string name, out PhpValue value)
            {
                int idx = 0;
                return TryGetConstant(name, ref idx, out value);
            }

            /// <summary>
            /// Gets constant value by its name. Uses cache variable to remember constants index.
            /// </summary>
            /// <param name="name">Variable name used if <paramref name="idx"/> is not provided.</param>
            /// <param name="idx">Variable containing cached constant index.</param>
            /// <param name="value">The resulting value.</param>
            /// <returns>Whether the constant is defined.</returns>
            public bool TryGetConstant(string name, ref int idx, out PhpValue value)
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

                //
                return TryGetConstant(idx, out value);
            }

            /// <summary>
            /// Gets constant value by constant index.
            /// Negative numbers denotates app constants, positive numbers conrrespond to constants defined within <see cref="Context"/> (user constants).
            /// </summary>
            bool TryGetConstant(int idx, out PhpValue value)
            {
                if (idx > 0)
                {
                    // user constant
                    if (ArrayUtils.TryGetItem(_valuesCtx, idx - 1, out value) && value.IsSet)
                    {
                        return true;
                    }
                }
                else // if (idx < 0)
                {
                    // app constant
                    if (ArrayUtils.TryGetItem(_valuesApp, -idx - 1, out var data) && data.Data.IsSet)
                    {
                        value = data.Value;
                        return true;
                    }
                }

                // undefined
                value = default;
                return false;
            }

            /// <summary>
            /// Gets value indicating whether given constant is defined.
            /// </summary>
            public bool IsDefined(string name) => TryGetConstant(name, out _);

            /// <summary>
            /// Enumerates all defined constants available in the context (including app constants).
            /// </summary>
            public IEnumerator<ConstantInfo> GetEnumerator()
            {
                var listApp = new ConstantInfo[_countApp];
                var listCtx = new ConstantInfo[_countCtx];

                //
                _rwLock.EnterReadLock();
                try
                {
                    // initilize values

                    var valuesApp = _valuesApp;
                    for (int i = 0; i < valuesApp.Length; i++)
                    {
                        ref var data = ref valuesApp[i];
                        if (!data.Data.IsDefault)
                        {
                            ref var item = ref listApp[i];
                            item.Value = data.Value;
                            item.ExtensionName = data.ExtensionName;
                            //item.IsUser = false;
                        }
                    }

                    var valuesCtx = _valuesCtx;
                    for (int i = 0; i < valuesCtx.Length; i++)
                    {
                        var value = valuesCtx[i];
                        if (!value.IsDefault)
                        {
                            ref var item = ref listCtx[i];
                            item.Value = value;
                            item.IsUser = true;
                        }
                    }

                    // assign name from the map
                    foreach (var pair in _map)
                    {
                        if (pair.Value < 0)
                        {
                            // app constant
                            listApp[-pair.Value - 1].Name = pair.Key.Name;
                        }
                        else
                        {
                            // user constant
                            listCtx[pair.Value - 1].Name = pair.Key.Name;
                        }
                    }
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }

                //
                return listApp.Concat(listCtx)
                    .Where(info => !info.Value.IsDefault)
                    .GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
