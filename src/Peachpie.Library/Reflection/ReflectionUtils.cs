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

            var classname = @class.ToStringOrNull();
            if (classname != null)
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
        public static PhpValue ResolveDefaultValueAttribute(this DefaultValueAttribute attr)
        {
            if (attr.SerializedValue != null && attr.SerializedValue.Length != 0)
            {
                using (var stream = new MemoryStream(attr.SerializedValue))
                {
                    var reader = new PhpSerialization.PhpSerializer.ObjectReader(null, Encoding.UTF8, stream, default);
                    return reader.Deserialize();
                }
            }
            else
            {
                switch (attr.Type)
                {
                    case DefaultValueAttribute.DefaultValueType.PhpArray:
                        return PhpArray.NewEmpty();
                    default:
                        Debug.Fail("Unexpected data.");
                        return default;
                }
            }
        }

        public static List<ReflectionParameter> ResolveReflectionParameters(ReflectionFunctionAbstract function, MethodInfo[] overloads)
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
                    var isVariadic = p.GetCustomAttribute<ParamArrayAttribute>() != null;

                    PhpValue defaultValue;
                    DefaultValueAttribute defaultValueAttr;

                    if (p.HasDefaultValue)
                    {
                        defaultValue = PhpValue.FromClr(p.RawDefaultValue);
                    }
                    else if ((defaultValueAttr = p.GetCustomAttribute<DefaultValueAttribute>()) != null)
                    {
                        defaultValue = ResolveDefaultValueAttribute(defaultValueAttr);
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
                            if (defaultValue.IsDefault) // optional parameter has not specified default value, set void so it is treated as optional
                            {
                                defaultValue = PhpValue.Void;
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
    }
}
