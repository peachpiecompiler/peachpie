using Pchp.Core.Dynamic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Collection of PHP type methods and magic methods.
    /// </summary>
    public class TypeMethods
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

        internal TypeMethods(Type type)
        {
            // collect available methods
            foreach (var m in type.GetTypeInfo().DeclaredMethods.ToLookup(_MethodName, StringComparer.OrdinalIgnoreCase))
            {
                if (!ReflectionUtils.IsAllowedPhpName(m.Key))   // .ctor, .phpnew, implicit interface implementation
                {
                    continue;
                }

                if (_methods == null)
                {
                    _methods = new Dictionary<string, PhpMethodInfo>(StringComparer.OrdinalIgnoreCase);
                }

                var info = new PhpMethodInfo(_methods.Count + 1, m.Key, m.ToArray());

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
    }
}
