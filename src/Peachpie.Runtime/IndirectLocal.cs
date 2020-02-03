using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Pchp.Core
{
    /// <summary>
    /// Represents access to an unoptimized local variable.
    /// - Used for passing unoptimized local/global variables to call sites.
    /// - Used for caching lookups to the hashtable.
    /// - Used for debugger purposes to reveal unoptimized locals in Watch and Locals window.
    /// </summary>
    [DebuggerDisplay("{DisplayString,nq}", Name = "${_name,nq}", Type = "{DebugTypeName,nq}")]
    [DebuggerNonUserCode, DebuggerStepThrough]
    public struct IndirectLocal
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly OrderedDictionary _locals;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly IntStringKey _name;

        // TODO: cache ref to the value (avoid repetitious lookups to hashtable), check _locals.version (_locals.entries) did not change

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public PhpValue Value
        {
            get => _locals.GetValueOrNull(_name);
            set => _locals[_name] = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ref PhpValue ValueRef => ref _locals.EnsureValue(_name);

        /// <summary>
        /// Gets underlaying value as <see cref="PhpAlias"/>.
        /// Modifies the underlaying table.
        /// </summary>
        public PhpAlias EnsureAlias() => ValueRef.EnsureAlias();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string DisplayString => Value.DisplayString;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string DebugTypeName => Value.DebugTypeName;

        public IndirectLocal(OrderedDictionary/*!*/locals, IntStringKey/*!*/name)
        {
            _locals = locals;
            _name = name;
        }

        public IndirectLocal(PhpArray/*!*/locals, IntStringKey/*!*/name)
            : this(locals.table, name)
        {
        }

        public IndirectLocal(PhpArray/*!*/locals, string/*!*/name)
            : this(locals, new IntStringKey(name))
        {
        }

        public override string ToString() => DisplayString;
    }
}
