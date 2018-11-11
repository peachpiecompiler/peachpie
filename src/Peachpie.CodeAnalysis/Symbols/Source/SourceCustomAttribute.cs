using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    sealed class SourceCustomAttribute : AttributeData
    {
        readonly TypeRef _tref;
        readonly IList<KeyValuePair<Name, LangElement>> _arguments;
        
        public SourceCustomAttribute(TypeRef tref, IList<KeyValuePair<Name, LangElement>> arguments)
        {
            _tref = tref;
            _arguments = arguments ?? Array.Empty<KeyValuePair<Name, LangElement>>();
        }

        protected override INamedTypeSymbol CommonAttributeClass => throw new NotImplementedException();

        protected override IMethodSymbol CommonAttributeConstructor => throw new NotImplementedException();

        protected override SyntaxReference CommonApplicationSyntaxReference => throw new NotImplementedException();

        protected internal override ImmutableArray<TypedConstant> CommonConstructorArguments => throw new NotImplementedException();

        protected internal override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => throw new NotImplementedException();
    }
}
