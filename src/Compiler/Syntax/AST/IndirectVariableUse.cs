using System;
using System.IO;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Indirect variable use - a variable or a field access by run-time evaluated name.
	/// </summary>
	public sealed class IndirectVarUse : SimpleVarUse
	{
        public override Operations Operation { get { return Operations.IndirectVarUse; } }

		public Expression VarNameEx { get { return varNameEx; } }
		internal Expression varNameEx;

		public IndirectVarUse(Text.Span span, int levelOfIndirection, Expression varNameEx)
            : base(span)
		{
			Debug.Assert(levelOfIndirection > 0 && varNameEx != null);

			if (levelOfIndirection == 1)
			{
				this.varNameEx = varNameEx;
			}
			else
			{
                Text.Span varspan = new Text.Span(span.Start + 1, span.Length - 1);
                this.varNameEx = new IndirectVarUse(varspan, --levelOfIndirection, varNameEx);
			}
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectVarUse(this);
        }

	}
}
