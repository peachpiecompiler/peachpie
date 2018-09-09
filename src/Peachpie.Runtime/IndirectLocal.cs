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
    [DebuggerDisplay("{DisplayString,nq}", Name = "{_name,nq}", Type = "{DebugTypeName,nq}")]
    [DebuggerTypeProxy(typeof(IndirectLocal.DebugView))]
    [DebuggerNonUserCode, DebuggerStepThrough]
    public struct IndirectLocal
    {
        [DebuggerDisplay("{_value.DisplayString,nq}", Type = "{_value.DebugTypeName,nq}")]
        sealed class DebugView
        {
            readonly IndirectLocal _local;

            public DebugView(IndirectLocal local)
            {
                _local = local;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public PhpValue DebugValue => _local.Value;
        }

        readonly OrderedDictionary _locals;
        readonly string _name;

        ref PhpValue Value
        {
            get => ref _locals._getref(_name);
            //set => _locals[_name] = value;
        }

        string DisplayString => Value.DisplayString;

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
