using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Dynamic;

namespace Pchp.Core.Reflection
{
    public static class ReflectionUtils
    {
        /// <summary>
        /// Well-known name of the PHP constructor.
        /// </summary>
        public const string PhpConstructorName = "__construct";

        /// <summary>
        /// Well-known name of the PHP destructor.
        /// </summary>
        public const string PhpDestructorName = "__destruct";

        /// <summary>
        /// Special name of public static method representing PHP's global code.
        /// </summary>
        public const string GlobalCodeMethodName = "<Main>";

        /// <summary>
        /// Well known assembly token key of Peachpie assemblies.
        /// </summary>
        public const string PeachpieAssemblyTokenKey = "5b4bee2bf1f98593";

        readonly static char[] _disallowedNameChars = new char[] { '`', '<', '>', '.', '\'', '"', '#', '!', '?', '$', '-' };

        /// <summary>
        /// Determines whether given name is valid PHP field, function or class name.
        /// </summary>
        public static bool IsAllowedPhpName(string name) => name != null && name.IndexOfAny(_disallowedNameChars) < 0;

        /// <summary>
        /// Gets value indicating the member has been marked as hidden in PHP context.
        /// </summary>
        public static bool IsPhpHidden(this MemberInfo m) => m.GetCustomAttribute<PhpHiddenAttribute>() != null; // TODO: PhpConditional

        /// <summary>
        /// Checks the fields represents special PHP runtime fields.
        /// </summary>
        public static bool IsRuntimeFields(FieldInfo fld)
        {
            // TODO: [CompilerGenerated] attribute
            // internal PhpArray <runtime_fields>;
            return
                (fld.Name == "__peach__runtimeFields" || fld.Name == "<runtime_fields>") &&
                !fld.IsPublic && !fld.IsStatic && fld.FieldType == typeof(PhpArray);
        }

        /// <summary>
        /// Checks if the field represents special PHP context holder.
        /// </summary>
        public static bool IsContextField(FieldInfo fld)
        {
            // TODO: [CompilerGenerated] attribute
            // protected Context _ctx|<ctx>;
            return !fld.IsStatic &&
                (fld.Attributes & FieldAttributes.Family) != 0 &&
                (fld.Name == "_ctx" || fld.Name == "<ctx>") &&
                fld.FieldType == typeof(Context);
        }

        /// <summary>
        /// Determines whether given constructor is <c>PhpFieldsOnlyCtorAttribute</c>.
        /// </summary>
        public static bool IsPhpFieldsOnlyCtor(this ConstructorInfo ctor)
        {
            return ctor.IsFamilyOrAssembly && !ctor.IsStatic && ctor.GetCustomAttribute<PhpFieldsOnlyCtorAttribute>() != null;
        }

        /// <summary>
        /// Gets value indicating the given type is a type of a class instance excluding builtin PHP value types.
        /// </summary>
        public static bool IsPhpClassType(Type t)
        {
            Debug.Assert(t != null);
            Debug.Assert(t != typeof(PhpAlias));

            return !t.IsValueType && !typeof(PhpArray).IsAssignableFrom(t) && t != typeof(string) && t != typeof(IPhpCallable);
        }

        /// <summary>
        /// Determines if the given type represents a compiled PHP's trait.
        /// </summary>
        /// <param name="t">Type, cannot be <c>null</c>.</param>
        public static bool IsTraitType(Type t)
        {
            // [PhpTraitAttribute]
            // public sealed class T { ... }

            return
                t.IsSealed &&
                t.IsGenericType &&
                t.GetCustomAttribute<PhpTraitAttribute>(false) != null;
        }

        /// <summary>
        /// Determines the type is not interface nor abstract.
        /// </summary>
        public static bool IsInstantiable(Type t) => t != null && !t.IsInterface && !t.IsAbstract; // => not static

