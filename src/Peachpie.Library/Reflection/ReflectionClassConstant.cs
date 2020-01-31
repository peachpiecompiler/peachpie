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

            if (_pinfo.IsPublic) flags |= ReflectionMethod.IS_PUBLIC;
            if (_pinfo.IsProtected) flags |= ReflectionMethod.IS_PROTECTED;
            if (_pinfo.IsPrivate) flags |= ReflectionMethod.IS_PRIVATE;

            //
            return flags;
        }

        [return: NotNull]
        public string getName() => _pinfo.PropertyName;

        public string __toString()
        {
            throw new NotImplementedException();
        }

        public PhpValue getValue(Context ctx) => _pinfo.GetValue(ctx, null);

        public bool isPrivate() => _pinfo.IsPrivate;

        public bool isProtected() => _pinfo.IsProtected;

        public bool isPublic() => _pinfo.IsPublic;

        public static string export(PhpValue @class, string name, bool @return = false) { throw new NotImplementedException(); }
    }

}
