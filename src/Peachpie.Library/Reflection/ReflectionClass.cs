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

        public ReflectionClass(Context ctx, PhpValue obj)
        {
            __construct(ctx, obj);
        }

        public void __construct(Context ctx, PhpValue obj)
        {
            Debug.Assert(_tinfo == null, "Subsequent call not allowed.");

            object instance;

            var classname = obj.ToStringOrNull();
            if (classname != null)
            {
                _tinfo = ctx.GetDeclaredType(classname, true);
            }
            else if ((instance = obj.AsObject()) != null)
            {
                _tinfo = instance.GetPhpTypeInfo();
            }
            else
            {
                // argument type exception
            }

            if (_tinfo == null)
            {
                throw new ArgumentException();  // TODO: ReflectionException
            }
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
        public PhpArray getInterfaceNames() { throw new NotImplementedException(); }
        public PhpArray getInterfaces() { throw new NotImplementedException(); }
        //public ReflectionMethod getMethod(string name) { throw new NotImplementedException(); }
        public PhpArray getMethods(int filter) { throw new NotImplementedException(); }
        public int getModifiers() { throw new NotImplementedException(); }
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
        public bool hasMethod(string name) { throw new NotImplementedException(); }
        public bool hasProperty(string name) { throw new NotImplementedException(); }
        public bool implementsInterface(string @interface) { throw new NotImplementedException(); }
        public bool inNamespace() => this.name.IndexOf(ReflectionUtils.NameSeparator) >= 0;
        public bool isAbstract() => _tinfo.Type.IsAbstract;
        public bool isAnonymous() { throw new NotImplementedException(); }
        public bool isCloneable() { throw new NotImplementedException(); }
        public bool isFinal() => _tinfo.Type.IsSealed;
        public bool isInstance(object @object) { throw new NotImplementedException(); }
        public bool isInstantiable() { throw new NotImplementedException(); }
        public bool isInterface() => _tinfo.IsInterface;
        public bool isInternal() { throw new NotImplementedException(); }
        public bool isIterateable() { throw new NotImplementedException(); }
        public bool isSubclassOf(string @class) { throw new NotImplementedException(); }
        public bool isTrait() { throw new NotImplementedException(); }
        public bool isUserDefined() { throw new NotImplementedException(); }
        public object newInstance(Context ctx, params PhpValue[] args) => _tinfo.Creator(ctx, args);
        public object newInstanceArgs(Context ctx, PhpArray args) => newInstance(ctx, args.GetValues());
        public object newInstanceWithoutConstructor() { throw new NotImplementedException(); }
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