        /// <summary>
        /// Determines whether given parameter allows <c>NULL</c> as the argument value.
        /// </summary>
        public static bool IsNullable(this ParameterInfo p)
        {
            Debug.Assert(typeof(PhpArray).IsValueType == false); // see TODO below

            if (p.ParameterType.IsValueType &&
                p.ParameterType != typeof(PhpValue) &&
                //p.ParameterType != typeof(PhpArray) // TODO: uncomment when PhpArray will be struct
                p.ParameterType != typeof(PhpString))
            {
                if (p.ParameterType.IsNullable_T(out var _))
                {
                    return true;
                }

                // NULL is not possible on value types
                return false;
            }
            else
            {
                // NULL is explicitly disallowed?
                return p.GetCustomAttribute<NotNullAttribute>() == null;
            }
        }

        /// <summary>
        /// Types that we do not expose in reflection.
        /// </summary>
        static readonly HashSet<Type> s_hiddenTypes = new HashSet<Type>()
        {
            typeof(object),
            typeof(IPhpCallable),
            typeof(IDisposable),
            typeof(PhpResource),
            typeof(System.Exception),
            typeof(System.Dynamic.IDynamicMetaObjectProvider),
            typeof(IPhpArray),
            typeof(IPhpConvertible),
            typeof(IPhpPrintable),
            typeof(IPhpComparable),
            typeof(IEnumerable),
            typeof(IEnumerable<>),
        };

        /// <summary>
        /// Determines if given type is not visible to PHP runtime.
        /// We implement these types implicitly in compile time, so we should ignore them at proper places.
        /// </summary>
        public static bool IsHiddenType(this Type t) => s_hiddenTypes.Contains(t) || (t.IsConstructedGenericType && s_hiddenTypes.Contains(t.GetGenericTypeDefinition()));

        /// <summary>
        /// Determines the parameter is considered as implicitly passed by runtime.
        /// </summary>
        public static bool IsImplicitParameter(ParameterInfo p) => BinderHelpers.IsImplicitParameter(p);

        /// <summary>
        /// Gets count of implicit parameters.
        /// Such parameters are passed by runtime automatically and not read from given arguments.
        /// </summary>
        public static int ImplicitParametersCount(ParameterInfo[] ps) => ps.TakeWhile(IsImplicitParameter).Count();

        /// <summary>
        /// Gets <see cref="ScriptAttribute"/> of given script type (the type that represents a compiled script file).
        /// </summary>
        /// <returns>The attribute or <c>null</c>.</returns>
        public static ScriptAttribute GetScriptAttribute(Type scriptType)
        {
            var attrs = scriptType.GetCustomAttributes(typeof(ScriptAttribute), inherit: false) as Attribute[]; // faster
            return attrs != null && attrs.Length != 0
                ? (ScriptAttribute)attrs[0]
                : null;
        }

        /// <summary>
        /// Gets types of parameters of given method
        /// </summary>
        public static Type[] GetParametersType(this MethodBase method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var ps = method.GetParameters();
            if (ps.Length == 0)
            {
                return Array.Empty<Type>();
            }

            //
            var types = new Type[ps.Length];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = ps[i].ParameterType;
            }
            return types;
        }

        /// <summary>
        /// Determines if the routine is entirely public.
        /// </summary>
        public static bool IsPublic(this RoutineInfo/*!*/routine)
        {
            var methods = routine.Methods;
            for (int i = 0; i < methods.Length; i++)
            {
                if (!methods[i].IsPublic) return false;
            }
            return true;    // all methods are public
        }

        public static bool IsPhpPublic(this MemberInfo m)
        {
            if (m is FieldInfo f) return f.IsPublic;
            if (m is PropertyInfo p) return IsPhpPublic(p.GetMethod);
            if (m is MethodBase method) return method.IsPublic;

            return false;
        }

        public static bool IsPhpProtected(this MemberInfo m)
        {
            if (m is FieldInfo f) return f.IsAssembly || f.IsFamily || f.IsFamilyAndAssembly || f.IsFamilyOrAssembly;
            if (m is MethodBase method) return method.IsAssembly || method.IsFamily || method.IsFamilyAndAssembly || method.IsFamilyOrAssembly;
            if (m is PropertyInfo p) return IsPhpProtected(p.GetMethod);

            return false;
        }

        public static bool IsPhpPrivate(this MemberInfo m)
        {
            if (m is FieldInfo f) return f.IsPrivate;
            if (m is MethodBase method) return method.IsPrivate;
            if (m is PropertyInfo p) return IsPhpProtected(p.GetMethod);

            return true;
        }
    }
}
