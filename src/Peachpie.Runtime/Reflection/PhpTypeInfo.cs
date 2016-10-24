using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    #region PhpTypeInfo

    /// <summary>
    /// Runtime information about a type.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    public class PhpTypeInfo
    {
        /// <summary>
        /// Index to the type slot.
        /// <c>0</c> is an uninitialized index.
        /// </summary>
        internal int Index { get { return _index; } set { _index = value; } }
        protected int _index;

        /// <summary>
        /// Whether the type is declared in application context.
        /// </summary>
        internal bool IsInAppContext => _index < 0;

        /// <summary>
        /// Gets the type name in PHP synytax, cannot be <c>null</c> or empty.
        /// </summary>
        public string Name => _name;
        protected readonly string _name;

        /// <summary>
        /// CLR type declaration.
        /// </summary>
        public Type Type => _type;
        readonly Type _type;

        /// <summary>
        /// Dynamically constructed delegate for object creation.
        /// </summary>
        public TObjectCreator Creator => _lazyCreator ?? BuildCreator();
        TObjectCreator _lazyCreator;

        /// <summary>
        /// Gets base type or <c>null</c> in case type does not extend another class.
        /// </summary>
        public PhpTypeInfo BaseType
        {
            get
            {
                if ((_flags & Flags.BaseTypePopulated) == 0)
                {
                    var binfo = _type.GetTypeInfo().BaseType;
                    _lazyBaseType = (binfo != null && binfo != typeof(object)) ? binfo.GetPhpTypeInfo() : null;
                    _flags |= Flags.BaseTypePopulated;
                }
                return _lazyBaseType;
            }
        }
        PhpTypeInfo _lazyBaseType;

        TObjectCreator BuildCreator()
        {
            lock (this)
            {
                if (_lazyCreator == null)
                {
                    var ctors = _type.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic).ToArray();
                    _lazyCreator = Dynamic.BinderHelpers.BindToCreator(ctors);
                }
            }

            return _lazyCreator;
        }

        internal PhpTypeInfo(Type t)
        {
            Debug.Assert(t != null);
            _type = t;
            _name = t.FullName  // full PHP type name instead of CLR type name
                .Replace('.', '\\')     // namespace separator
                .Replace('+', '\\');    // nested type separator

            // remove suffixed indexes (after a special metadata character)
            var idx = _name.IndexOfAny(_metadataSeparators);
            if (idx >= 0)
            {
                _name = _name.Remove(idx);
            }
        }

        /// <summary>
        /// Array of characters used to separate class name from its metadata indexes (order, generics, etc).
        /// These characters and suffixed text has to be ignored.
        /// </summary>
        private static readonly char[] _metadataSeparators = new[] { '#', '@', '`' };

        #region Reflection

        /// <summary>
        /// Various type info flags.
        /// </summary>
        [Flags]
        enum Flags
        {
            BaseTypePopulated = 64,
            RuntimeFieldsHolderPopulated = 128,
        }

        Flags _flags;

        /// <summary>
        /// Gets collection of PHP methods in this type.
        /// </summary>
        public TypeMethods DeclaredMethods => _declaredMethods ?? (_declaredMethods = new TypeMethods(_type));
        TypeMethods _declaredMethods;

        /// <summary>
        /// Gets collection of PHP fields, static fields and constants declared in this type.
        /// </summary>
        public TypeFields DeclaredFields => _declaredfields ?? (_declaredfields = new TypeFields(_type));
        TypeFields _declaredfields;

        /// <summary>
        /// Gets field holding the array of runtime fields.
        /// Can be <c>null</c>.
        /// </summary>
        public FieldInfo RuntimeFieldsHolder
        {
            get
            {
                if ((_flags & Flags.RuntimeFieldsHolderPopulated) == 0)
                {
                    _runtimeFieldsHolder = Dynamic.BinderHelpers.LookupRuntimeFields(_type);
                    _flags |= Flags.RuntimeFieldsHolderPopulated;
                }
                return _runtimeFieldsHolder;
            }
        }
        FieldInfo _runtimeFieldsHolder;

        // TODO: PHPDoc

        #endregion
    }

    #endregion

    #region PhpTypeInfoExtension

    public static class PhpTypeInfoExtension
    {
        static MethodInfo _lazyGetPhpTypeInfo_T;

        readonly static Dictionary<RuntimeTypeHandle, PhpTypeInfo> _cache = new Dictionary<RuntimeTypeHandle, PhpTypeInfo>();

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of given <typeparamref name="TType"/>.
        /// </summary>
        /// <typeparam name="TType">Type to get info about.</typeparam>
        /// <returns>Runtime type information.</returns>
        public static PhpTypeInfo GetPhpTypeInfo<TType>()
            => TypeInfoHolder<TType>.TypeInfo;

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of given <paramref name="type"/>.
        /// </summary>
        public static PhpTypeInfo GetPhpTypeInfo(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            PhpTypeInfo result = null;
            var handle = type.TypeHandle;

            // lookup cache first
            lock (_cache)
            {
                _cache.TryGetValue(handle, out result);
            }

            // invoke GetPhpTypeInfo<TType>() dynamically and cache the result
            if (result == null)
            {
                if (_lazyGetPhpTypeInfo_T == null)
                {
                    _lazyGetPhpTypeInfo_T = typeof(PhpTypeInfoExtension).GetRuntimeMethod("GetPhpTypeInfo", Dynamic.Cache.Types.Empty);
                }

                // TypeInfoHolder<TType>.TypeInfo;
                result = (PhpTypeInfo)_lazyGetPhpTypeInfo_T
                    .MakeGenericMethod(type)
                    .Invoke(null, Utilities.ArrayUtils.EmptyObjects);

                lock (_cache)
                {
                    _cache[handle] = result;
                }
            }

            //
            return result;
        }
    }


    /// <summary>
    /// Delegate for dynamic object creation.
    /// </summary>
    /// <param name="ctx">Current runtime context. Cannot be <c>null</c>.</param>
    /// <param name="arguments">List of arguments to be passed to called constructor.</param>
    /// <returns>Object instance.</returns>
    public delegate object TObjectCreator(Context ctx, PhpValue[] arguments);

    #endregion

    #region TypeInfoHolder

    /// <summary>
    /// Helper class holding runtime type information about <typeparamref name="TType"/>.
    /// </summary>
    /// <typeparam name="TType">CLR type.</typeparam>
    internal static class TypeInfoHolder<TType>
    {
        /// <summary>
        /// Associated runtime type information.
        /// </summary>
        public static readonly PhpTypeInfo TypeInfo = Create(typeof(TType));

        static PhpTypeInfo Create(Type type)
        {
            return new PhpTypeInfo(type);
        }
    }

    #endregion
}
