using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Reflection
{
    [PhpType("[name]"), PhpExtension(ReflectionUtils.ExtensionName)]
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
        PhpTypeInfo _tinfo;

        #endregion

        #region Construction

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

            _tinfo = ResolvePhpTypeInfo(ctx, @class);

            if (_tinfo == null)
            {
                throw new ArgumentException();  // TODO: ReflectionException
            }
        }

        internal static PhpTypeInfo ResolvePhpTypeInfo(Context ctx, PhpValue @class)
        {
            object instance;

            var classname = @class.ToStringOrNull();
            if (classname != null)
            {
                return ctx.GetDeclaredType(classname, true);
            }
            else if ((instance = @class.AsObject()) != null)
            {
                return instance.GetPhpTypeInfo();
            }
            else
            {
                // argument type exception
            }

            return null;
        }

        #endregion

        public PhpValue getConstant(string name) { throw new NotImplementedException(); }
        public PhpArray getConstants() { throw new NotImplementedException(); }
        //public ReflectionMethod getConstructor() { throw new NotImplementedException(); }
        public PhpArray getDefaultProperties() { throw new NotImplementedException(); }
        public string getDocComment() { throw new NotImplementedException(); }
        public int getEndLine() { throw new NotImplementedException(); }
        //public ReflectionExtension getExtension() { throw new NotImplementedException(); }
        public string getExtensionName() { throw new NotImplementedException(); }
        public string getFileName() { throw new NotImplementedException(); }
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
                result.Add(iinfo.Name, PhpValue.FromClass(new ReflectionClass() { _tinfo = iinfo }));
            }

            return result;
        }
        [return: CastToFalse]
        public ReflectionMethod getMethod(string name)
        {
            var routine = _tinfo.RuntimeMethods[name];
            return (routine != null)
                ? new ReflectionMethod(_tinfo, routine)
                : null;
        }
        public PhpArray getMethods(int filter) { throw new NotImplementedException(); }
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
        public ReflectionClass getParentClass() => (_tinfo.BaseType != null) ? new ReflectionClass() { _tinfo = _tinfo.BaseType } : null;
        public PhpArray getProperties(int filter) { throw new NotImplementedException(); }
        //public ReflectionProperty getProperty(string name) { throw new NotImplementedException(); }
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
        public PhpArray getTraitNames() { throw new NotImplementedException(); }
        public PhpArray getTraits() { throw new NotImplementedException(); }
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
        public bool isInternal() { throw new NotImplementedException(); }
        public bool isIterateable() => _tinfo.Type.IsSubclassOf(typeof(Iterator)) || _tinfo.Type.IsSubclassOf(typeof(IteratorAggregate)) || _tinfo.Type.IsSubclassOf(typeof(System.Collections.IEnumerable));
        public bool isSubclassOf(string @class) { throw new NotImplementedException(); }
        public bool isTrait() => _tinfo.IsTrait;
        public bool isUserDefined() => !isInternal();
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
