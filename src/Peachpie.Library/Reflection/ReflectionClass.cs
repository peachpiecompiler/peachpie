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

        #region Reflector

        public string __toString()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
