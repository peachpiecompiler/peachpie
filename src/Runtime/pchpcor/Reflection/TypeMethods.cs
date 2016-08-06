using Pchp.Core.Dynamic;
using System;
using System.Collections.Generic;
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
        #region Fields
        /// <summary>
        /// Index to the magic method.
        /// </summary>
        readonly short _get, _set, _call, _callStatic, _isset, _unset, _invoke, _clone, _toString;

        /// <summary>
        /// Array of declared routines.
        /// </summary>
        readonly PhpMethodInfo[] _methods;

        /// <summary>
        /// Map of routine name to its index in <see cref="_methods"/>.
        /// </summary>
        readonly Dictionary<string, int> _methodsByName;

        #endregion

        #region Initialization

        internal TypeMethods(Type type)
        {
            var methods = new List<PhpMethodInfo>();

            // collect available methods
            foreach (var m in type.GetTypeInfo().DeclaredMethods.ToLookup(_MethodName, StringComparer.OrdinalIgnoreCase))
            {
                var info = new PhpMethodInfo(methods.Count + 1, m.Key, m.ToArray());
                methods.Add(info);

                // resolve special methods
                if (info.Name.StartsWith("__", StringComparison.Ordinal))
                {
                    switch (info.Name.ToLowerInvariant())
                    {
                        case "__get": _get = (short)info.Index; break;
                        case "__set": _set = (short)info.Index; break;
                        case "__call": _call = (short)info.Index; break;
                        case "__callstatic": _callStatic = (short)info.Index; break;
                        case "__isset": _isset = (short)info.Index; break;
                        case "__unset": _unset = (short)info.Index; break;
                        case "__invoke": _invoke = (short)info.Index; break;
                        case "__clone": _clone = (short)info.Index; break;
                        case "__tostring": _toString = (short)info.Index; break;
                        case "__sleep": break;
                        case "__wakeup": break;
                    }
                }
            }

            _methods = (methods.Count != 0) ? methods.ToArray() : null;

            // init map of names
            if (_methods != null)
            {
                _methodsByName = new Dictionary<string, int>(_methods.Length, StringComparer.OrdinalIgnoreCase);

                foreach (var r in _methods)
                {
                    _methodsByName.Add(r.Name, r.Index - 1);
                }
            }
        }

        static Func<MethodInfo, string> _MethodName = m => m.Name;

        #endregion

        internal Expression Bind(string name, Type classCtx, Type treturn, Expression target, Expression ctx, params Expression[] arguments)
        {
            int idx;
            if (_methodsByName != null && _methodsByName.TryGetValue(name, out idx))
            {
                var m = _methods[idx];
                var methods = m.Methods.SelectVisible(classCtx);
                if (methods.Length != 0)
                {
                    return OverloadBinder.BindOverloadCall(treturn, target, methods, ctx, arguments);
                }
            }

            // TODO: magic call

            if (target != null && _call != 0)
            {
                var m = _methods[_call];
                // return Dynamic.OverloadBinder.BindOverloadCall(treturn, target, m.Methods, ctx, name, args[]);
            }

            if (target == null && _callStatic != 0)
            {
                // return Dynamic.OverloadBinder.BindOverloadCall(treturn, target, _methods[_callStatic].Methods, ctx, name, args[]);
            }

            //
            return null;
        }
    }
}
