using System;
using System.Collections.Concurrent;
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
                return obj._type ^ StringComparer.InvariantCulture.GetHashCode(obj._name ?? string.Empty) ^ obj._h1.GetHashCode();
            }
        }

        /// <summary>
        /// Cache of instantiated binders.
        /// They hold rules cache which should be shared in order to not bind everythiung over and over.
        /// </summary>
        readonly static ConcurrentDictionary<BinderKey, CallSiteBinder> _bindersCache = new ConcurrentDictionary<BinderKey, CallSiteBinder>(new BinderKeyComparer());

        #endregion

        public static CallSiteBinder Function(string name, string nameOpt, RuntimeTypeHandle returnType)
        {
            return _bindersCache.GetOrAdd(
                new BinderKey(1, name, nameOpt, returnType),
                b => new CallFunctionBinder(b._name, b._name2, b._h1));
        }

        public static CallSiteBinder InstanceFunction(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType)
        {
            return _bindersCache.GetOrAdd(
                new BinderKey(2, name, null, classContext, returnType),
                b => new CallInstanceMethodBinder(b._name, b._h1, b._h2));
        }

        public static CallSiteBinder StaticFunction(RuntimeTypeHandle type, string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType)
        {
            return _bindersCache.GetOrAdd(
                new BinderKey(3, name, null, type, classContext, returnType),
                b => new CallStaticMethodBinder(b._h1, b._name, b._h2, b._h3));
        }

        public static CallSiteBinder GetField(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessMask access)
        {
            return _bindersCache.GetOrAdd(
                new BinderKey(((int)access << 3) + 4, name, null, classContext, returnType),
                b => new GetFieldBinder(b._name, b._h1, b._h2, (AccessMask)(b._type >> 3)));
        }

        public static CallSiteBinder SetField(string name, RuntimeTypeHandle classContext, AccessMask access)
        {
            return _bindersCache.GetOrAdd(
                new BinderKey(((int)access << 3) + 5, name, null, classContext),
                b => new SetFieldBinder(b._name, b._h1, (AccessMask)(b._type >> 3)));
        }

        public static CallSiteBinder GetClassConst(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessMask access)
        {
            return _bindersCache.GetOrAdd(
                new BinderKey(((int)access << 3) + 6, name, null, classContext, returnType),
                b => new GetClassConstBinder(b._name, b._h1, b._h2, (AccessMask)(b._type >> 3)));
        }
    }

}
