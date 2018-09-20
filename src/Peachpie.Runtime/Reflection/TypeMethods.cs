using Pchp.Core.Dynamic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Collection of PHP type methods and magic methods.
    /// </summary>
    public class TypeMethods : IEnumerable<RoutineInfo>
    {
        /// <summary>
        /// Enumeration of magic methods.
        /// Enum name corresponds to the method name.
        /// Names all lowercased.
        /// </summary>
        public enum MagicMethods
        {
            undefined = 0,

            __get, __set,
            __call, __callstatic,
            __isset, __unset,
            __invoke, __tostring,
            __clone, __set_state, __debuginfo,
            __sleep, __wakeup,
        }

        #region Fields

        /// <summary>
        /// Declared magic methods. Optional.
        /// </summary>
        readonly Dictionary<MagicMethods, PhpMethodInfo> _magicMethods;

        /// <summary>
        /// Declared methods. Optional.
        /// </summary>
        readonly Dictionary<string, PhpMethodInfo> _methods;

        #endregion

        #region Initialization

        internal TypeMethods(PhpTypeInfo type)
        {
            IEnumerable<MethodInfo> methods = type.Type
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            // skip members of {System.Object} if we are in a PHP type
            if (type.Type.AsType() != typeof(object))
            {
                methods = methods.Where(s_notObjectMember);
            }

            // skip [PhpHidden] methods
            methods = methods.Where(s_notPhpHidden);

            // collect available methods (including methods on base classes)
            foreach (var m in methods.ToLookup(_MethodName, StringComparer.OrdinalIgnoreCase))
            {
                if (!ReflectionUtils.IsAllowedPhpName(m.Key))   // .ctor, .phpnew, implicit interface implementation
                {
                    continue;
                }

                var overrides = m.ToList();

                // ignore methods in base classes that has been "overriden" in current class
                // in PHP we do override even if signature does not match (e.g. __construct)
                SelectVisibleOverrides(overrides);

                //
                if (_methods == null)
                {
                    _methods = new Dictionary<string, PhpMethodInfo>(StringComparer.OrdinalIgnoreCase);
                }

                var info = PhpMethodInfo.Create(_methods.Count + 1, m.Key, overrides.ToArray(), type);

                _methods[info.Name] = info;

                // resolve special methods
                var magic = MagicMethodByName(info.Name);
                if (magic != MagicMethods.undefined)
                {
                    if (_magicMethods == null)
                        _magicMethods = new Dictionary<MagicMethods, PhpMethodInfo>();

                    _magicMethods[magic] = info;
                }
            }
        }

        MagicMethods MagicMethodByName(string name)
        {
            var result = MagicMethods.undefined;

            if (name.StartsWith("__", StringComparison.Ordinal))
            {
                Enum.TryParse<MagicMethods>(name.ToLowerInvariant(), out result);
            }

            return result;
        }

        static readonly Func<MethodInfo, string> _MethodName = m => m.Name;

        static readonly Func<MethodInfo, bool> s_notObjectMember = m => m.DeclaringType != typeof(object);

        static readonly Func<MethodInfo, bool> s_notPhpHidden = m => m.GetCustomAttribute<PhpHiddenAttribute>() == null;

        static void SelectVisibleOverrides(List<MethodInfo> overrides)
        {
            if (overrides.Count > 1)
            {
                Type topPhpType = null;

                for (int i = 0; i < overrides.Count; i++)
                {
                    var t = overrides[i].DeclaringType;
                    if (t.GetPhpTypeInfo().IsPhpType)
                    {
                        if (topPhpType == null || t.IsSubclassOf(topPhpType))
                        {
                            topPhpType = t;
                        }
                    }
                }

                if (topPhpType != null) // deal with PHP-like overriding
                {
                    for (int i = overrides.Count - 1; i >= 0; i--)
                    {
                        var declaringType = overrides[i].DeclaringType;
                        if (declaringType != topPhpType && declaringType.GetPhpTypeInfo().IsPhpType)
                        {
                            overrides.RemoveAt(i);
                        }
                    }
                }
            }
        }

        #endregion

        #region IEnumerable<RoutineInfo>

        IEnumerator<RoutineInfo> IEnumerable<RoutineInfo>.GetEnumerator()
        {
            return (_methods != null)
                ? _methods.Values.GetEnumerator()
                : Enumerable.Empty<RoutineInfo>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<RoutineInfo>)this).GetEnumerator();

        #endregion

        /// <summary>
        /// Gets routine by its name.
        /// Returns <c>null</c> in case method does not exist.
        /// </summary>
        /// <param name="name">Method name.</param>
        /// <returns>Routine or <c>null</c> reference.</returns>
        public RoutineInfo this[string name]
        {
            get
            {
                PhpMethodInfo info;
                return (_methods != null && _methods.TryGetValue(name, out info)) ? info : null;
            }
        }

        /// <summary>
        /// Gets magic method if declared.
        /// </summary>
        public RoutineInfo this[MagicMethods magic]
        {
            get
            {
                Debug.Assert(magic != MagicMethods.undefined);

                PhpMethodInfo m;
                return (_magicMethods != null && _magicMethods.TryGetValue(magic, out m)) ? m : null;
            }
        }

        /// <summary>
        /// Gets enumeration of methods visible in given caller context.
        /// </summary>
        public IEnumerable<RoutineInfo> EnumerateVisible(Type typectx)
        {
            if (_methods != null)
            {
                Func<RoutineInfo, bool> predicate = (routine) =>
                {
                    var clrmethods = routine.Methods;
                    for (int i = 0; i < clrmethods.Length; i++)
                    {
                        if (clrmethods[i].IsVisible(typectx)) return true;
                    }

                    return false;
                };

                return _methods.Values.Where(predicate);
            }
            else
            {
                return Enumerable.Empty<RoutineInfo>();
            }
        }
    }
}
