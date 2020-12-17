using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Pchp.Library.Reflection
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionClassConstant : Reflector
    {
        public string name => _pinfo.PropertyName;

        public string @class => _pinfo.ContainingType.Name;

        /// <summary>
        /// Runtime property information.
        /// Cannot be <c>null</c>.
        /// </summary>
        [PhpHidden]
        PhpPropertyInfo _pinfo;

        #region Constants

        public const int IS_PUBLIC = 1;
        public const int IS_PROTECTED = 2;
        public const int IS_PRIVATE = 4;

        #endregion

        #region Construction

        internal ReflectionClassConstant(PhpPropertyInfo pinfo)
        {
            Debug.Assert(pinfo != null);
            _pinfo = pinfo;
        }

        [PhpFieldsOnlyCtor]
        protected ReflectionClassConstant() { }

        public ReflectionClassConstant(Context ctx, PhpValue @class, string name)
        {
            __construct(ctx, @class, name);
        }

        #endregion

        public void __construct(Context ctx, PhpValue @class, string name)
        {
            var tinfo = ReflectionUtils.ResolvePhpTypeInfo(ctx, @class);

            _pinfo = tinfo.GetDeclaredConstant(name);

            if (_pinfo == null)
            {
                throw new ReflectionException();
            }
        }

        public ReflectionClass getDeclaringClass() => new ReflectionClass(_pinfo.ContainingType);

        [return: CastToFalse]
        public string getDocComment() => ReflectionUtils.getDocComment(_pinfo.ContainingType.Type.Assembly, _pinfo.ContainingType.Type.FullName + "." + _pinfo.PropertyName);

        public int getModifiers()
        {
            int flags = 0;

            if (_pinfo.IsPublic) flags |= IS_PUBLIC;
            if (_pinfo.IsProtected) flags |= IS_PROTECTED;
            if (_pinfo.IsPrivate) flags |= IS_PRIVATE;

            //
            return flags;
        }

        public string getName() => _pinfo.PropertyName;

        public string __toString()
        {
            throw new NotImplementedException();
        }

        public PhpValue getValue(Context ctx) => _pinfo.GetValue(ctx, null);

        public virtual PhpArray getAttributes(string class_name = null, int flags = 0)
            => ReflectionUtils.getAttributes(_pinfo.Member, class_name, flags);

        public bool isPrivate() => _pinfo.IsPrivate;

        public bool isProtected() => _pinfo.IsProtected;

        public bool isPublic() => _pinfo.IsPublic;

        public static string export(PhpValue @class, string name, bool @return = false) { throw new NotImplementedException(); }
    }

}
