using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Reflection
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
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

        /// <summary>
        /// The declaring type.
        /// Cannot be <c>null</c>.
        /// </summary>
        [PhpHidden]
        internal protected PhpTypeInfo _tinfo;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected ReflectionMethod() { }

        internal ReflectionMethod(RoutineInfo routine)
        {
            _routine = routine ?? throw new ArgumentNullException(nameof(routine));
            _tinfo = routine.DeclaringType ?? throw new ArgumentException();
        }

        internal ReflectionMethod(PhpTypeInfo tinfo, RoutineInfo routine)
        {
            Debug.Assert(tinfo != null);
            Debug.Assert(routine != null);

            _tinfo = tinfo;
            _routine = routine;
        }

        public ReflectionMethod(Context ctx, string class_method) => __construct(ctx, class_method);

        public ReflectionMethod(Context ctx, PhpValue @class, string name) => __construct(ctx, @class, name);

        public void __construct(Context ctx, PhpValue @class, string name)
        {
            if (name != null)
            {
                _tinfo = ReflectionUtils.ResolvePhpTypeInfo(ctx, @class);
                _routine = _tinfo.RuntimeMethods[name] ?? throw new ReflectionException(string.Format(Resources.Resources.method_does_not_exist, _tinfo.Name, name));
            }
            else if (@class.IsString(out var class_method))
            {
                __construct(ctx, class_method);
            }
            else
            {
                throw new ReflectionException(string.Format("Invalid method name '{0}'", @class.ToString(ctx)));
            }

            // get the real declaring type from routine:
            _tinfo = _routine.DeclaringType ?? _tinfo;
        }

        public void __construct(Context ctx, string class_method)
        {
            if (class_method != null)
            {
                var col = class_method.IndexOf("::", StringComparison.Ordinal);
                if (col > 0)
                {
                    var @class = class_method.Remove(col);      // class name
                    var name = class_method.Substring(col + 2); // method namne

                    _tinfo = ctx.GetDeclaredTypeOrThrow(@class, true);
                    _routine = _tinfo.RuntimeMethods[name] ?? throw new ReflectionException(string.Format(Resources.Resources.method_does_not_exist, _tinfo.Name, name));

                    // ok
                    return;
                }
            }

            throw new ReflectionException("Invalid method name");
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
        public override string getFileName(Context ctx)
        {
            var path = _tinfo.RelativePath;

            return path != null
                ? Path.GetFullPath(Path.Combine(ctx.RootPath, path))
                : null;
        }
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

        /// <summary>
        /// Gets the method prototype (if there is one).
        /// </summary>
        /// <returns><see cref="ReflectionMethod"/> of the prototype.</returns>
        /// <exception cref="ReflectionException">If the method does not have a prototype.</exception>
        [return: NotNull]
        public ReflectionMethod getPrototype()
        {
            // NOTE: not the same as _routine.Methods[0].GetBaseDefinition()

            if (!_tinfo.IsInterface)
            {
                // first look in interfaces:
                foreach (var t in _tinfo.Type.ImplementedInterfaces)
                {
                    var r = t.GetPhpTypeInfo().RuntimeMethods[name];
                    if (r != null)
                    {
                        return new ReflectionMethod(r);
                    }
                }
            }

            // then base classes:
            RoutineInfo prototype = null;
            for (var t = _tinfo.BaseType; t != null; t = t.BaseType)
            {
                var r = t.RuntimeMethods[name];
                if (r != null)
                {
                    prototype = r;
                }
            }

            return prototype != null && prototype.DeclaringType != _tinfo
                ? new ReflectionMethod(prototype)
                : throw new ReflectionException(string.Format(Resources.Resources.method_doesnt_have_prototype, _tinfo.Name, name));
        }

        public PhpValue invoke(Context ctx, object @object, params PhpValue[] args) => _routine.Invoke(ctx, @object, args);
        public PhpValue invokeArgs(Context ctx, object @object, PhpArray args) => _routine.Invoke(ctx, @object, args.GetValues());
        public bool isAbstract() => _routine.Methods.Any(m => m.IsAbstract);
        public bool isConstructor() => name.EqualsOrdinalIgnoreCase(Core.Reflection.ReflectionUtils.PhpConstructorName);
        public bool isDestructor() => name.EqualsOrdinalIgnoreCase(Core.Reflection.ReflectionUtils.PhpDestructorName);
        public bool isFinal() => _routine.Methods.All(m => m.IsFinal);
        public bool isPrivate() => _routine.Methods.All(m => m.IsPrivate);
        public bool isProtected() => _routine.Methods.All(m => m.IsFamily);
        public bool isPublic() => _routine.Methods.Any(m => m.IsPublic);
        public bool isStatic() => _routine.Methods.Any(m => m.IsStatic);
        public void setAccessible(bool accessible)
        {
            // silently ignore,
            // invoke() does not check accessibility
        }
        public override bool hasReturnType()
        {
            if (base.hasReturnType())
            {
                // magic methods that can't have declared return type:
                if (_routine.Name != "__construct" &&
                    _routine.Name != "__destruct")
                {
                    return true;
                }
            }

            //
            return false;
        }
    }
}
