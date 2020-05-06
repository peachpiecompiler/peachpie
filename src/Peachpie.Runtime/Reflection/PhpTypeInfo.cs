using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pchp.Core.Dynamic;
using Peachpie.Runtime.Dynamic;

namespace Pchp.Core.Reflection
{
    #region PhpTypeInfo

    /// <summary>
    /// Runtime information about a type.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    [DebuggerNonUserCode]
    public class PhpTypeInfo : ICloneable
    {
        /// <summary>
        /// Index to the type slot.
        /// <c>0</c> is an uninitialized index.
        /// </summary>
        internal int Index { get { return _index; } set { _index = value; } }
        protected int _index;

        /// <summary>
        /// Gets value indicating the type was declared in a users code.
        /// Otherwise the type was declared into application context.
        /// </summary>
        public bool IsUserType => _index > 0;

        /// <summary>
        /// Gets value indicating the type is an interface.
        /// </summary>
        public bool IsInterface => _type.IsInterface;

        /// <summary>
        /// Gets value indicating the type is a trait.
        /// </summary>
        public bool IsTrait => ReflectionUtils.IsTraitType(_type);

        /// <summary>
        /// Gets value indicating the type was declared within a PHP code.
        /// </summary>
        /// <remarks>This is determined based on a presence of <see cref="PhpTypeAttribute"/>.</remarks>
        public bool IsPhpType => GetPhpTypeAttribute() != null;

        /// <summary>
        /// Gets the associated PHP extension name.
        /// </summary>
        public string ExtensionName => _type.GetCustomAttribute<PhpExtensionAttribute>(false)?.FirstExtensionOrDefault;

        /// <summary>
        /// Gets the full type name in PHP syntax, cannot be <c>null</c> or empty.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the relative path to the file where the type is declared.
        /// Gets <c>null</c> if the type is from core, eval etc.
        /// </summary>
        public string RelativePath => GetPhpTypeAttribute()?.FileName;

        /// <summary>
        /// CLR type declaration.
        /// </summary>
        public TypeInfo Type => _type;
        readonly TypeInfo _type;

        /// <summary>
        /// Gets <see cref="RuntimeTypeHandle"/> of corresponding type information.
        /// </summary>
        public RuntimeTypeHandle TypeHandle => _type.UnderlyingSystemType.TypeHandle;

        /// <summary>
        /// Dynamically constructed delegate for object creation.
        /// </summary>
        public TObjectCreator Creator => _lazyCreator ?? BuildCreator();

        /// <summary>
        /// Gets value indicating the type can be publicly instantiated.
        /// If <c>true</c>, the class is not-abstract, not-trait, not-interface and has a public constructor.
        /// </summary>
        public bool isInstantiable => this.Creator != null /*ensures _flags initialized */ && (_flags & Flags.InstantiationNotAllowed) == 0;

        TObjectCreator Creator_private => _lazyCreatorPrivate ?? BuildCreatorPrivate();
        TObjectCreator Creator_protected => _lazyCreatorProtected ?? BuildCreatorProtected();
        TObjectCreator _lazyCreator, _lazyCreatorPrivate, _lazyCreatorProtected;

        /// <summary>
        /// Dynamically constructed delegate for object creation in specific type context.
        /// </summary>
        /// <param name="caller">Current type context in order to resolve only visible constructors.</param>
        public TObjectCreator ResolveCreator(Type caller)
        {
            if (caller != null)
            {
                if (caller == _type.AsType())
                {
                    // creation including private|protected|public .ctors
                    return this.Creator_private;
                }

                if (_lazyCreatorProtected == null || _lazyCreatorProtected != _lazyCreator) // in case protected creator == public creator, we can skip following checks
                {
                    if (caller.IsAssignableFrom(_type) || _type.IsAssignableFrom(caller))
                    {
                        // creation including protected|public .ctors
                        return this.Creator_protected;
                    }
                }
            }

            // creation using public .ctors
            return this.Creator;
        }

