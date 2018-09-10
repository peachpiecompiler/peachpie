using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Pchp.Core
{
    /// <summary>
    /// Dummy object that shows debug value of an indirect local variable.
    /// Used for debugger purposes to reveal indirect locals in Watch and Locals window.
    /// </summary>
    [DebuggerDisplay("{DisplayString,nq}", Name = "${_name,nq}", Type = "{DebugTypeName,nq}")]
    [DebuggerNonUserCode, DebuggerStepThrough]
    public struct IndirectLocal
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly OrderedDictionary _locals;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly string _name;

        //[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public PhpValue Value
        {
            get
            {
                var key = new IntStringKey(_name);
                return _locals._get(ref key);
            }
            set
            {
                _locals[_name] = value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string DisplayString => Value.DisplayString;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string DebugTypeName => Value.DebugTypeName;

        public IndirectLocal(OrderedDictionary/*!*/locals, string/*!*/name)
        {
            _locals = locals;
            _name = name;
        }

        public IndirectLocal(PhpArray/*!*/locals, string/*!*/name)
            : this(locals.table, name)
        {
        }

        public override string ToString() => DisplayString;
    }
}
