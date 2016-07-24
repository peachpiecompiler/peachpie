using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    #region TypeInfo

    /// <summary>
    /// Runtime information about a type.
    /// </summary>
    public class TypeInfo
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

        internal TypeInfo(Type t)
        {
            Debug.Assert(t != null);
            _type = t;
            _name = t.FullName.Replace('.', '\\');
        }

        #region Reflection

        /// <summary>
        /// Various type info flags.
        /// </summary>
        [Flags]
        enum Flags
        {

            RuntimeFieldsHolderPopulated = 128,
        }

        Flags _flags;

        // TODO: magic methods (__call, __callStatic, __get, __set, ...)

        /// <summary>
        /// Gets collection of PHP constants declared in this type.
        /// </summary>
        public TypeConstants DeclaredConstants => _declaredConstants ?? (_declaredConstants = new TypeConstants(_type));
        TypeConstants _declaredConstants;

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
        public static readonly TypeInfo TypeInfo = Create(typeof(TType));

        static TypeInfo Create(Type type)
        {
            return new TypeInfo(type);
        }
    }

    #endregion
}
