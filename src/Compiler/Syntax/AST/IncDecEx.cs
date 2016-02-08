using System;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Post/pre increment/decrement expression.
	/// </summary>
	public sealed class IncDecEx : Expression
	{
        public override Operations Operation { get { return Operations.IncDec; } }

        [Flags]
        private enum Flags : byte
        {
            /// <summary>
            /// Indicates incrementation.
            /// </summary>
            incrementation = 1,

            /// <summary>
            /// Indicates post-incrementation or post-decrementation.
            /// </summary>
            post = 2,
        }

        private readonly Flags flags;

        /// <summary>Indicates incrementation.</summary>
        public bool Inc { get { return flags.HasFlag(Flags.incrementation); } }
		/// <summary>Indicates post-incrementation or post-decrementation</summary>
        public bool Post { get { return flags.HasFlag(Flags.post); } }

        private VariableUse/*!*/ variable;
        /// <summary>Variable being incremented/decremented</summary>
        public VariableUse /*!*/ Variable { get { return variable; } }

		public IncDecEx(Text.Span span, bool inc, bool post, VariableUse/*!*/ variable)
			: base(span)
		{
			this.variable = variable;

            if (inc) this.flags |= Flags.incrementation;
            if (post) this.flags |= Flags.post;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIncDecEx(this);
        }
	}
}
