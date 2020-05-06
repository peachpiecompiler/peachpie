using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Pchp.Core.Resources;

namespace Pchp.Library.Reflection
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionClass : Reflector, IPhpCloneable
    {
        #region Constants

        /// <summary>
        /// Indicates class that is abstract because it has some abstract methods.
        /// </summary>
        public const int IS_IMPLICIT_ABSTRACT = 16;

        /// <summary>
        /// Indicates class that is abstract because of its definition.
        /// </summary>
        public const int IS_EXPLICIT_ABSTRACT = 32;

        /// <summary>
        /// Indicates final class.
        /// </summary>
        public const int IS_FINAL = 64;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Gets name of the class.
        /// </summary>
        public string name
        {
            get
            {
                return _tinfo.Name;
            }
            //set
            //{
            //    // Read-only, throws ReflectionException in attempt to write.
            //    throw new ReflectionException(); // TODO: message
            //}
        }

        /// <summary>
        /// Underlaying type information.
        /// Cannot be <c>null</c>.
        /// </summary>
        internal PhpTypeInfo _tinfo;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected ReflectionClass() { }

        internal ReflectionClass(PhpTypeInfo tinfo)
        {
            Debug.Assert(tinfo != null);
            _tinfo = tinfo;
        }

        public ReflectionClass(Context ctx, PhpValue @class)
        {
            __construct(ctx, @class);
        }

        public void __construct(Context ctx, PhpValue @class)
        {
            Debug.Assert(_tinfo == null, "Subsequent call not allowed.");

            _tinfo = ReflectionUtils.ResolvePhpTypeInfo(ctx, @class);
        }

        #endregion

        public PhpValue getConstant(Context ctx, string name)
        {
            var p = _tinfo.GetDeclaredConstant(name);
            if (p != null)
            {
                return p.GetValue(ctx, null);
            }

            //
            return PhpValue.False;
        }

        public PhpArray getConstants(Context ctx)
        {
            var result = new PhpArray();
            foreach (var p in _tinfo.GetDeclaredConstants())
            {
                result.Add(p.PropertyName, p.GetValue(ctx, null));
            }
            return result;
        }

        /// <summary>
        /// Gets class constants.
        /// </summary>
        [return: NotNull]
        public PhpArray getReflectionConstants()
        {
            var result = new PhpArray();
            foreach (var p in _tinfo.GetDeclaredConstants())
            {
                result.Add(new ReflectionClassConstant(p));
            }
            return result;
        }

        [return: CastToFalse]
        public ReflectionClassConstant getReflectionConstant(string name)
        {
            var p = _tinfo.GetDeclaredConstant(name);
            return p != null ? new ReflectionClassConstant(p) : null;
        }

        /// <summary>
        /// Gets the constructor of the class.
        /// </summary>
        /// <returns>A <see cref="ReflectionMethod"/> object reflecting the class' constructor,
        /// or NULL if the class has no constructor.</returns>
        public ReflectionMethod getConstructor()
        {
            var routine = _tinfo.RuntimeMethods[Pchp.Core.Reflection.ReflectionUtils.PhpConstructorName];
            return (routine != null)
                ? new ReflectionMethod(routine)
                : null;
        }

        [return: NotNull]
        public PhpArray getDefaultProperties(Context ctx)
        {
            var tinfo = _tinfo;

            if (tinfo.IsInterface)
            {
                // interfaces cannot have properties:
                return PhpArray.NewEmpty();
            }
            else if (tinfo.IsTrait && tinfo.Type.IsGenericTypeDefinition)
            {
                // construct the generic trait class with <object>
                tinfo = tinfo.Type.MakeGenericType(typeof(object)).GetPhpTypeInfo();
            }

            // we have to instantiate the type to get the initial values:
            var inst = tinfo.CreateUninitializedInstance(ctx);
            if (inst != null)
            {
                var array = new PhpArray();

                foreach (var p in tinfo.GetDeclaredProperties())
                {
                    array[p.PropertyName] = p.GetValue(ctx, inst);
                }

                return array;
            }

            //
            throw new NotSupportedException("not instantiable type");
        }

        [return: CastToFalse]
        public string getDocComment() => ReflectionUtils.getDocComment(_tinfo.Type);

        public ReflectionExtension getExtension()
        {
            var extensionName = _tinfo.ExtensionName;
            return extensionName != null
                ? new ReflectionExtension(extensionName)
                : null; // NULL
        }

        [return: CastToFalse]
        public string getExtensionName() => _tinfo.ExtensionName; // null means FALSE

        /// <summary>Gets the filename of the file in which the class has been defined</summary>
        /// <param name="ctx">Current runtime context</param>
        /// <returns>Returns the filename of the file in which the class has been defined.
        /// If the class is defined in the PHP core or in a PHP extension, FALSE is returned.</returns>
        [return: CastToFalse]
        public string getFileName(Context ctx)
        {
            var path = _tinfo.RelativePath;

            return path != null
                ? Path.GetFullPath(Path.Combine(ctx.RootPath, path))
                : null;
        }

        public PhpArray getInterfaceNames()
        {
            var result = new PhpArray();

            foreach (var t in _tinfo.Type.ImplementedInterfaces)
            {
                if (t.IsHiddenType())
                    continue;

                result.Add(t.GetPhpTypeInfo().Name);
            }

            return result;
        }
        public PhpArray getInterfaces()
        {
            var result = new PhpArray();

            foreach (var t in _tinfo.Type.ImplementedInterfaces)
            {
                if (t.IsHiddenType())
                    continue;

                var iinfo = t.GetPhpTypeInfo();
                result.Add(iinfo.Name, PhpValue.FromClass(new ReflectionClass(iinfo)));
            }

            return result;
        }

        /// <summary>
        /// Gets a <see cref="ReflectionMethod"/> for a class method.
        /// </summary>
        /// <param name="name">The method name to reflect, throws <see cref="ReflectionException"/>
        /// if the method doesn't exist.</param>
        /// <returns>A <see cref="ReflectionMethod"/>.</returns>
        /// <exception cref="ReflectionException"/>
        public ReflectionMethod getMethod(string name)
        {
            var routine = _tinfo.RuntimeMethods[name];

            if (routine == null && _tinfo.IsInterface)
            {
                // look into interface interfaces (CLR does not do that in RuntimeMethods)
                foreach (var t in _tinfo.Type.ImplementedInterfaces)
                {
                    if ((routine = t.GetPhpTypeInfo().RuntimeMethods[name]) != null)
                    {
                        break;
                    }
                }
            }

            //
            return (routine != null)
                ? new ReflectionMethod(routine)
                : throw new ReflectionException();
        }

        public PhpArray getMethods(long filter = -1)
        {
            IEnumerable<RoutineInfo> routines = _tinfo.RuntimeMethods;

            if (_tinfo.IsInterface)
            {
                // enumerate all the interface and collect their methods:
                foreach (var t in _tinfo.Type.ImplementedInterfaces)
                {
                    routines = routines.Concat(t.GetPhpTypeInfo().RuntimeMethods);
                }

                // TODO: remove duplicit names
            }
            else
            {
                // routines are already merged from all the type hierarchy:
            }

            //
            var result = new PhpArray();

            foreach (var routine in routines)
            {
                var rmethod = new ReflectionMethod(routine);
                if (filter == -1 || ((int)rmethod.getModifiers() & filter) != 0)
                {
                    result.Add(rmethod);
                }
            }

            return result;
        }
        public long getModifiers()
        {
            long flags = 0;

            if (_tinfo.Type.IsSealed) flags |= IS_FINAL;
            if (_tinfo.Type.IsAbstract) flags |= IS_EXPLICIT_ABSTRACT;

            return flags;
        }
        public string getName() => this.name;
        public string getNamespaceName()
        {
            // opposite of getShortName()
            var name = this.name;
            var sep = name.LastIndexOf(ReflectionUtils.NameSeparator);
            return (sep < 0) ? string.Empty : name.Remove(sep);
        }
        [return: CastToFalse]
        public ReflectionClass getParentClass() => (_tinfo.BaseType != null) ? new ReflectionClass(_tinfo.BaseType) : null;

        /// <summary>
        /// Retrieves reflected properties.
        /// </summary>
        /// <param name="filter">Optional filter. A bit mask of modifiers (see <see cref="ReflectionClass"/> constants).</param>
        /// <returns></returns>
        public virtual PhpArray getProperties(int filter = 0)
        {
            var result = new PhpArray(8);
            foreach (var p in _tinfo.GetDeclaredProperties())
            {
                var pinfo = new ReflectionProperty(p);
                if (filter == 0 || ((int)pinfo.getModifiers() & filter) != 0)
                {
                    result.Add(PhpValue.FromClass(pinfo));
                }
            }

            return result;
        }

        [return: CastToFalse]
        public virtual ReflectionProperty getProperty(string name)
        {
            var prop = _tinfo.GetDeclaredProperty(name);
            return (prop != null) ? new ReflectionProperty(prop) : null;
        }
        public string getShortName()
        {
            var name = this.name;
            var sep = name.LastIndexOf(ReflectionUtils.NameSeparator);
            return (sep < 0) ? name : name.Substring(sep + 1);
        }

        [return: CastToFalse]
        public int getStartLine()
        {
            PhpException.FunctionNotSupported("ReflectionClass::getStartLine");
            return -1;
        }

        [return: CastToFalse]
        public int getEndLine()
        {
            PhpException.FunctionNotSupported("ReflectionClass::getEndLine");
            return -1;
        }

        [return: NotNull]
        public PhpArray getStaticProperties(Context ctx)
        {
            var array = new PhpArray();

            foreach (var p in TypeMembersUtils.GetDeclaredProperties(_tinfo))
            {
                if (p.IsStatic)
                {
                    array[p.PropertyName] = p.GetValue(ctx, null);
                }
            }

            return array;
        }

        public PhpValue getStaticPropertyValue(Context ctx, string name)
        {
            var prop = _tinfo.GetDeclaredProperty(name) ?? throw new ReflectionException();
            return prop.GetValue(ctx, null);
        }

        public void setStaticPropertyValue(Context ctx, string name, PhpValue def_value)
        {
            var prop = _tinfo.GetDeclaredProperty(name) ?? throw new ReflectionException();
            prop.SetValue(ctx, null, def_value);
        }

        public PhpArray getTraitAliases() { throw new NotImplementedException(); }

        public PhpArray getTraitNames()
        {
            var result = new PhpArray();

            // get traits (in current class only, not the parent)
            // { trait_name }

            foreach (var t in _tinfo.GetImplementedTraits())
            {
                result.Add(t.Name);
            }

            return result;
        }
        public PhpArray getTraits()
        {
            var result = new PhpArray();

            // get traits (in current class only, not the parent)
            // { [trait_name] => ReflectionClass(trait) }

            foreach (var t in _tinfo.GetImplementedTraits())
            {
                result.Add(t.Name, PhpValue.FromClass(new ReflectionClass(t)));
            }

            return result;
        }
        public bool hasConstant(string name) => _tinfo.GetDeclaredConstant(name) != null;
        public bool hasMethod(string name) => _tinfo.RuntimeMethods[name] != null;
        public bool hasProperty(string name) => _tinfo.Type.GetField(name) != null;
        public bool implementsInterface(string @interface) => _tinfo.Type.GetInterface(@interface.Replace("\\", ".")) != null;
        public bool inNamespace() => this.name.IndexOf(ReflectionUtils.NameSeparator) >= 0;
        public bool isAbstract() => _tinfo.Type.IsAbstract;
        public bool isAnonymous() => _tinfo.Type.IsSealed && _tinfo.Type.IsNotPublic && _tinfo.Type.Name.StartsWith("class@anonymous", StringComparison.Ordinal); // internal sealed 'class@anonymous...' {}=

        /// <summary>
        /// Determines whether the class can be cloned.
        /// </summary>
        public bool isCloneable()
        {
            if (_tinfo.IsInterface || _tinfo.IsTrait || _tinfo.Type.IsAbstract)
            {
                return false;
            }

            if (typeof(IPhpCloneable).IsAssignableFrom(_tinfo.Type))
            {
                // internal: implements IPhpCloneable
                return true;
            }

            var __clone = _tinfo.RuntimeMethods[TypeMethods.MagicMethods.__clone];
            if (__clone != null && !__clone.Methods.All(m => m.IsPublic))
            {
                return false;
            }

            if (!_tinfo.Type.DeclaredConstructors.Any(c => !c.IsStatic && !c.IsPrivate))
            {
                // no available .ctor
                return false;
            }

            //
            return true;
        }
        public bool isFinal() => _tinfo.Type.IsSealed;
        public bool isInstance(object @object) => _tinfo.Type.IsInstanceOfType(@object);
        public bool isInstantiable() => _tinfo.isInstantiable;
        public bool isInterface() => _tinfo.IsInterface;
        public bool isInternal() => !isUserDefined();

        [Obsolete("Instead of the missspelled RefectionClass::isIterateable(), ReflectionClass::isIterable() should be preferred.")]
        public bool isIterateable() => isIterable();    // alias to isIterable()

        public bool isIterable() => _tinfo.Type.IsSubclassOf(typeof(Iterator)) || _tinfo.Type.IsSubclassOf(typeof(IteratorAggregate)) || _tinfo.Type.IsSubclassOf(typeof(System.Collections.IEnumerable));

        /// <summary>
        /// Checks if the class is a subclass of a specified class or implements a specified interface.
        /// </summary>
        /// <param name="ctx">Current runtime context</param>
        /// <param name="class">The class name being checked against.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public bool isSubclassOf(Context ctx, string @class)
        {
            // Look for the class, use autoload and throw an error if it doesn't exist
            var base_tinfo = ctx.GetDeclaredType(@class, true);
            if (base_tinfo == null)
            {
                throw new ReflectionException();
            }

            return base_tinfo.Type.IsAssignableFrom(_tinfo.Type);
        }

        public bool isTrait() => _tinfo.IsTrait;
        public bool isUserDefined() => _tinfo.IsUserType || _tinfo.RelativePath != null;
        public object newInstance(Context ctx, params PhpValue[] args) => _tinfo.Creator(ctx, args);
        public object newInstanceArgs(Context ctx, PhpArray args) => newInstance(ctx, args.GetValues());
        public object newInstanceWithoutConstructor(Context ctx) => _tinfo.CreateUninitializedInstance(ctx);
        public void setStaticPropertyValue(string name, PhpValue value) { throw new NotImplementedException(); }

        #region Reflector

        public static string export(PhpValue argument, bool @return = false)
        {
            throw new NotImplementedException();
        }

        public virtual string __toString()
        {
            throw new NotImplementedException();
        }

        #endregion

        object IPhpCloneable.Clone() => throw PhpException.ErrorException(ErrResources.uncloneable_cloned, nameof(ReflectionClass));
    }
}
