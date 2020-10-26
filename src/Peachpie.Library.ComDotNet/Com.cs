using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Pchp.Core;

namespace Peachpie.Library.ComDotNet
{
    /// <summary>
    /// COM object
    /// <para>TODO : implements VARIANT and inherit from it, which would allow returning VARIANT object from __call and __get
    /// and working with it, see test case com_dotnet/com.php</para>
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("com_dotnet")]
    public class COM
    {
        #region Private members

        [PhpHidden]
        internal protected object comObject = null;

        [PhpHidden]
        internal protected const BindingFlags MemberAccessCom =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        #endregion

        #region Construction

        public COM(Context ctx, string module_name)
        {
            __construct(ctx, module_name);
        }

        [PhpFieldsOnlyCtor]
        protected COM()
        {
        }

        public virtual void __construct(Context ctx, string module_name)
        {
            var comType = Type.GetTypeFromProgID(module_name);
            if (comType == null)
                return;

            comObject = Activator.CreateInstance(comType);
        }

        public virtual void __construct(Context ctx, string module_name, string server_name)
        {
            var comType = Type.GetTypeFromProgID(module_name, server_name);
            if (comType == null)
                return;

            comObject = Activator.CreateInstance(comType);
        }

        public virtual void __construct(Context ctx, string module_name, string server_name, int code_page)
        {
            throw new NotSupportedException();
        }

        public virtual void __construct(Context ctx, string module_name, string server_name, int code_page, string typelib)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Magic methods

        /// <summary>
        /// Special field containing runtime fields.
        /// </summary>
        /// <remarks>
        /// The field is handled by runtime and is not intended for direct use.
        /// Magic methods for property access are ignored without runtime fields.
        /// </remarks>
        [CompilerGenerated]
        internal PhpArray __peach__runtimeFields = null;

        public PhpValue __call(string name, PhpArray arguments)
        {
            if (comObject == null)
                return PhpValue.Null;

            var parameters = arguments.GetValues().Select(v => v.ToClr()).ToArray();

            var result = comObject.GetType().InvokeMember(
                name, MemberAccessCom | BindingFlags.InvokeMethod, null, comObject, parameters);

            return PhpValue.FromClr(result);
        }

        public virtual PhpValue __get(string name)
        {
            if (comObject == null)
                return PhpValue.Null;

            var prop = comObject.GetType().InvokeMember(
                name, MemberAccessCom | BindingFlags.GetProperty, null, comObject, null);
            
            return prop != null 
                ? PhpValue.FromClr(prop)
                : PhpValue.Null;
        }

        public virtual bool __set(string name, PhpValue value)
        {
            if (comObject == null)
                return false;

            comObject.GetType().InvokeMember(
                name, MemberAccessCom | BindingFlags.SetProperty, null, comObject, new object[1] { value.ToClr() });

            return true;
        }

        #endregion
    }
}
