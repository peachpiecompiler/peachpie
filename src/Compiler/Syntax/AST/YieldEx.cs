using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Pchp.Syntax.Parsers;

namespace Pchp.Syntax.AST
{
    /// <summary>
    /// Represents <c>yield</c> expression for the support for PHP Generator.
    /// </summary>
    public sealed class YieldEx : Expression
    {
        #region Fields & Properties

        public override Operations Operation { get { return Operations.Yield; } }

        /// <summary>
        /// Represents the key expression in case of <c>yield key =&gt; value</c> form.
        /// Can be a <c>null</c> reference in case of key is not provided.
        /// </summary>
        public Expression KeyExpr { get { return _keyEx; } }

        /// <summary>
        /// Represents the value expression in case of <c>yield key =&gt; value</c> or <c>yield value</c> forms.
        /// Can be a <c>null</c> reference in case of yield is used in read context. (see Generator::send()).
        /// </summary>
        public Expression ValueExpr { get { return _valueEx; } }

        /// <summary>
        /// <c>yield</c> parameters.
        /// </summary>
        private Expression _keyEx, _valueEx;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes new instance of <see cref="YieldEx"/>.
        /// </summary>
        public YieldEx(Text.Span span)
            : this(span, null, null)
        {
        }

        /// <summary>
        /// Initializes new instance of <see cref="YieldEx"/>.
        /// </summary>
        public YieldEx(Text.Span span, Expression keyEx, Expression valueEx)
            : base(span)
        {
            if (keyEx != null && valueEx == null) throw new ArgumentException();

            _keyEx = keyEx;
            _valueEx = valueEx;
        }

        #endregion

        #region LangElement

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitYieldEx(this);
        }

        #endregion
    }
}