        /// <summary>
        /// Gets base type or <c>null</c> in case type does not extend another class.
        /// </summary>
        public PhpTypeInfo BaseType
        {
            get
            {
                if ((_flags & Flags.BaseTypePopulated) == 0)
                {
                    var binfo = _type.BaseType;
                    _lazyBaseType = (binfo != null && !binfo.IsHiddenType()) ? binfo.GetPhpTypeInfo() : null;
                    _flags |= Flags.BaseTypePopulated;
                }
                return _lazyBaseType;
            }
        }
        PhpTypeInfo _lazyBaseType;

        /// <summary>
        /// Build creation delegate using public .ctors.
        /// </summary>
        TObjectCreator BuildCreator()
        {
            lock (this)
            {
                if (_lazyCreator == null)
                {
                    if (ReflectionUtils.IsInstantiable(_type) && !IsTrait)
                    {
                        var ctors = _type.DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic && !c.IsPhpHidden()).ToArray();
                        if (ctors.Length != 0)
                        {
                            _lazyCreator = Dynamic.BinderHelpers.BindToCreator(_type.AsType(), ctors);
                        }
                        else
                        {
                            _flags |= Flags.InstantiationNotAllowed;
                            _lazyCreator = (_1, _2) => throw new InvalidOperationException($"Class {this.Name} cannot be instantiated. No .ctor found.");
                        }
                    }
                    else
                    {
                        _flags |= Flags.InstantiationNotAllowed;
                        _lazyCreator = (_1, _2) => throw PhpException.ErrorException(
                            this.IsInterface ? Resources.ErrResources.interface_instantiated :
                            this.IsTrait ? Resources.ErrResources.trait_instantiated :
                            /*this.IsAbstract*/ Resources.ErrResources.abstract_class_instantiated, // or static class
                            this.Name);
                    }
                }
            }

            return _lazyCreator;
        }

        /// <summary>
        /// Build creation delegate using public, protected and private .ctors.
        /// </summary>
        TObjectCreator BuildCreatorPrivate()
        {
            lock (this)
            {
                if (_lazyCreatorPrivate == null)
                {
                    if (ReflectionUtils.IsInstantiable(_type) && !IsTrait)
                    {
                        List<ConstructorInfo> ctorsList = null;
                        bool hasPrivate = false;
                        foreach (var c in _type.DeclaredConstructors)
                        {
                            if (!c.IsStatic && !c.IsPhpFieldsOnlyCtor() && !c.IsPhpHidden())
                            {
                                if (ctorsList == null) ctorsList = new List<ConstructorInfo>(1);
                                ctorsList.Add(c);
                                hasPrivate |= c.IsPrivate;
                            }
                        }
                        _lazyCreatorPrivate = hasPrivate
                            ? Dynamic.BinderHelpers.BindToCreator(_type.AsType(), ctorsList.ToArray())
                            : this.Creator_protected;
                    }
                    else
                    {
                        _flags |= Flags.InstantiationNotAllowed;
                        _lazyCreatorPrivate = this.Creator;
                    }
                }
            }

            return _lazyCreatorPrivate;
        }

        /// <summary>
        /// Build creation delegate using public and protected .ctors.
        /// </summary>
        TObjectCreator BuildCreatorProtected()
        {
            lock (this)
            {
                if (_lazyCreatorProtected == null)
                {
                    if (ReflectionUtils.IsInstantiable(_type) && !IsTrait)
                    {
                        List<ConstructorInfo> ctorsList = null;
                        bool hasProtected = false;
                        foreach (var c in _type.DeclaredConstructors)
                        {
                            if (!c.IsStatic && !c.IsPrivate && !c.IsPhpFieldsOnlyCtor() && !c.IsPhpHidden())
                            {
                                if (ctorsList == null) ctorsList = new List<ConstructorInfo>(1);
                                ctorsList.Add(c);
                                hasProtected |= c.IsFamily;
                            }
                        }
                        _lazyCreatorProtected = hasProtected
                            ? Dynamic.BinderHelpers.BindToCreator(_type.AsType(), ctorsList.ToArray())
                            : this.Creator;
                    }
                    else
                    {
                        _flags |= Flags.InstantiationNotAllowed;
                        _lazyCreatorProtected = this.Creator;
                    }
                }
            }

            return _lazyCreatorProtected;
        }

