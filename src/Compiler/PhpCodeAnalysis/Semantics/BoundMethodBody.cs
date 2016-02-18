using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a method semantics.
    /// </summary>
    public class BoundMethodBody : BoundBlock
    {
        // type context

        // locals state

        protected ImmutableArray<ILocalSymbol> _locals;

        /// <summary>
        /// Array of local variables.
        /// </summary>
        public override ImmutableArray<ILocalSymbol> Locals => _locals;

        public BoundMethodBody(IEnumerable<IStatement> statements)
            : this(statements, ImmutableArray<ILocalSymbol>.Empty)
        { }

        public BoundMethodBody(IEnumerable<IStatement> statements, ImmutableArray<ILocalSymbol> locals)
            :base(statements)
        {
            _locals = locals;
        }
    }
}
