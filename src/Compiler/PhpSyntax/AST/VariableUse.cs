using System;
using System.Diagnostics;
using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	#region VariableUse

	/// <summary>
	/// Base class for variable uses.
	/// </summary>
	public abstract class VariableUse : VarLikeConstructUse
	{
		protected VariableUse(Text.Span p) : base(p) { }
	}

	#endregion

	#region CompoundVarUse

	/// <summary>
	/// Base class for compound variable uses.
	/// </summary>
    public abstract class CompoundVarUse : VariableUse
	{
		protected CompoundVarUse(Text.Span p) : base(p) { }
	}

	#endregion

	#region SimpleVarUse

	/// <summary>
	/// Base class for simple variable uses.
	/// </summary>
    public abstract class SimpleVarUse : CompoundVarUse
	{
        protected SimpleVarUse(Text.Span p) : base(p) { }
	}

	#endregion
}
