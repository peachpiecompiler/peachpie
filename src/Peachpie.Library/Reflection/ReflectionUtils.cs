using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Core.Resources;
using Peachpie.Runtime.Reflection;

namespace Pchp.Library.Reflection
{
    internal static class ReflectionUtils
    {
        public const char NameSeparator = '\\';

        public const string ExtensionName = "Reflection";

        /// <summary>
        /// Resolves type of <paramref name="class"/>.
        /// </summary>
        /// <param name="ctx">Context.</param>
        /// <param name="class">Either string or class instance. Otherwise an exception is thrown.</param>
        /// <returns>Type info. Cannot get <c>null</c>.</returns>
        /// <exception cref="ReflectionException">In case <paramref name="class"/> does not exist or <paramref name="class"/> is not a string or object.</exception>
        public static PhpTypeInfo ResolvePhpTypeInfo(Context ctx, PhpValue @class)
        {
            object instance;

            if (@class.IsString(out var classname))
            {
                return ctx.GetDeclaredType(classname, true)
                    ?? throw new ReflectionException(string.Format(Resources.Resources.class_does_not_exist, classname));
            }
            else if ((instance = @class.AsObject()) != null)
            {
                return instance.GetPhpTypeInfo();
            }
            else
            {
                throw new ReflectionException(string.Format(ErrResources.invalid_argument_type, nameof(@class), "string or object"));
            }
        }

        /// <summary>
        /// TODO: move to Peachpie.Runtime
        /// Creates PhpValue from this attribute.
        /// </summary>
        public static PhpValue ResolveDefaultValueAttribute(this DefaultValueAttribute attr, Context ctx, Type containingType)
        {
            if (attr == null)
            {
                return default;
            }

            // special case, empty array:
            if (attr.FieldName == "Empty" && attr.ExplicitType == typeof(PhpArray))
            {
                // NOTE: make sure the value will be copied when accessed!
                return PhpArray.Empty;
            }

            // resolve declaring type (bind trait definitions)
            var fieldcontainer = attr.ExplicitType ?? containingType;

            if (Core.Reflection.ReflectionUtils.IsTraitType(fieldcontainer) && !fieldcontainer.IsConstructedGenericType)
            {
                // construct something! T<object>
                // NOTE: "self::class" will refer to "System.Object"
                fieldcontainer = fieldcontainer.MakeGenericType(typeof(object));
            }

            //
            var field = fieldcontainer.GetField(attr.FieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField);
            if (field != null)
            {
                Debug.Assert(field.IsStatic);
                var value = field.GetValue(null);
                if (value is Func<Context, PhpValue> func)
                {
                    return func(ctx);
                }
                else
                {
                    return PhpValue.FromClr(value);
                }
            }
            else
            {
                Debug.Fail($"Backing field {attr.FieldName} for parameter default value not found.");
                return default;
            }
        }

        public static List<ParameterInfo> ResolvePhpParameters(MethodInfo[] overloads)
        {
            var parameters = new List<ParameterInfo>();

            for (int mi = 0; mi < overloads.Length; mi++)
            {
                var ps = overloads[mi].GetParameters();
                var implicitps = Core.Reflection.ReflectionUtils.ImplicitParametersCount(ps);   // number of implicit compiler-generated parameters
                int pi = implicitps;

                for (; pi < ps.Length; pi++)
                {
                    var p = ps[pi];

                    if (!Core.Reflection.ReflectionUtils.IsAllowedPhpName(p.Name))
                    {
                        break;  // synthesized at the end of CLR method
                    }

                    var index = pi - implicitps;

                    if (index == parameters.Count)
                    {
                        parameters.Add(p);
                    }
                    else
                    {
                        // choose the better - the one with more metadata
                        var oldp = parameters[index];
                        if (p.HasDefaultValue || p.GetCustomAttribute<DefaultValueAttribute>() != null) // TODO: or has type information
                        {
                            parameters[index] = p;
                        }
                    }
                }
            }

            //
            return parameters;
        }

        public static List<ReflectionParameter> ResolveReflectionParameters(Context ctx, ReflectionFunctionAbstract function, MethodInfo[] overloads)
        {
            var parameters = new List<ReflectionParameter>();

            for (int mi = 0; mi < overloads.Length; mi++)
            {
                var ps = overloads[mi].GetParameters();
                var implicitps = Core.Reflection.ReflectionUtils.ImplicitParametersCount(ps);   // number of implicit compiler-generated parameters
                int pi = implicitps;

                for (; pi < ps.Length; pi++)
                {
                    var p = ps[pi];

                    var allowsNull = p.IsNullable();
                    var isVariadic = pi == ps.Length - 1 && p.GetCustomAttribute<ParamArrayAttribute>() != null;

                    PhpValue? defaultValue;
                    DefaultValueAttribute defaultValueAttr;

                    if (p.HasDefaultValue)
                    {
                        defaultValue = PhpValue.FromClr(p.RawDefaultValue);
                    }
                    else if ((defaultValueAttr = p.GetCustomAttribute<DefaultValueAttribute>()) != null)
                    {
                        defaultValue = defaultValueAttr.ResolveDefaultValueAttribute(ctx, overloads[mi].DeclaringType);
                    }
                    else
                    {
                        defaultValue = default;
                    }

                    int index = pi - implicitps;
                    if (index == parameters.Count)
                    {
                        if (mi != 0) // we are adding and optional parameter!
                        {
                            if (!defaultValue.HasValue)
                            {
                                // optional parameter has not specified default value, set void so it is treated as optional
                                defaultValue = PhpValue.Null;
                            }
                        }

                        parameters.Add(new ReflectionParameter(function, index, p.ParameterType, allowsNull, isVariadic, p.Name, defaultValue));
                    }
                    else
                    {
                        // update existing
                        Debug.Assert(index < parameters.Count);
                        parameters[index].AddOverload(p.ParameterType, allowsNull, isVariadic, p.Name, defaultValue);
                    }
                }

                // remaining parameters have to be marked as optional
                for (var index = pi - implicitps; index < parameters.Count; index++)
                {
                    parameters[index].SetOptional();
                }
            }

            //
            return parameters;
        }

        public static string getDocComment(Assembly assembly, string symbolId)
        {
            var metadata = MetadataResourceManager.GetMetadata(assembly, symbolId);
            return getDocComment(metadata);
        }

        public static string getDocComment(MethodInfo method) => getDocComment(method.DeclaringType.Assembly, method.DeclaringType.FullName + "." + method.Name);

        public static string getDocComment(TypeInfo type) => getDocComment(type.Assembly, type.FullName);

        static string getDocComment(string metadata)
        {
            if (metadata != null)
            {
                var decoded = (stdClass)StringUtils.JsonDecode(metadata).Object;
                if (decoded.GetRuntimeFields().TryGetValue("doc", out var doc))
                {
                    return doc.AsString();
                }
            }

            return null;
        }
    }
}
