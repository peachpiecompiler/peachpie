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
    public class ReflectionMethod : ReflectionFunctionAbstract
    {
        #region Constants

        public const int IS_STATIC = 1;
        public const int IS_ABSTRACT = 2;
        public const int IS_FINAL = 4;
        public const int IS_PUBLIC = 256;
        public const int IS_PROTECTED = 512;
        public const int IS_PRIVATE = 1024;

        #endregion

        #region Fields & Properties

        public string @class
        {
            get
            {
                Debug.Assert(_tinfo != null);
                return _tinfo.Name;
            }
            //set
            //{
            //    // Read-only, throws ReflectionException in attempt to write.
            //    throw new ReflectionException(); // TODO: message
            //}
        }

        internal PhpTypeInfo _tinfo;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected ReflectionMethod() { }

        internal ReflectionMethod(PhpTypeInfo tinfo, RoutineInfo routine)
        {
            Debug.Assert(tinfo != null);
            Debug.Assert(routine != null);

            _tinfo = tinfo;
            _routine = routine;
        }

        public ReflectionMethod(Context ctx, PhpValue @class, string name)
        {
            __construct(ctx, @class, name);
        }

        public void __construct(Context ctx, PhpValue @class, string name)
        {
            var tinfo = ReflectionClass.ResolvePhpTypeInfo(ctx, @class);
            if (tinfo != null)
            {
                _tinfo = tinfo;
                _routine = tinfo.RuntimeMethods[name];
            }

            if (_routine == null)
            {
                throw new ArgumentException(); // TODO: ReflectionException();
            }
        }

        #endregion

        public static string export(string @class, string name, bool @return = false) { throw new NotImplementedException(); }
        public Closure getClosure(object @object)
        {
            //return Operators.BuildClosure(
            //    _routine,
            //    PhpArray.Empty,   // TODO: list of _routine parameters
            //    PhpArray.Empty);  // TODO: fix @object parameter (PhpInvokable(@object) -> PhpCallable)

            throw new NotImplementedException();
        }
        public ReflectionClass getDeclaringClass() => new ReflectionClass(_tinfo);
        public long getModifiers()
        {
            long flags = 0;

            foreach (var m in _routine.Methods)
            {
                if (m.IsStatic) flags |= IS_STATIC;
                if (m.IsFinal) flags |= IS_FINAL;
                if (m.IsAbstract) flags |= IS_ABSTRACT;
                if (m.IsPublic) flags |= IS_PUBLIC;
                if (m.IsFamily) flags |= IS_PROTECTED;
                if (m.IsPrivate) flags |= IS_PRIVATE;
            }

            return flags;
        }
        public ReflectionMethod getPrototype() { throw new NotImplementedException(); }
        public PhpValue invoke(Context ctx, object @object, params PhpValue[] args) => _routine.Invoke(ctx, @object, args);
        public PhpValue invokeArgs(Context ctx, object @object, PhpArray args) => _routine.Invoke(ctx, @object, args.GetValues());
        public bool isAbstract() => _routine.Methods.Any(m => m.IsAbstract);
        public bool isConstructor() { throw new NotImplementedException(); }
        public bool isDestructor() { throw new NotImplementedException(); }
        public bool isFinal() => _routine.Methods.All(m => m.IsFinal);
        public bool isPrivate() { throw new NotImplementedException(); }
        public bool isProtected() { throw new NotImplementedException(); }
        public bool isPublic() => _routine.Methods.Any(m => m.IsPublic);
        public bool isStatic() => _routine.Methods.Any(m => m.IsStatic);
        public void setAccessible(bool accessible) { throw new NotImplementedException(); }
    }
}
