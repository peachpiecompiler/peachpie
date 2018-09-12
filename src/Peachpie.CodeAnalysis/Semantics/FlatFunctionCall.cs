using System;
using System.Collections.Generic;
using System.Text;
using Devsense.PHP.Text;

namespace Devsense.PHP.Syntax.Ast
{
    public abstract class FlatFunctionCall : FunctionCall
    {
        public FlatFunctionCall(Span span, IList<ActualParam> parameters, Span parametersSpan, IList<TypeRef> genericParams) :
            base(span, parameters, parametersSpan, genericParams)
        {
        }

        public FlatFunctionCall[] CallStack { get; set; }
    }
}
