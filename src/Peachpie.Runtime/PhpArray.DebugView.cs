using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    [DebuggerDisplay("array(length = {Count})", Type = PhpTypeName)]
    [DebuggerTypeProxy(typeof(PhpArrayDebugView))]
    [DebuggerNonUserCode]
    partial class PhpArray
    {
        sealed class PhpArrayDebugView
        {
            readonly PhpArray _array;

            public PhpArrayDebugView(PhpArray/*!*/ array)
            {
                _array = array;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public PhpHashEntryDebugView[] Items
            {
                get
                {
                    var result = new PhpHashEntryDebugView[_array.Count];

                    int i = 0;
                    var enumerator = _array.GetFastEnumerator();
                    while (enumerator.MoveNext())
                    {
                        result[i++] = new PhpHashEntryDebugView(enumerator.CurrentKey, enumerator.CurrentValue);
                    }
                    
                    return result;
                }
            }
        }

        [DebuggerDisplay("{_value.DisplayString,nq}", Name = "[{Key}]", Type = "{KeyType,nq} => {ValueType,nq}")]
        sealed class PhpHashEntryDebugView
        {
            [DebuggerDisplay("{Key}", Name = "Key", Type = "{KeyType,nq}")]
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public object Key { get { return _key.Object; } }

            [DebuggerDisplay("{_value}", Name = "Value", Type = "{ValueType,nq}")]
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public PhpValue Value { get { return _value; } }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            readonly IntStringKey _key;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            readonly PhpValue _value;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public string KeyType
            {
                get
                {
                    return _key.IsInteger ? PhpVariable.TypeNameInteger : PhpVariable.TypeNameString;
                }
            }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public string ValueType
            {
                get
                {
                    return PhpVariable.GetTypeName(_value);
                }
            }

            public PhpHashEntryDebugView(IntStringKey key, PhpValue value)
            {
                _key = key;
                _value = value;
            }
        }
    }
}
