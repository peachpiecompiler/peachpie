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

        class ConstsMap : IEnumerable<KeyValuePair<string, PhpValue>>
        {
            /// <summary>
            /// Maps of constant name to its ID.
            /// </summary>
            readonly static Dictionary<ConstName, int> _map = new Dictionary<ConstName, int>(new ConstName.ConstNameComparer());

            /// <summary>
            /// Maps constant ID to its actual value, accross all contexts (application wide).
            /// </summary>
            static PhpValue[] _valuesApp = new PhpValue[32];

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

                if (!_map.TryGetValue(cname, out idx))
                {
                    // TODO: W lock

                    // new constant ID, non zero
                    idx = appConstant
                        ? -(++_countApp)    // app constants are negative
                        : (++_countCtx);    //

                    _map.Add(cname, idx);
                }

                //
                return idx;
            }

            public static void DefineAppConstant(string name, PhpValue value, bool ignorecase = false)
            {
                // TODO: Assert value.IsScalar

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
                // TODO: Assert value.IsScalar

                var idx = RegisterConstantId(name, ignorecase, false);
                Debug.Assert(idx != 0);

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
                    // TODO: R lock
                    _map.TryGetValue(new ConstName(name), out idx);
                }

                return GetConstant(idx);
            }

            /// <summary>
            /// Gets constant value by its name.
            /// </summary>
            public PhpValue GetConstant(string name)
            {
                int idx;
                _map.TryGetValue(new ConstName(name), out idx);
                return GetConstant(idx);
            }

            /// <summary>
            /// Gets constant value by constant index.
            /// </summary>
            PhpValue GetConstant(int idx)
                => idx > 0 ? GetConstant(idx - 1, _valuesCtx) : GetConstant(-idx - 1, _valuesApp);

            static PhpValue GetConstant(int idx, PhpValue[] values)
            {
                return (idx >= 0 && idx < values.Length)
                    ? values[idx]
                    : PhpValue.Void;
            }

            /// <summary>
            /// Gets value indicating whether given constant is defined.
            /// </summary>
            public bool IsDefined(string name) => GetConstant(name).IsSet;

            /// <summary>
            /// Enumerates all defined constants available in the context (including app-wide constants).
            /// </summary>
            public IEnumerator<KeyValuePair<string, PhpValue>> GetEnumerator()
            {
                // TODO: R lock
                foreach (var pair in _map)
                {
                    var value = GetConstant(pair.Value);
                    if (value.IsSet)
                    {
                        yield return new KeyValuePair<string, PhpValue>(pair.Key.Name, value);
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
