using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Pchp.Syntax.Parsers;
using Pchp.Syntax.AST;

namespace Pchp.Syntax.AST
{
	#region enum Operations

	public enum Operations
	{
		// unary ops:
		Plus,
		Minus,
		LogicNegation,
		BitNegation,
		AtSign,
		Print,
		Clone,

		// casts:
		BoolCast,
		Int8Cast,
		Int16Cast,
		Int32Cast,
		Int64Cast,
		UInt8Cast,
		UInt16Cast,
		UInt32Cast,
		UInt64Cast,
		DoubleCast,
		FloatCast,
		DecimalCast,
		StringCast,
        BinaryCast,
		UnicodeCast,
		ObjectCast,
		ArrayCast,
		UnsetCast,

		// binary ops:
		Xor, Or, And,
		BitOr, BitXor, BitAnd,
		Equal, NotEqual,
		Identical, NotIdentical,
		LessThan, GreaterThan, LessThanOrEqual, GreaterThanOrEqual,
		ShiftLeft, ShiftRight,
		Add, Sub, Mul, Div, Mod, Pow,
		Concat,

		// n-ary ops:
		ConcatN,
		List,
		Conditional,

		// assignments:
		AssignRef,
		AssignValue,
		AssignAdd,
		AssignSub,
		AssignMul,
        AssignPow,
		AssignDiv,
		AssignMod,
		AssignAnd,
		AssignOr,
		AssignXor,
		AssignShiftLeft,
		AssignShiftRight,
		AssignAppend,
		AssignPrepend,

		// constants, variables, fields, items:
		GlobalConstUse,
		ClassConstUse,
		PseudoConstUse,
		DirectVarUse,
		IndirectVarUse,
		DirectStaticFieldUse,
		IndirectStaticFieldUse,
		ItemUse,

		// literals:
		NullLiteral,
		BoolLiteral,
		IntLiteral,
		LongIntLiteral,
		DoubleLiteral,
		StringLiteral,
		BinaryStringLiteral,

		// routine calls:
		DirectCall,
		IndirectCall,
		DirectStaticCall,
		IndirectStaticCall,

		// instances:
		New,
		Array,
		InstanceOf,
		TypeOf,

		// built-in functions:
		Inclusion,
		Isset,
		Empty,
		Eval,

		// others:
		Exit,
		ShellCommand,
		IncDec,
        Yield,

        // lambda function:
        Closure,
	}

	#endregion

	#region Expression

	/// <summary>
	/// Abstract base class for expressions.
	/// </summary>
    public abstract class Expression : LangElement
	{
		public abstract Operations Operation { get; }

        protected Expression(Text.Span span) : base(span) { }

        /// <summary>
        /// Internal type information determined during type analysis.
        /// </summary>
        public ulong/*A*/TypeInfoValue { get; set; }

		/// <summary>
        /// Whether the expression is allowed to be passed by reference to a routine.
        /// </summary>
        internal virtual bool AllowsPassByReference { get { return false; } }

		/// <summary>
		/// Whether to mark sequence point when the expression appears in an expression statement.
		/// </summary>
		internal virtual bool DoMarkSequencePoint { get { return true; } }
	}

	#endregion

	#region ConstantDecl

    public abstract class ConstantDecl : LangElement
	{
		public VariableName Name { get { return name; } }
		protected VariableName name;

        public Expression/*!*/ Initializer { get { return initializer; } internal set { initializer = value; } }
		private Expression/*!*/ initializer;

		public ConstantDecl(Text.Span span, string/*!*/ name, Expression/*!*/ initializer)
			: base(span)
		{
			this.name = new VariableName(name);
			this.initializer = initializer;
		}
	}

	#endregion

	#region VarLikeConstructUse

	/// <summary>
	/// Common abstract base class representing all constructs that behave like a variable (L-value).
	/// </summary>
	public abstract class VarLikeConstructUse : Expression
	{
        public VarLikeConstructUse IsMemberOf { get { return isMemberOf; } set { isMemberOf = value; } }
        protected VarLikeConstructUse isMemberOf;
            
		internal override bool AllowsPassByReference { get { return true; } }

		protected VarLikeConstructUse(Text.Span p) : base(p) { }
	}

	#endregion
}