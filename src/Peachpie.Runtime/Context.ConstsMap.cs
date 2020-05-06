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
        [DebuggerDisplay("Value={Data}, Extension={ExtensionName}")]
        struct ConstData
        {
            /// <summary>
            /// The constant value or value getter Func&lt;PhpValue&gt;.
            /// </summary>
            public PhpValue? Data;

            /// <summary>
            /// Gets a value indicating the constant has been initialized.
            /// </summary>
            public bool HasValue => Data.HasValue;

            /// <summary>
            /// Resolves the constant value.
            /// </summary>
            public PhpValue GetValue(Context ctx)
            {
                var value = Data.GetValueOrDefault();

                if (value.Object is Delegate)
                {
                    return value.Object switch
                    {
                        Func<PhpValue> func => func(),
                        Func<Context, PhpValue> func2 => func2(ctx),
                        _ => throw null,
                    };
                }
                else
                {
                    return value;
                }
            }

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
            internal enum ConstantState
            {
                Uninitialized = 0,
                AppConstant = 1,
                UserConstant = 2,
            }

            internal ConstantState _flags;

            public bool IsSet => _flags != ConstantState.Uninitialized;

            public string Name { get; set; }

            public string ExtensionName { get; set; }

            public PhpValue Value { get; set; }

            public bool IsUser => _flags == ConstantState.UserConstant;
        }

        #endregion

        struct ConstsMap : IEnumerable<ConstantInfo>
        {
            /// <summary>
            /// Maps of constant name to its ID.
            /// </summary>
            readonly static Dictionary<ConstName, int> s_map = new Dictionary<ConstName, int>(1024, new ConstName.ConstNameComparer());

            /// <summary>
            /// Lock mechanism for accessing statics.
            /// </summary>
            readonly static ReaderWriterLockSlim s_rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            /// <summary>
            /// Maps constant ID to its actual value, accross all contexts (application wide).
            /// </summary>
            static ConstData[] s_valuesApp = new ConstData[1500];   // there is at least ~1357 builtin constants

            /// <summary>
            /// Actual count of defined constant names.
            /// </summary>
            static int s_countApp, s_countCtx;

            /// <summary>
            /// Maps constant ID to its actual value in current context.
            /// </summary>
            PhpValue?[]/*!*/_valuesCtx;

            /// <summary>
            /// Runtime context.
            /// </summary>
            readonly Context _ctx;

            /// <summary>
            /// Initializes <see cref="ConstsMap"/>.
            /// </summary>
            public static ConstsMap Create(Context ctx) => new ConstsMap(ctx);

            ConstsMap(Context ctx)
            {
                _ctx = ctx;
                _valuesCtx = Array.Empty<PhpValue?>();
            }

            static void EnsureArray<T>(ref T[] arr, int size)
            {
                if (arr.Length < size)
                {
                    Array.Resize(ref arr, Math.Max(size * 2 + 1, s_countCtx));
                }
            }

            static void RedeclarationError(string name)
            {
                throw new InvalidOperationException(string.Format(Resources.ErrResources.constant_redeclared, name));
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

                s_rwLock.EnterUpgradeableReadLock();
                try
                {
                    if (!s_map.TryGetValue(cname, out idx))
                    {
                        s_rwLock.EnterWriteLock();
                        try
                        {
                            // new constant ID, non zero
                            idx = appConstant
                                ? -(++s_countApp)    // app constants are negative
                                : (++s_countCtx);    //

                            s_map.Add(cname, idx);
                        }
                        finally
                        {
                            s_rwLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    s_rwLock.ExitUpgradeableReadLock();
                }

                //
                return idx;
            }

            public static void DefineAppConstant(string name, PhpValue value, bool ignoreCase, string extensionName)
            {
                Debug.Assert(value.IsScalar || value.Object is Func<PhpValue> || value.Object is Func<Context, PhpValue>);

                var idx = -RegisterConstantId(name, ignoreCase, true);
                Debug.Assert(idx != 0);

                if (idx < 0)
                {
                    RedeclarationError(name);   // runtime constant with this name was already defined
                }

                EnsureArray(ref s_valuesApp, idx);

                // fill in the app contant slot
                ref var slot = ref s_valuesApp[idx - 1];

                if (SetValue(ref slot.Data, value))
                {
                    slot.ExtensionName = extensionName;
                }
            }

            public static bool DefineConstant(ref ConstsMap self, string name, PhpValue value, ref int idx, bool ignoreCase = false)
            {
                Debug.Assert(value.IsScalar || value.IsArray);

                if (idx == 0)
                {
                    idx = RegisterConstantId(name, ignoreCase, false);
                    Debug.Assert(idx != 0);
                }

                if (idx < 0) // app constant cannot be redeclared
                {
                    RedeclarationError(name);
                }

                ref var values = ref self._valuesCtx;
                EnsureArray(ref values, idx);
                return SetValue(ref values[idx - 1], value);
            }

            /// <summary>
            /// Overwrites given slot with value in case the slot is not set yet.
            /// </summary>
            /// <param name="slot">Constant slot to be set.</param>
            /// <param name="value">Value to be set.</param>
            /// <returns>True if slot was set, otherwise false.</returns>
            static bool SetValue(ref PhpValue? slot, PhpValue value)
            {
                if (slot.HasValue)
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
                    s_rwLock.EnterReadLock();
                    try
                    {
                        s_map.TryGetValue(new ConstName(name), out idx);
                    }
                    finally
                    {
                        s_rwLock.ExitReadLock();
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
                    if (ArrayUtils.TryGetItem(_valuesCtx, idx - 1, out var slot) && slot.HasValue)
                    {
                        value = slot.GetValueOrDefault();
                        return true;
                    }
                }
                else // if (idx < 0)
                {
                    // app constant
                    if (ArrayUtils.TryGetItem(s_valuesApp, -idx - 1, out var data) && data.HasValue)
                    {
                        value = data.GetValue(_ctx);
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
                var listApp = new ConstantInfo[s_countApp];
                var listCtx = new ConstantInfo[s_countCtx];

                //
                s_rwLock.EnterReadLock();
                try
                {
                    // initilize values

                    var valuesApp = s_valuesApp;
                    for (int i = 0; i < valuesApp.Length; i++)
                    {
                        ref var data = ref valuesApp[i];
                        if (data.HasValue)
                        {
                            ref var item = ref listApp[i];
                            item.Value = data.GetValue(_ctx);
                            item.ExtensionName = data.ExtensionName;
                            item._flags = ConstantInfo.ConstantState.AppConstant;
                        }
                    }

                    var valuesCtx = _valuesCtx;
                    for (int i = 0; i < valuesCtx.Length; i++)
                    {
                        var value = valuesCtx[i];
                        if (value.HasValue)
                        {
                            ref var item = ref listCtx[i];
                            item.Value = value.GetValueOrDefault();
                            item._flags = ConstantInfo.ConstantState.UserConstant;
                        }
                    }

                    // assign name from the map
                    foreach (var pair in s_map)
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
                    s_rwLock.ExitReadLock();
                }

                //
                return listApp.Concat(listCtx)
                    .Where(info => info.IsSet)
                    .GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