        internal PhpTypeInfo(Type/*!*/t)
        {
            Debug.Assert(t != null);
            _type = t.GetTypeInfo();

            Name = ResolvePhpTypeName(t, GetPhpTypeAttribute());

            // register type in extension tables
            ExtensionsAppContext.ExtensionsTable.AddType(this);
        }

        PhpTypeAttribute GetPhpTypeAttribute() => _type.GetCustomAttribute<PhpTypeAttribute>(false);

        /// <summary>
        /// Resolves PHP-like type name.
        /// </summary>
        static string ResolvePhpTypeName(Type tinfo, PhpTypeAttribute attr)
        {
            string name = (attr != null ? attr.TypeNameAs : PhpTypeAttribute.PhpTypeName.Default) switch
            {
                PhpTypeAttribute.PhpTypeName.Default => // CLR type 
                    tinfo.FullName      // full PHP type name instead of CLR type name
                    .Replace('.', '\\') // namespace separator
                    .Replace('+', '\\') // nested type separator
                    ,

                PhpTypeAttribute.PhpTypeName.NameOnly => tinfo.Name,

                PhpTypeAttribute.PhpTypeName.CustomName =>
                    attr.ExplicitTypeName ?? tinfo.Name,

                _ => throw new ArgumentException(),
            };

            Debug.Assert(!string.IsNullOrEmpty(name));

            // remove suffixed indexes if any (after a special metadata character)
            var idx = name.IndexOfAny(s_metadataSeparators);
            if (idx >= 0)
            {
                name = name.Remove(idx);
            }

            // Debug.Assert(ReflectionUtils.IsAllowedPhpName(name)); // anonymous classes have not allowed name, but it's ok

            //
            return name;
        }

        object ICloneable.Clone() => this;

        /// <summary>
        /// Array of characters used to separate class name from its metadata indexes (order, generics, etc).
        /// These characters and suffixed text has to be ignored.
        /// </summary>
        private static readonly char[] s_metadataSeparators = new[] { '#', '`', '<', '?' };

        #region Reflection

        /// <summary>
        /// Various type info flags.
        /// </summary>
        [Flags]
        enum Flags
        {
            BaseTypePopulated = 64,
            RuntimeFieldsHolderPopulated = 128,

            /// <summary>
            /// Marks the type that the class cannot be instantiated publically.
            /// </summary>
            InstantiationNotAllowed = 256,
        }

        Flags _flags;

        /// <summary>
        /// Gets collection of PHP methods in this type and base types.
        /// </summary>
        public TypeMethods RuntimeMethods => _runtimeMethods ?? (_runtimeMethods = new TypeMethods(this));
        TypeMethods _runtimeMethods;

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
                    _runtimeFieldsHolder = Dynamic.BinderHelpers.LookupRuntimeFields(_type.AsType());
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

    [DebuggerNonUserCode]
    public static class PhpTypeInfoExtension
    {
        /// <summary>
        /// <see cref="MethodInfo"/> of <see cref="PhpTypeInfoExtension.GetPhpTypeInfo{TType}"/>.
        /// </summary>
        readonly static MethodInfo s_getPhpTypeInfo_T = typeof(PhpTypeInfoExtension).GetRuntimeMethod("GetPhpTypeInfo", Dynamic.Cache.Types.Empty);

        /// <summary>
        /// Cache of resolved <see cref="PhpTypeInfo"/> corresponding to <see cref="RuntimeTypeHandle"/>.
        /// </summary>
        readonly static Dictionary<RuntimeTypeHandle, PhpTypeInfo> s_cache = new Dictionary<RuntimeTypeHandle, PhpTypeInfo>();

        /// <summary>
        /// RW lock used ot access underlaying cache.
        /// </summary>
        readonly static ReaderWriterLockSlim s_lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

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
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsByRef)
            {
                type = type.GetElementType() ?? throw new InvalidOperationException();
            }

            PhpTypeInfo result = null;
            var handle = type.TypeHandle;

