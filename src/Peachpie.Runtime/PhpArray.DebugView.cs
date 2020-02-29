using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    [DebuggerDisplay("array (length = {Count})", Type = PhpTypeName)]
    [DebuggerTypeProxy(typeof(PhpArrayDebugView))]
    [DebuggerNonUserCode, DebuggerStepThrough]
    partial class PhpArray
    {
        [DebuggerNonUserCode]
        sealed class PhpArrayDebugView
        {
            readonly PhpArray _array;

            public PhpArrayDebugView(PhpArray/*!*/ array)
            {
                _array = array;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public PhpArrayEntryDebugProxy[] Items
            {
                get
                {
                    var count = Math.Min(_array.Count, 100);
                    if (count == 0)
                    {
                        return Array.Empty<PhpArrayEntryDebugProxy>();
                    }

                    //
                    var result = new PhpArrayEntryDebugProxy[count];

                    int i = 0;
                    var enumerator = _array.GetFastEnumerator();
                    while (enumerator.MoveNext() && i < count)
                    {
                        result[i++] = new PhpArrayEntryDebugProxy(_array, enumerator.CurrentKey, enumerator.CurrentValue);
                    }

                    return result;
                }
            }
        }

        [DebuggerDisplay("{Value}", Name = "{_key}", Type = "{DebugTypeName,nq}")]
        [DebuggerNonUserCode]
        readonly struct PhpArrayEntryDebugProxy
        {
            //[DebuggerBrowsable(DebuggerBrowsableState.Never)]
            //readonly PhpArray _array;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            readonly PhpValue _value;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            readonly IntStringKey _key;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            string DebugTypeName => _value.DebugTypeName;
            //string DebugTypeName => $"{(_key.IsInteger ? PhpVariable.TypeNameInteger : PhpVariable.TypeNameString)} => {_value.DebugTypeName}";

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Value
            {
                get => _value.ToClr();
            }

            public PhpArrayEntryDebugProxy(PhpArray array, IntStringKey key, PhpValue value)
            {
                //_array = array ?? throw new ArgumentNullException();
                _key = key;
                _value = value;
            }
        }
    }
}
