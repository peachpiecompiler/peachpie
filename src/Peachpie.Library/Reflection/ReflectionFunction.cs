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
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionFunction : ReflectionFunctionAbstract
    {
        /// <summary>
        /// Indicates deprecated functions.
        /// </summary>
        public const int IS_DEPRECATED = 0x40000;

        /// <summary>
        /// When constructed from a <see cref="Closure"/>,
        /// the original instance of the closure is here.
        /// Otherwise, this is <c>null</c>.
        /// </summary>
        Closure _closure;

        #region Construction

        [PhpFieldsOnlyCtor]
        protected ReflectionFunction() { }

        internal ReflectionFunction(RoutineInfo routine)
        {
            Debug.Assert(routine != null);
            _routine = routine;
        }

        public ReflectionFunction(Context ctx, PhpValue name)
        {
            __construct(ctx, name);
        }

        public void __construct(Context ctx, PhpValue name)
        {
            Debug.Assert(_routine == null, "Subsequent call not allowed.");

            object instance;
            var str = name.ToStringOrNull();
            if (str != null)
            {
                _closure = null;
                _routine = ctx.GetDeclaredFunction(str) ?? throw new ReflectionException(string.Format(Resources.LibResources.function_does_not_exist, str));
            }
            else if ((instance = name.AsObject()) != null)
            {
                if (instance is Closure closure)
                {
                    _closure = closure;
                    _routine = closure.Callable() as RoutineInfo;
                    // TODO: handle its $this parameter and use parameters
                }
            }

            if (_routine == null)
            {
                throw new ReflectionException();
            }
        }

        #endregion

        public static string export(string name, bool @return = false) { throw new NotImplementedException(); }

        public Closure getClosure(Context ctx) => _closure ?? Operators.BuildClosure(ctx, _routine, null, default, null, PhpArray.Empty, PhpArray.Empty);

        public PhpValue invoke(Context ctx, params PhpValue[] args) => _closure != null ? _closure.__invoke(args) : _routine.PhpCallable(ctx, args);

        public PhpValue invokeArgs(Context ctx, PhpArray args) => invoke(ctx, args.GetValues());

        public bool isDisabled() => false;

        public override bool isClosure() => _closure != null;

        public override object getClosureThis() => _closure?.This();

        public override ReflectionClass getClosureScopeClass() => _closure != null ? new ReflectionClass(_closure.Scope().GetPhpTypeInfo()) : null;

        public override string getFileName(Context ctx)
        {
            var methods = _routine.Methods;
            if (methods.Length != 0 && methods[0].IsStatic)
            {
                var scriptattr = Core.Reflection.ReflectionUtils.GetScriptAttribute(methods[0].DeclaringType);
                if (scriptattr != null)
                {
                    return System.IO.Path.Combine(ctx.RootPath, scriptattr.Path);
                }
            }

            return string.Empty;
        }
    }
}
