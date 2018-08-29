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
    public class ReflectionClass : Reflector
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

        public virtual void __construct(Context ctx, PhpValue @class)
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
        /// Gets the constructor of the class.
        /// </summary>
        /// <returns>A <see cref="ReflectionMethod"/> object reflecting the class' constructor,
        /// or NULL if the class has no constructor.</returns>
        public ReflectionMethod getConstructor()
        {
            var routine = _tinfo.RuntimeMethods[Pchp.Core.Reflection.ReflectionUtils.PhpConstructorName];
            return (routine != null)
                ? new ReflectionMethod(_tinfo, routine)
                : null;
        }

        public PhpArray getDefaultProperties() { throw new NotImplementedException(); }
        [return: CastToFalse]
        public string getDocComment() => null;
        public int getEndLine() { throw new NotImplementedException(); }
        //public ReflectionExtension getExtension() { throw new NotImplementedException(); }
        public string getExtensionName() => _tinfo.Extensions.FirstOrDefault() ?? string.Empty;

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
                result.Add(t.GetPhpTypeInfo().Name);
            }

            return result;
        }
        public PhpArray getInterfaces()
        {
            var result = new PhpArray();

            foreach (var t in _tinfo.Type.ImplementedInterfaces)
            {
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
            return (routine != null)
                ? new ReflectionMethod(_tinfo, routine)
                : throw new ReflectionException();
        }

        public PhpArray getMethods(long filter = -1)
        {
            var result = new PhpArray();

            foreach (var routine in _tinfo.RuntimeMethods)
            {
                var rmethod = new ReflectionMethod(_tinfo, routine);
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
        public virtual PhpArray getProperties(int filter)
        {
            var result = new PhpArray(8);
            foreach (var p in _tinfo.GetDeclaredProperties())
            {
                var pinfo = new ReflectionProperty(p);
                if (filter == 0 || ((int)pinfo.getModifiers() | filter) != 0)
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
        public int getStartLine() { throw new NotImplementedException(); }
        public PhpArray getStaticProperties() { throw new NotImplementedException(); }
        public PhpValue getStaticPropertyValue(string name) { throw new NotImplementedException(); }
        public PhpValue getStaticPropertyValue(string name, PhpAlias def_value) { throw new NotImplementedException(); }
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
        public bool hasConstant(string name) { throw new NotImplementedException(); }
        public bool hasMethod(string name) => _tinfo.RuntimeMethods[name] != null;
        public bool hasProperty(string name) { throw new NotImplementedException(); }
        public bool implementsInterface(string @interface) { throw new NotImplementedException(); }
        public bool inNamespace() => this.name.IndexOf(ReflectionUtils.NameSeparator) >= 0;
        public bool isAbstract() => _tinfo.Type.IsAbstract;
        public bool isAnonymous() => _tinfo.Type.IsSealed && _tinfo.Type.IsNotPublic && _tinfo.Type.Name.StartsWith("class@anonymous", StringComparison.Ordinal); // internal sealed 'class@anonymous...' {}
        public bool isCloneable() { throw new NotImplementedException(); }
        public bool isFinal() => _tinfo.Type.IsSealed;
        public bool isInstance(object @object) { throw new NotImplementedException(); }
        public bool isInstantiable() => !object.ReferenceEquals(_tinfo.Creator, PhpTypeInfo.InaccessibleCreator);
        public bool isInterface() => _tinfo.IsInterface;
        public bool isInternal() => !isUserDefined();
        public bool isIterateable() => _tinfo.Type.IsSubclassOf(typeof(Iterator)) || _tinfo.Type.IsSubclassOf(typeof(IteratorAggregate)) || _tinfo.Type.IsSubclassOf(typeof(System.Collections.IEnumerable));

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
        public bool isUserDefined() => _tinfo.IsUserType;
        public object newInstance(Context ctx, params PhpValue[] args) => _tinfo.Creator(ctx, args);
        public object newInstanceArgs(Context ctx, PhpArray args) => newInstance(ctx, args.GetValues());
        public object newInstanceWithoutConstructor(Context ctx) => _tinfo.GetUninitializedInstance(ctx);
        public void setStaticPropertyValue(string name, PhpValue value) { throw new NotImplementedException(); }

        #region Reflector

        public static string export(PhpValue argument, bool @return = false)
        {
            throw new NotImplementedException();
        }

        public string __toString()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
