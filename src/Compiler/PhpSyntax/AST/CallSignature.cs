using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;

using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
	#region ActualParam

	public sealed class ActualParam : LangElement
	{
        [Flags]
        public enum Flags
        {
            Default = 0,
            IsByRef = 1,
            IsVariadic = 2,
        }

		public Expression/*!*/ Expression { get { return _expression; } }
		internal Expression/*!*/_expression;

        /// <summary>
        /// Gets value indicating whether the parameter is prefixed by <c>&amp;</c> character.
        /// </summary>
        public bool Ampersand { get { return (_flags & Flags.IsByRef) != 0; } }

        /// <summary>
        /// Gets value indicating whether the parameter is passed with <c>...</c> prefix and so it has to be unpacked before passing to the function call.
        /// </summary>
        public bool IsVariadic { get { return (_flags & Flags.IsVariadic) != 0; } }
        
        /// <summary>
        /// Flags describing use of the parameter.
        /// </summary>
        private Flags _flags;

        public ActualParam(Text.Span p, Expression param)
            : this(p, param, Flags.Default)
        { }

        public ActualParam(Text.Span p, Expression param, Flags flags)
			: base(p)
		{
			Debug.Assert(param != null);
			_expression = param;
            _flags = flags;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitActualParam(this);
        }
	}

	#endregion

	#region NamedActualParam

    public sealed class NamedActualParam : LangElement
	{
		public Expression/*!*/ Expression { get { return expression; } }
		internal Expression/*!*/ expression;

		public VariableName Name { get { return name; } }
		private VariableName name;

        public NamedActualParam(Text.Span span, string name, Expression/*!*/ expression)
            : base(span)
        {
            this.name = new VariableName(name);
            this.expression = expression;
        }

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitNamedActualParam(this);
        }
	}

	#endregion

	#region CallSignature

    public sealed class CallSignature : AstNode
	{
		/// <summary>
		/// List of actual parameters (<see cref="ActualParam"/> nodes).
		/// </summary>	
		public ActualParam[]/*!*/ Parameters { get { return parameters; } }
		private readonly ActualParam[]/*!*/ parameters;

		/// <summary>
		/// List of generic parameters.
		/// </summary>
        public TypeRef[]/*!*/ GenericParams
        {
            get { return this.GetProperty<TypeRef[]>() ?? EmptyArray<TypeRef>.Instance; }
            set
            {
                if (value.Any())
                    this.SetProperty<TypeRef[]>(value);
                else
                    this.Properties.RemoveProperty<TypeRef[]>();
            }
        }

        /// <summary>
        /// Initialize new instance of <see cref="CallSignature"/>.
        /// </summary>
        /// <param name="parameters">List of parameters.</param>
        public CallSignature(IList<ActualParam> parameters)
            : this(parameters, null)
        {
        }
        
        /// <summary>
        /// Initialize new instance of <see cref="CallSignature"/>.
        /// </summary>
        /// <param name="parameters">List of parameters.</param>
        /// <param name="genericParams">List of type parameters for generics.</param>
        public CallSignature(IList<ActualParam> parameters, IList<TypeRef> genericParams)
		{
			this.parameters = parameters.AsArray();
            this.GenericParams = genericParams.AsArray();
		}        
    }

	#endregion
}
