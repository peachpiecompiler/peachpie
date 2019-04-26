using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis;

namespace Peachpie.CodeAnalysis.Syntax
{
    /// <summary>
    /// this is a basic do-nothing implementation of a syntax reference
    /// </summary>
    internal class PhpSyntaxReference : SyntaxReference
    {
        readonly PhpSyntaxTree _tree;
        readonly LangElement _node;

        internal PhpSyntaxReference(PhpSyntaxTree tree, LangElement node)
        {
            _tree = tree;
            _node = node;
        }

        public override SyntaxTree SyntaxTree
        {
            get
            {
                return _tree;
            }
        }

        public override TextSpan Span
        {
            get
            {
                return _node.Span.ToTextSpan();
            }
        }

        public override SyntaxNode GetSyntax(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
