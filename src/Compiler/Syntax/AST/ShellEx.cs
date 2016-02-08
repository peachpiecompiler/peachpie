using System;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents a content of backtick operator (shell command execution).
	/// </summary>
	public sealed class ShellEx : Expression
	{
        public override Operations Operation { get { return Operations.ShellCommand; } }

		/// <summary>Command to excute</summary>
        public Expression/*!*/ Command { get { return command; } internal set { command = value; } }
        private Expression/*!*/ command;
        
		public ShellEx(Text.Span span, Expression/*!*/ command)
            : base(span)
		{
            Debug.Assert(command is StringLiteral || command is ConcatEx || command is BinaryStringLiteral);
			this.command = command;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitShellEx(this);
        }
	}
}
