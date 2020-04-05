using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Constructs callsite binders.
    /// </summary>
    public static class BinderFactory
    {
        #region _bindersCache

        /// <summary>
        /// Cache key of a binder instance.
        /// </summary>
        readonly struct BinderKey
        {
            public readonly int _type;
            public readonly string _name, _name2;
            public readonly RuntimeTypeHandle _h1, _h2, _h3;

            public BinderKey(int type, string name, string name2 = null, RuntimeTypeHandle h1 = default, RuntimeTypeHandle h2 = default, RuntimeTypeHandle h3 = default)
            {
                _type = type;
                _name = name;
                _name2 = name2;
                _h1 = h1;
                _h2 = h2;
                _h3 = h3;
            }
        }

        /// <summary>
        /// Comparer of <see cref="BinderKey"/>.
        /// </summary>
        sealed class BinderKeyComparer : IEqualityComparer<BinderKey>
        {
            public bool Equals(BinderKey x, BinderKey y)
            {
                return
                    x._type == y._type &&
                    x._name == y._name &&
                    x._h1.Equals(y._h1) &&
                    x._h2.Equals(y._h2) &&
                    x._h3.Equals(y._h3) &&
                    x._name2 == y._name2;
            }

            public int GetHashCode(BinderKey obj)
            {
                return obj._type ^ (obj._name != null ? obj._name.GetHashCode() : -1) ^ obj._h1.GetHashCode();
            }
        }

        /// <summary>
        /// Cache of instantiated binders.
        /// They hold rules cache which should be shared in order to not bind everythiung over and over.
        /// </summary>
        readonly static Dictionary<BinderKey, CallSiteBinder> _bindersCache = new Dictionary<BinderKey, CallSiteBinder>(new BinderKeyComparer());

        #endregion

        public static CallSiteBinder Function(string name, string nameOpt, RuntimeTypeHandle returnType)
        {
            var key = new BinderKey(1, name, nameOpt, returnType);
            if (_bindersCache.TryGetValue(key, out CallSiteBinder binder) == false)
            {
                _bindersCache[key] = binder = new CallFunctionBinder(name, nameOpt, returnType);
            }

            return binder;
        }

        public static CallSiteBinder InstanceFunction(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType)
        {
            var key = new BinderKey(2, name, null, classContext, returnType);
            if (_bindersCache.TryGetValue(key, out CallSiteBinder binder) == false)
            {
                _bindersCache[key] = binder = new CallInstanceMethodBinder(name, classContext, returnType);
            }

            return binder;
        }

        public static CallSiteBinder StaticFunction(RuntimeTypeHandle type, string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType)
        {
            var key = new BinderKey(3, name, null, type, classContext, returnType);
            if (_bindersCache.TryGetValue(key, out CallSiteBinder binder) == false)
            {
                _bindersCache[key] = binder = new CallStaticMethodBinder(type, name, classContext, returnType);
            }

            return binder;
        }

        public static CallSiteBinder GetField(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessMask access)
        {
            var key = new BinderKey(((int)access << 3) + 4, name, null, classContext, returnType);
            if (_bindersCache.TryGetValue(key, out CallSiteBinder binder) == false)
            {
                _bindersCache[key] = binder = new GetFieldBinder(name, classContext, returnType, access);
            }

            return binder;
        }

        public static CallSiteBinder SetField(string name, RuntimeTypeHandle classContext, AccessMask access)
        {
            var key = new BinderKey(((int)access << 3) + 5, name, null, classContext);
            if (_bindersCache.TryGetValue(key, out CallSiteBinder binder) == false)
            {
                _bindersCache[key] = binder = new SetFieldBinder(name, classContext, access);
            }

            return binder;
        }

        public static CallSiteBinder GetClassConst(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessMask access)
        {
            var key = new BinderKey(((int)access << 3) + 6, name, null, classContext, returnType);
            if (_bindersCache.TryGetValue(key, out CallSiteBinder binder) == false)
            {
                _bindersCache[key] = binder = new GetClassConstBinder(name, classContext, returnType, access);
            }

            return binder;
        }
    }

}
