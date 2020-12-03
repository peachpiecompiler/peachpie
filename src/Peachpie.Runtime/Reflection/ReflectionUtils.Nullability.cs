using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Pchp.Core.Dynamic;

namespace Pchp.Core.Reflection
{
    public static partial class ReflectionUtils
    {
        [Flags]
        private enum FlowAnalysisAnnotations
        {
            None = 0,
            AllowNull = 1 << 0,
            DisallowNull = 1 << 1,
            MaybeNullWhenTrue = 1 << 2,
            MaybeNullWhenFalse = 1 << 3,
            MaybeNull = MaybeNullWhenTrue | MaybeNullWhenFalse,
            NotNullWhenTrue = 1 << 4,
            NotNullWhenFalse = 1 << 5,
            NotNull = NotNullWhenTrue | NotNullWhenFalse,
            DoesNotReturnIfFalse = 1 << 6,
            DoesNotReturnIfTrue = 1 << 7,
            DoesNotReturn = DoesNotReturnIfTrue | DoesNotReturnIfFalse,
        }

        private enum NullableKind : byte
        {
            Oblivious = 0,
            NotAnnotated = 1,
            Annotated = 2
        }

        /// <summary>
        /// Determines whether given parameter allows <c>NULL</c> as the argument value.
        /// </summary>
        public static bool IsNullable(this ParameterInfo p)
        {
            Debug.Assert(typeof(PhpArray).IsValueType == false); // see TODO below

            if (p.ParameterType.IsValueType &&
                //p.ParameterType != typeof(PhpArray) // TODO: uncomment when PhpArray will be struct
                p.ParameterType != typeof(PhpString))
            {
                // There is currently no way to annotate PhpValue to be non-nullable (although [NullableContext(1)] could confuse that)
                if (p.ParameterType == typeof(PhpValue) || p.ParameterType.IsNullable_T(out var _))
                {
                    return true;
                }

                // NULL is not possible on value types
                return false;
            }
            else
            {
                // NOTE: This code must be kept in sync with the behaviour of AttributeHelpers.IsNotNullable in the compiler

                // C# basic 8.0 Nullability check
                bool isNotNullable;
                if (TryGetNullableAttributeValue(p.CustomAttributes, out byte nullableVal))
                {
                    // [Nullable(1)] disallows null
                    isNotNullable = (nullableVal == (byte)NullableKind.NotAnnotated);
                }
                else
                {
                    // [NullableContext(1)] sets all containing symbols as not nullable by default
                    isNotNullable = (GetNullableContext(p.Member) == (byte)NullableKind.NotAnnotated);
                }

                // Special C# 8.0 attributes for flow static analysis
                var flowAnnotations = DecodeFlowAnalysisAttributes(p.CustomAttributes);
                if (flowAnnotations != FlowAnalysisAnnotations.None)
                {
                    // Specified attributes can override nullability in both directions
                    isNotNullable =
                        isNotNullable
                        ? (flowAnnotations & (FlowAnalysisAnnotations.AllowNull | FlowAnalysisAnnotations.MaybeNull)) == 0
                        : ((flowAnnotations & FlowAnalysisAnnotations.DisallowNull) != 0 || (flowAnnotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull);
                }

                return !isNotNullable;
            }
        }

        private static byte GetNullableContext(MemberInfo member)
        {
            if (TryGetNullableContextAttributeValue(member.CustomAttributes, out byte memberContext))
            {
                // [NullableContext] directly on the member
                return memberContext;
            }
            else
            {
                for (var type = member.DeclaringType; type != null; type = type.DeclaringType)
                {
                    if (TryGetNullableContextAttributeValue(type.CustomAttributes, out byte typeContext))
                    {
                        // [NullableContext] in the hierarchy of containing types
                        return typeContext;
                    }
                }

                // Oblivious by default
                return (byte)NullableKind.Oblivious;
            }
        }

        private static bool TryGetNullableAttributeValue(IEnumerable<CustomAttributeData> attributes, out byte value)
        {
            // The attribute must have the same signature as our NullableAttribute, but can be defined elsewhere
            var nullableAttr = attributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(NullableAttribute).FullName);
            if (nullableAttr?.ConstructorArguments.Count == 1)
            {
                var nullableArg = nullableAttr.ConstructorArguments[0];
                if (nullableArg.ArgumentType == typeof(byte))
                {
                    value = (byte)nullableArg.Value;
                    return true;
                }
                else if (nullableArg.ArgumentType == typeof(byte[]))
                {
                    var args = (ReadOnlyCollection<CustomAttributeTypedArgument>)nullableArg.Value;
                    if (args.Count > 0 && args[0].ArgumentType == typeof(byte))
                    {
                        value = (byte)args[0].Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static bool TryGetNullableContextAttributeValue(IEnumerable<CustomAttributeData> attributes, out byte value)
        {
            // The attribute must have the same signature as our NullableContextAttribute, but can be defined elsewhere
            var nullableAttr = attributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(NullableContextAttribute).FullName);
            if (nullableAttr?.ConstructorArguments.Count == 1)
            {
                var nullableArg = nullableAttr.ConstructorArguments[0];
                if (nullableArg.ArgumentType == typeof(byte))
                {
                    value = (byte)nullableArg.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static FlowAnalysisAnnotations DecodeFlowAnalysisAttributes(IEnumerable<CustomAttributeData> attributes)
        {
            // NOTE: This code must be kept in sync with the behaviour of AttributeHelpers.DecodeFlowAnalysisAttributes in the compiler

            var result = FlowAnalysisAnnotations.None;

            foreach (var attr in attributes)
            {
                switch (attr.AttributeType.FullName)
                {
                    case "System.Diagnostics.CodeAnalysis.AllowNullAttribute":
                        result |= FlowAnalysisAnnotations.AllowNull;
                        break;
                    case "System.Diagnostics.CodeAnalysis.DisallowNullAttribute":
                        result |= FlowAnalysisAnnotations.DisallowNull;
                        break;
                    case "System.Diagnostics.CodeAnalysis.MaybeNullAttribute":
                        result |= FlowAnalysisAnnotations.MaybeNull;
                        break;
                    case "System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute":
                        if (TryGetBoolArgument(attr, out bool maybeNullWhen))
                        {
                            result |= maybeNullWhen ? FlowAnalysisAnnotations.MaybeNullWhenTrue : FlowAnalysisAnnotations.MaybeNullWhenFalse;
                        }
                        break;
                    case "System.Diagnostics.CodeAnalysis.NotNullAttribute":
                        result |= FlowAnalysisAnnotations.AllowNull;
                        break;
                }
            }

            return result;

            static bool TryGetBoolArgument(CustomAttributeData attr, out bool value)
            {
                if (attr.ConstructorArguments.Count == 1)
                {
                    var arg = attr.ConstructorArguments[0];
                    if (arg.ArgumentType == typeof(bool))
                    {
                        value = (bool)arg.Value;
                        return true;
                    }
                }

                value = default;
                return false;
            }
        }
    }
}
