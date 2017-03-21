using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Library.Reflection
{
    [PhpType("[name]"), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionProperty : Reflector
    {
        /// <summary>
        /// Indicates static properties.
        /// </summary>
        public const int IS_STATIC = 1;

        /// <summary>
        /// Indicates public properties.
        /// </summary>
        public const int IS_PUBLIC = 256;

        /// <summary>
        /// Indicates protected properties.
        /// </summary>
        public const int IS_PROTECTED = 512;

        /// <summary>
        /// Indicates private properties.
        /// </summary>
        public const int IS_PRIVATE = 1024;

        #region Fields & Properties

        /// <summary>
        /// Name of the property.
        /// </summary>
        public string name
        {
            get
            {
                return _pinfo.PropertyName;
            }
            //set
            //{
            //    // Read-only, throws ReflectionException in attempt to write.
            //    throw new ReflectionException(); // TODO: message
            //}
        }

        /// <summary>
        /// Name of the class where the property is defined.
        /// </summary>
        public string @class
        {
            get
            {
                return _pinfo.ContainingType.Name;
            }
            //set
            //{
            //    // Read-only, throws ReflectionException in attempt to write.
            //    throw new ReflectionException(); // TODO: message
            //}
        }

        /// <summary>
        /// Runtime property information.
        /// Cannot be <c>null</c>.
        /// </summary>
        PhpPropertyInfo _pinfo;

        #endregion

        #region Reflector

        public string __toString()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Construction
        
        internal ReflectionProperty(PhpPropertyInfo pinfo)
        {
            Debug.Assert(pinfo != null);
            _pinfo = pinfo;
        }

        [PhpFieldsOnlyCtor]
        protected ReflectionProperty() { }

        public ReflectionProperty(Context ctx, PhpValue @class, string name)
        {
            __construct(ctx, @class, name);
        }

        #endregion

        //void __clone() { throw new NotImplementedException(); }
        public void __construct(Context ctx, PhpValue @class, string name)
        {
            var classname = @class.ToStringOrNull();
            if (classname != null)
            {
                var tinfo = ctx.GetDeclaredTypeOrThrow(classname, true);
                if (tinfo != null)
                {
                    _pinfo = tinfo.GetDeclaredProperty(name);
                }
            }

            if (_pinfo == null)
            {
                throw new ReflectionException();
            }
        }
        public static string export(PhpValue @class, string name, bool @return = false) { throw new NotImplementedException(); }
        public virtual ReflectionClass getDeclaringClass() => new ReflectionClass(_pinfo.ContainingType);
        public virtual string getDocComment() { throw new NotImplementedException(); }
        public virtual long getModifiers()
        {
            long flags = 0;

            if (isStatic()) flags |= IS_STATIC;

            if (isPublic()) flags |= IS_PUBLIC;
            else if (isProtected()) flags |= IS_PROTECTED;
            else if (isPrivate()) flags |= IS_PRIVATE;

            return flags;
        }
        public virtual string getName() => name;
        public virtual PhpValue getValue(Context ctx, object @object) => _pinfo.GetValue(ctx, @object);
        public virtual bool isDefault() => !_pinfo.IsRuntimeProperty;
        public virtual bool isPrivate() => _pinfo.IsPrivate;
        public virtual bool isProtected() => _pinfo.IsProtected;
        public virtual bool isPublic() => _pinfo.IsPublic;
        public virtual bool isStatic() => _pinfo.IsStatic;
        public virtual void setAccessible(bool accessible) { throw new NotImplementedException(); }
        public virtual void setValue(Context ctx, object @object, PhpValue value) { _pinfo.SetValue(ctx, @object, value); }
    }
}
