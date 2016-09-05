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
    partial struct PhpValue
    {
        /// <summary>
        /// Debug textual representation of the value.
        /// </summary>
        internal string DisplayString => IsSet ? _type.DisplayString(ref this) : "Undefined";

        /// <summary>
        /// Gets php type name of the value.
        /// </summary>
        internal string DebugTypeName => IsSet ? PhpVariable.GetTypeName(this) : "Void";

        [DebuggerDisplay("{_value.DisplayString,nq}", Type = "{_value.DebugTypeName,nq}")]
        internal sealed class PhpValueDebugView
        {
            readonly PhpValue _value;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object DebugValue
            {
                get
                {
                    switch (_value.TypeCode)
                    {
                        case PhpTypeCode.Alias: return _value.Alias;
                        case PhpTypeCode.Boolean: return _value.Boolean;
                        case PhpTypeCode.Double: return _value.Double;
                        case PhpTypeCode.Int32: return _value._value.Int;
                        case PhpTypeCode.Long: return _value.Long;
                        case PhpTypeCode.Object: return _value.Object;
                        case PhpTypeCode.PhpArray: return _value.Array;
                        case PhpTypeCode.String: return _value.String;
                        case PhpTypeCode.WritableString: return _value.WritableString.ToString();
                        default: return null;
                    }
                }
            }

            public PhpValueDebugView(PhpValue value)
            {
                _value = value;
            }
        }
    }
}
