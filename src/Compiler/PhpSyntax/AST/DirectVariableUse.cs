using System;
using System.IO;
using System.Diagnostics;

using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	/// <summary>
	/// Direct variable use - a variable or a field accessed by an identifier.
	/// </summary>
	public sealed class DirectVarUse : SimpleVarUse
	{
        public override Operations Operation { get { return Operations.DirectVarUse; } }

		public VariableName VarName { get { return varName; } set { varName = value; } }
		private VariableName varName;

		public DirectVarUse(Text.Span span, VariableName varName)
            : base(span)
		{
			this.varName = varName;
		}

		public DirectVarUse(Text.Span span, string/*!*/ varName)
            : base(span)
		{
			this.varName = new VariableName(varName);
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectVarUse(this);
        }
	}
}
