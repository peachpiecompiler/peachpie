using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;

using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	/// <summary>
	/// Represents <c>array</c> constructor.
	/// </summary>
    public sealed class ArrayEx : VarLikeConstructUse
	{
		public override Operations Operation { get { return Operations.Array; } }
        internal override bool AllowsPassByReference { get { return false; } }

        public Item[]/*!*/ Items { get { return items; } }
        private readonly Item[]/*!*/items;
        
		public ArrayEx(Text.Span span, IList<Item>/*!*/items)
			: base(span)
		{
			Debug.Assert(items != null);
			this.items = items.AsArray();
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitArrayEx(this);
        }
	}

	#region Item

	/// <summary>
	/// Base class for item of an array defined by <c>array</c> constructor.
	/// </summary>
    public abstract class Item : AstNode
	{
        public Expression Index { get { return index; } internal set { index = value; } }
		private Expression index; // can be null

		protected Item(Expression index)
		{
			this.index = index;
		}

		internal bool HasKey { get { return (index != null); } }
		internal bool IsIndexLiteral { get { return index is Literal; } }
		internal bool IsIndexIntLiteral { get { return index is IntLiteral; } }
		internal bool IsIndexStringLiteral { get { return index is StringLiteral; } }
	}

	#endregion

	#region ValueItem

	/// <summary>
	/// Expression for the value of an array item defined by <c>array</c> constructor.
	/// </summary>
    public sealed class ValueItem : Item
	{
		/// <summary>Value of array item</summary>
        public Expression ValueExpr { get { return valueExpr; } internal set { valueExpr = value; } }
        private Expression valueExpr;
        
		public ValueItem(Expression index, Expression/*!*/ valueExpr)
			: base(index)
		{
			Debug.Assert(valueExpr != null);
			this.valueExpr = valueExpr;
		}
	}

	#endregion

	#region RefItem

	/// <summary>
	/// Reference to a variable containing the value of an array item defined by <c>array</c> constructor.
	/// </summary>
    public sealed class RefItem : Item
	{
		private readonly VariableUse/*!*/refToGet;
        /// <summary>Object to obtain reference of</summary>
        public VariableUse/*!*/RefToGet { get { return this.refToGet; } }

		public RefItem(Expression index, VariableUse refToGet)
			: base(index)
		{
            Debug.Assert(refToGet != null);
            this.refToGet = refToGet;
		}
	}

	#endregion

}