            // lookup cache first
            s_lock.EnterUpgradeableReadLock();
            try
            {
                if (!s_cache.TryGetValue(handle, out result))
                {
                    s_lock.EnterWriteLock();
                    try
                    {
                        if (s_cache.TryGetValue(handle, out result))
                        {
                            // double checked lock
                        }
                        else if (type.IsGenericTypeDefinition) // TODO: or type.IsByRefLike - netstandard2.1
                        {
                            // generic type definition cannot be used as a type parameter for GetPhpTypeInfo<T>
                            // just instantiate the type info and cache the result
                            result = new PhpTypeInfo(type);
                        }
                        else
                        {
                            // invoke GetPhpTypeInfo<TType>() dynamically and cache the result
                            result = (PhpTypeInfo)s_getPhpTypeInfo_T
                                .MakeGenericMethod(type)
                                .Invoke(null, Array.Empty<object>());
                        }

                        //
                        s_cache[handle] = result;
                    }
                    finally
                    {
                        s_lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                s_lock.ExitUpgradeableReadLock();
            }

            //
            Debug.Assert(result != null);
            return result;
        }

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of given <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">Type handle of the CLR type.</param>
        public static PhpTypeInfo GetPhpTypeInfo(this RuntimeTypeHandle handle)
        {
            PhpTypeInfo result = null;

            if (handle.Equals(default))
            {
                return null;
            }

            // lookup cache first
            s_lock.EnterReadLock();
            try
            {
                s_cache.TryGetValue(handle, out result);
            }
            finally
            {
                s_lock.ExitReadLock();
            }

            return result ?? GetPhpTypeInfo(Type.GetTypeFromHandle(handle));
        }

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of given <paramref name="typeInfo"/>.
        /// </summary>
        public static PhpTypeInfo GetPhpTypeInfo(this TypeInfo typeInfo) => typeInfo.AsType().GetPhpTypeInfo();

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of given object.
        /// </summary>
        public static PhpTypeInfo GetPhpTypeInfo(this object obj) => obj.GetType().GetPhpTypeInfo();

        /// <summary>
        /// Enumerates self, all base types and all inherited interfaces.
        /// </summary>
        public static IEnumerable<PhpTypeInfo> EnumerateTypeHierarchy(this PhpTypeInfo phptype)
        {
            return EnumerateClassHierarchy(phptype).Concat( // phptype + base types
                phptype.Type.GetInterfaces().Select(GetPhpTypeInfo)); // inherited interfaces
        }

        /// <summary>
        /// Enumerates self and all base types.
        /// </summary>
        static IEnumerable<PhpTypeInfo> EnumerateClassHierarchy(this PhpTypeInfo phptype)
        {
            for (; phptype != null; phptype = phptype.BaseType)
            {
                yield return phptype;
            }
        }

        /// <summary>
        /// Gets a collection of the trait types implemented by the current type.
        /// </summary>
        public static IEnumerable<PhpTypeInfo> GetImplementedTraits(this PhpTypeInfo phptype)
        {
            if (ReferenceEquals(phptype, null))
            {
                throw new ArgumentNullException(nameof(phptype));
            }

            foreach (var f in phptype.Type.DeclaredFields)
            {
                // traits instance is stored in a fields:
                // private readonly TraitType<phptype> <>trait_TraitType;

                if (f.IsPrivate && f.IsInitOnly && f.Name.StartsWith("<>trait_") && f.FieldType.IsConstructedGenericType)
                {
                    yield return GetPhpTypeInfo(f.FieldType.GetGenericTypeDefinition());
                }
            }
        }

        /// <summary>
        /// Gets value indicating the type has been declared in the context.
        /// </summary>
        public static bool IsDeclared(this PhpTypeInfo phptype, Context ctx) => !phptype.IsUserType || ctx.IsUserTypeDeclared(phptype);

        /// <summary>
        /// Cached functions that creates an empty instance (instance of a class without calling its PHP constructor).
        /// </summary>
        readonly static Dictionary<PhpTypeInfo, Func<Context, object>> s_emptyObjectActivators = new Dictionary<PhpTypeInfo, Func<Context, object>>();

        /// <summary>
        /// Creates instance of the class without invoking its constructor.
        /// </summary>
        public static object CreateUninitializedInstance(this PhpTypeInfo phptype, Context ctx)
        {
            Func<Context, object> activator;

            s_lock.EnterUpgradeableReadLock();
            try
            {
                if (!s_emptyObjectActivators.TryGetValue(phptype, out activator))
                {
                    s_lock.EnterWriteLock();
                    try
                    {
                        if (!s_emptyObjectActivators.TryGetValue(phptype, out activator) &&
                            !TryBuildCreateEmptyObjectFunc(phptype, out activator))
                        {
                            activator = (_ctx) =>
                            {
                                throw new NotSupportedException();
                            };
                        }

                        s_emptyObjectActivators[phptype] = activator;
                    }
                    finally
                    {
                        s_lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                s_lock.ExitUpgradeableReadLock();
            }

            return activator(ctx);
        }

        /// <summary>
        /// Builds delegate that creates uninitialized class instance for purposes of deserialization and reflection.
        /// </summary>
        static bool TryBuildCreateEmptyObjectFunc(PhpTypeInfo tinfo, out Func<Context, object> activator)
        {
            Debug.Assert(tinfo != null);

            activator = null;

            if (tinfo.IsInterface)
            {
                // cannot be instantiated
            }
            else if (tinfo.Type.IsAbstract)
            {
                // abstract class,
                // generate a non-abstract class that implements this one:

                var dummytype = EmitHelpers.CreatDefaultAbstractClassImplementation(tinfo.Type, out var ctor);
                activator = _ctx => ctor.Invoke(new object[] { _ctx });
            }
            else if (tinfo.IsTrait)
            {
                // trait class, can be instantiated using following (and the only) .ctor:
                // .ctor( Context, TSelf )

                if (tinfo.Type.IsGenericTypeDefinition)
                {
                    // trait class must be constructed first!
                    throw new NotSupportedException("Trait type is not constructed, cannot be created.");
                    // tinfo = tinfo.Type.MakeGenericType(typeof(object));
                }
                else
                {
                    foreach (var c in tinfo.Type.GetConstructors())
                    {
                        // there is only one .ctor:
                        var ps = c.GetParameters();
                        if (ps.Length == 2 && ps[0].IsContextParameter() && ps[1].ParameterType == typeof(object) && c.IsPublic)
                        {
                            activator = _ctx => c.Invoke(new[] { _ctx, new object(), });
                        }
                    }
                }
            }
            else
            {
                // regular instantiable class

                foreach (var c in tinfo.Type.DeclaredConstructors)
                {
                    if (c.IsStatic || c.IsPrivate || c.IsPhpHidden())
                    {
                        continue;
                    }

                    var ps = c.GetParameters();

                    switch (ps.Length)
                    {
                        // .ctor()
                        case 0:
                            activator = (_ctx) => c.Invoke(Array.Empty<object>());
                            break;

                        // .ctor(Context)
                        case 1 when ps[0].IsContextParameter():
                            activator = (_ctx) => c.Invoke(new object[] { _ctx });
                            break;

                        // [PhpFieldsOnly] .ctor(Context, Dummy)
                        case 2 when ps[0].IsContextParameter() && ps[1].ParameterType == typeof(DummyFieldsOnlyCtor):
                            activator = (_ctx) => c.Invoke(new object[] { _ctx, default(DummyFieldsOnlyCtor) });
                            break;
                    }

                    //
                    if (activator != null && c.IsPhpFieldsOnlyCtor())
                    {
                        // best candidate
                        break;
                    }
                }
            }

            //
            return activator != null;
        }
    }

    /// <summary>
    /// Delegate for dynamic object creation.
    /// </summary>
    /// <param name="ctx">Current runtime context. Cannot be <c>null</c>.</param>
    /// <param name="arguments">List of arguments to be passed to called constructor.</param>
    /// <returns>Object instance.</returns>
    public delegate object TObjectCreator(Context ctx, params PhpValue[] arguments);

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
        public static readonly PhpTypeInfo TypeInfo = new PhpTypeInfo(typeof(TType));
    }

    #endregion
}
