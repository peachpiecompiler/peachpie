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
    public readonly struct IndirectLocal
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly OrderedDictionary _locals;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly IntStringKey _name;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        object DebugValue
        {
            get => _locals.TryGetValue(_name, out var value) ? value.ToClr() : null;
            //set => _locals[_name] = value;
        }

        ref PhpValue EnsureValueRef() => ref _locals.EnsureValue(_name);

        /// <summary>
        /// Gets underlaying value as <see cref="PhpAlias"/>.
        /// Modifies the underlaying table.
        /// </summary>
        public PhpAlias EnsureAlias() => PhpValue.EnsureAlias(ref EnsureValueRef());

        /// <summary>
        /// Gets the underlaying value or <c>NULL</c> if value does not exist.
        /// </summary>
        public PhpValue GetValue() => _locals.GetValueOrNull(_name);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string DisplayString => GetValue().DisplayString;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string DebugTypeName => GetValue().DebugTypeName;

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
