using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Cci = Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Emit;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract partial class BaseAttributeData : Cci.ICustomAttribute
    {
        ImmutableArray<Cci.IMetadataExpression> Cci.ICustomAttribute.GetArguments(EmitContext context)
        {
            var commonArgs = this.CommonConstructorArguments;
            if (commonArgs.IsEmpty)
            {
                return ImmutableArray<Cci.IMetadataExpression>.Empty;
            }

            var builder = ArrayBuilder<Cci.IMetadataExpression>.GetInstance();
            foreach (var argument in commonArgs)
            {
                Debug.Assert(argument.Kind != TypedConstantKind.Error);
                builder.Add(CreateMetadataExpression(argument, context));
            }
            return builder.ToImmutableAndFree();
        }

        Cci.IMethodReference Cci.ICustomAttribute.Constructor(EmitContext context, bool reportDiagnostics)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return (Cci.IMethodReference)moduleBeingBuilt.Translate(this.AttributeConstructor, /*context.SyntaxNodeOpt, */context.Diagnostics, false);
        }

        ImmutableArray<Cci.IMetadataNamedArgument> Cci.ICustomAttribute.GetNamedArguments(EmitContext context)
        {
            var commonArgs = this.CommonNamedArguments;
            if (commonArgs.IsEmpty)
            {
                return ImmutableArray<Cci.IMetadataNamedArgument>.Empty;
            }

            var builder = ArrayBuilder<Cci.IMetadataNamedArgument>.GetInstance();
            foreach (var namedArgument in commonArgs)
            {
                builder.Add(CreateMetadataNamedArgument(namedArgument.Key, namedArgument.Value, context));
            }
            return builder.ToImmutableAndFree();
        }

        int Cci.ICustomAttribute.ArgumentCount
        {
            get
            {
                return this.CommonConstructorArguments.Length;
            }
        }

        ushort Cci.ICustomAttribute.NamedArgumentCount
        {
            get
            {
                return (ushort)this.CommonNamedArguments.Length;
            }
        }

        Cci.ITypeReference Cci.ICustomAttribute.GetType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.Translate(this.AttributeClass, syntaxNodeOpt: context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        bool Cci.ICustomAttribute.AllowMultiple
        {
            get { return false; } //get { return this.AttributeClass.GetAttributeUsageInfo().AllowMultiple; }
        }

        private Cci.IMetadataExpression CreateMetadataExpression(TypedConstant argument, EmitContext context)
        {
            if (argument.IsNull)
            {
                return CreateMetadataConstant(argument.Type, null, context);
            }

            switch (argument.Kind)
            {
                case TypedConstantKind.Array:
                    return CreateMetadataArray(argument, context);

                case TypedConstantKind.Type:
                    return CreateType(argument, context);

                default:
                    return CreateMetadataConstant(argument.Type, argument.Value, context);
            }
        }

        private MetadataCreateArray CreateMetadataArray(TypedConstant argument, EmitContext context)
        {
            Debug.Assert(!argument.Values.IsDefault);
            var values = argument.Values;
            var arrayType = Emit.PEModuleBuilder.Translate((ArrayTypeSymbol)argument.Type);

            if (values.Length == 0)
            {
                return new MetadataCreateArray(arrayType,
                                               arrayType.GetElementType(context),
                                               ImmutableArray<Cci.IMetadataExpression>.Empty);
            }

            var metadataExprs = new Cci.IMetadataExpression[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                metadataExprs[i] = CreateMetadataExpression(values[i], context);
            }

            return new MetadataCreateArray(arrayType,
                                           arrayType.GetElementType(context),
                                           metadataExprs.AsImmutableOrNull());
        }

        private static MetadataTypeOf CreateType(TypedConstant argument, EmitContext context)
        {
            Debug.Assert(argument.Value != null);
            var moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var syntaxNodeOpt = (SyntaxNode)context.SyntaxNodeOpt;
            var diagnostics = context.Diagnostics;
            return new MetadataTypeOf(moduleBeingBuilt.Translate((TypeSymbol)argument.Value, syntaxNodeOpt, diagnostics),
                                      moduleBeingBuilt.Translate((TypeSymbol)argument.Type, syntaxNodeOpt, diagnostics));
        }

        private static MetadataConstant CreateMetadataConstant(ITypeSymbol type, object value, EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.CreateConstant((TypeSymbol)type, value, syntaxNodeOpt: context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        private Cci.IMetadataNamedArgument CreateMetadataNamedArgument(string name, TypedConstant argument, EmitContext context)
        {
            var symbol = LookupName(name);
            var value = CreateMetadataExpression(argument, context);
            TypeSymbol type;
            var fieldSymbol = symbol as FieldSymbol;
            if ((object)fieldSymbol != null)
            {
                type = fieldSymbol.Type;
            }
            else
            {
                type = ((PropertySymbol)symbol).Type;
            }

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return new MetadataNamedArgument(symbol, moduleBeingBuilt.Translate(type, syntaxNodeOpt: context.SyntaxNodeOpt, diagnostics: context.Diagnostics), value);
        }

        private Symbol LookupName(string name)
        {
            var type = this.AttributeClass;
            while ((object)type != null)
            {
                foreach (var member in type.GetMembers(name))
                {
                    if (member.DeclaredAccessibility == Accessibility.Public)
                    {
                        return member;
                    }
                }
                type = type.BaseType; // BaseTypeNoUseSiteDiagnostics;
            }

            Debug.Assert(false, "Name does not match an attribute field or a property.  How can that be?");
            return null;
        }
    }
}
