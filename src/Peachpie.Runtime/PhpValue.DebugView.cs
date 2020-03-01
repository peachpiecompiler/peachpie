using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    [DebuggerDisplay("{DisplayString,nq}", Type = "{DebugTypeName,nq}")]
    [DebuggerTypeProxy(typeof(PhpValueDebugView))]
    [DebuggerNonUserCode, DebuggerStepThrough]
    partial struct PhpValue
    {
        static string UndefinedTypeName => "undefined";

        /// <summary>
        /// Debug textual representation of the value.
        /// </summary>
        public string DisplayString => IsDefault ? "undefined" : _type.DisplayString(ref this);

        /// <summary>
        /// Gets php type name of the value.
        /// </summary>
        internal string DebugTypeName => IsDefault ? UndefinedTypeName : PhpVariable.GetTypeName(this);

        sealed class PhpValueDebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object DebugValue { get; }

            public PhpValueDebugView(PhpValue value)
            {
                DebugValue = value.IsDefault ? null : value.ToClr();
            }
        }
    }
}
