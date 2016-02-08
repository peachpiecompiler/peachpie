using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents an <c>echo</c> statement.
	/// </summary>
	public sealed class EchoStmt : Statement
	{
		/// <summary>Array of parameters - Expressions.</summary>
        public Expression[] /*!*/ Parameters { get { return parameters; } }
        private Expression[]/*!*/ parameters;
        
        /// <summary>
        /// Gets value indicating whether this <see cref="EchoStmt"/> represents HTML code.
        /// </summary>
        public bool IsHtmlCode { get { return isHtmlCode; } }
        private readonly bool isHtmlCode;

		public EchoStmt(Text.Span span, IList<Expression>/*!*/ parameters)
            : base(span)
		{
			Debug.Assert(parameters != null);
			this.parameters = parameters.AsArray();
            this.isHtmlCode = false;
		}

        /// <summary>
        /// Initializes new echo statement as a representation of HTML code.
        /// </summary>
        public EchoStmt(Text.Span span, string htmlCode)
            : base(span)
        {
            this.parameters = new Expression[] { new StringLiteral(span, htmlCode) };
            this.isHtmlCode = true;
        }

		internal override bool SkipInPureGlobalCode()
		{
			StringLiteral literal;
			if (parameters.Length == 1 && (literal = parameters[0] as StringLiteral) != null)
			{
				return StringUtils.IsWhitespace((string)literal.Value);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEchoStmt(this);
        }
	}
}