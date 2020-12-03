using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Dynamic;

namespace Pchp.Core.Reflection
{
    public static partial class ReflectionUtils
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
        public static string PeachpieAssemblyTokenKey => _lazyPeachpieAssemblyTokenKey ??= GetPublicKeyTokenString(typeof(Context).Assembly);
        static string _lazyPeachpieAssemblyTokenKey;

        /// <summary>
        /// Gets assembly public key token as string.
        /// </summary>
        public static string GetPublicKeyTokenString(this Assembly assembly)
        {
            if (assembly != null)
            {
                var token = assembly.GetName().GetPublicKeyToken();
                if (token != null)
                {
                    return Utilities.StringUtils.BinToHex(token);
                }
            }

            return null;
        }

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
        /// Resolves lazy constant field in form of:<br/>
        /// public static readonly Func&lt;Context, TResult&gt; FIELD;
        /// </summary>
        internal static bool TryBindLazyConstantField(FieldInfo fld, out Func<Context, PhpValue> getter)
        {
            if (fld.IsInitOnly && fld.IsStatic)
            {
                var rtype = fld.FieldType;
                if (rtype.IsGenericType && rtype.GetGenericTypeDefinition() == typeof(Func<,>))
                {
                    // Func<Context, TResult>
                    var g = rtype.GenericTypeArguments;
                    if (g.Length == 2 && g[0] == typeof(Context))
                    {
                        var getter1 = (MulticastDelegate)fld.GetValue(null); // initonly

                        getter = BinderHelpers.BindFuncInvoke<PhpValue>(getter1);
                        return true;
                    }
                }
            }

            getter = null;
            return false;
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
        /// Determines whether the method is declared in user's PHP code (within a user type or within a source script).
        /// </summary>
        public static bool IsUserRoutine(this MethodBase method)
        {
            Debug.Assert(method != null);

            var type = method.DeclaringType;
            if (type != null)
            {
                var phptype = type.GetCustomAttribute<PhpTypeAttribute>();
                if (phptype != null)
                {
                    return phptype.FileName != null;
                }

                var script = type.GetCustomAttribute<ScriptAttribute>();
                if (script != null)
                {
                    return script.Path != null; // always true
                }
            }

            return false;
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
