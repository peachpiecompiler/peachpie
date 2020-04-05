using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

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
        public string DisplayString => TypeCode switch
        {
            PhpTypeCode.Null => "null",    // lowercased `null` as it is shown for other CLR null references,
            PhpTypeCode.Boolean => Boolean ? PhpVariable.True : PhpVariable.False, // CONSIDER: CLR's True/False
            PhpTypeCode.Long => Long.ToString(),
            PhpTypeCode.Double => Double.ToString(CultureInfo.InvariantCulture),
            PhpTypeCode.PhpArray => "array (length = " + Array.Count.ToString() + ")",
            PhpTypeCode.String => "'" + String + "'",
            PhpTypeCode.MutableString => "'" + MutableStringBlob.ToString() + "'",
            PhpTypeCode.Object => (Object is PhpResource resource)
                ? $"resource id='{resource.Id}' type='{resource.TypeName}'"
                : Object.GetPhpTypeInfo().Name + "#" + Object.GetHashCode().ToString("X"),
            PhpTypeCode.Alias => "&" + Alias.Value.DisplayString,
            _ => "invalid",
        };

        /// <summary>
        /// Gets php type name of the value.
        /// </summary>
        internal string DebugTypeName => PhpVariable.GetTypeName(this);

        sealed class PhpValueDebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object DebugValue { get; }

            public PhpValueDebugView(PhpValue value)
            {
                DebugValue = value.ToClr();
            }
        }
    }
}
