using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.Symbols
{
    static class WellKnownMembersHelper
    {
        public static AttributeData CreateCompilerGeneratedAttribute(this PhpCompilation compilation)
        {
            // [CompilerGenerated]
            var compilergenerated = (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor);

            return new SynthesizedAttributeData(
                compilergenerated,
                ImmutableArray<TypedConstant>.Empty,
                ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

        public static AttributeData CreateObsoleteAttribute(this PhpCompilation compilation, PHPDocBlock.DeprecatedTag deprecated)
        {
            if (deprecated == null)
                throw new ArgumentNullException(nameof(deprecated));

            // [ObsoleteAttribute(message, false)]
            return new SynthesizedAttributeData(
                (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_ObsoleteAttribute__ctor),
                    ImmutableArray.Create(
                        compilation.CreateTypedConstant(deprecated.Version/*NOTE:Version contains the message*/),
                        compilation.CreateTypedConstant(false/*isError*/)),
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

        public static AttributeData CreateParamsAttribute(this PhpCompilation compilation)
        {
            return new SynthesizedAttributeData(
                (MethodSymbol)compilation.GetWellKnownTypeMember(WellKnownMember.System_ParamArrayAttribute__ctor),
                ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

        public static AttributeData CreateNotNullAttribute(this PhpCompilation compilation)
        {
            return new SynthesizedAttributeData(
                compilation.CoreMethods.Ctors.NotNullAttribute,
                ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

        //public static AttributeData CreateDefaultValueAttribute(this PhpCompilation compilation, IMethodSymbol method, BoundArrayEx arr)
        //{
        //    var typeParameter = new KeyValuePair<string, TypedConstant>("Type", new TypedConstant(compilation.CoreTypes.DefaultValueType.Symbol, TypedConstantKind.Enum, 1/*PhpArray*/));
        //    var namedparameters = ImmutableArray.Create(typeParameter);

        //    if (arr.Items.Length != 0)
        //    {
        //        try
        //        {
        //            var byteSymbol = compilation.GetSpecialType(SpecialType.System_Byte);
        //            var serializedValue = Encoding.UTF8.GetBytes(arr.PhpSerializeOrThrow());
        //            var p = new KeyValuePair<string, TypedConstant>(
        //                "SerializedValue",
        //                new TypedConstant(compilation.CreateArrayTypeSymbol(byteSymbol), serializedValue.Select(compilation.CreateTypedConstant).AsImmutable()));

        //            namedparameters = namedparameters.Add(p);
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new InvalidOperationException($"Cannot construct serialized parameter default value. Routine '{method.Name}', {ex.Message}.", ex);
        //        }
        //    }

        //    return new SynthesizedAttributeData(
        //        compilation.CoreMethods.Ctors.DefaultValueAttribute,
        //        ImmutableArray<TypedConstant>.Empty, namedparameters);
        //}

        public static AttributeData CreateDefaultValueAttribute(this PhpCompilation compilation, TypeSymbol containingType, FieldSymbol field)
        {
            var namedparameters = ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;

            var fieldContainer = field.ContainingType;

            if (fieldContainer != containingType)
            {
                namedparameters = ImmutableArray.Create(new KeyValuePair<string, TypedConstant>(
                    "ExplicitType",
                    compilation.CreateTypedConstant(fieldContainer)));
            }

            // [DefaultValueAttribute(name) { ExplicitType = ... }]
            return new SynthesizedAttributeData(
                compilation.CoreMethods.Ctors.DefaultValueAttribute_string,
                ImmutableArray.Create(compilation.CreateTypedConstant(field.Name)),
                namedparameters);
        }
    }
}
