using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using Pchp.Core.Reflection;
using System.Reflection;

namespace Pchp.Core
{
    partial struct PhpValue
    {
        /// <summary>
        /// Methods table for <see cref="PhpValue"/> instance.
        /// </summary>
        [DebuggerNonUserCode, DebuggerStepThrough]
        abstract class TypeTable
        {
            #region Singletons

            public static readonly NullTable NullTable = new NullTable();
            public static readonly LongTable LongTable = new LongTable();
            public static readonly DoubleTable DoubleTable = new DoubleTable();
            public static readonly BoolTable BoolTable = new BoolTable();
            public static readonly StringTable StringTable = new StringTable();
            public static readonly MutableStringTable MutableStringTable = new MutableStringTable();
            public static readonly ClassTable ClassTable = new ClassTable();
            public static readonly ArrayTable ArrayTable = new ArrayTable();
            public static readonly AliasTable AliasTable = new AliasTable();

            #endregion

            public abstract PhpTypeCode Type { get; }
            
            /// <summary>
            /// Ensures the value is a PHP array.
            /// In case it isn't, creates PhpArray according to PHP semantics.
            /// In case current value is empty, replaces current value with newly created array.
            /// </summary>
            /// <returns>Non-null object.</returns>
            public abstract IPhpArray EnsureArray(ref PhpValue me);

            /// <summary>
            /// Gets <see cref="IPhpArray"/> instance providing access to the value with array operators.
            /// Returns <c>null</c> if underlaying value does provide array access.
            /// </summary>
            public virtual IPhpArray GetArrayAccess(ref PhpValue me) => null;

            /// <summary>
            /// Accesses the value as an array and gets item at given index.
            /// Gets empty value in case the key is not found.
            /// </summary>
            public virtual PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => new PhpAlias(PhpValue.Null);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        class NullTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Null;
            public override IPhpArray EnsureArray(ref PhpValue me)
            {
                var arr = new PhpArray();
                me = PhpValue.Create(arr);
                return arr;
            }
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet)
            {
                var arr = new PhpArray();
                me = PhpValue.Create(arr);
                return arr.EnsureItemAlias(index, quiet);
            }
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class LongTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Long;
            public override IPhpArray EnsureArray(ref PhpValue me) => new PhpArray(); // me is not changed
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => new PhpAlias(PhpValue.Null);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class DoubleTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Double;
            public override IPhpArray EnsureArray(ref PhpValue me) => new PhpArray(); // me is not changed
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => new PhpAlias(PhpValue.Null);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class BoolTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Boolean;
            public override IPhpArray EnsureArray(ref PhpValue me)
            {
                var arr = new PhpArray();

                // me is changed if me.Boolean == FALSE
                if (me.Boolean == false)
                    me = PhpValue.Create(arr);

                return arr;
            }
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => new PhpAlias(PhpValue.Null);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class StringTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.String;
            public override IPhpArray EnsureArray(ref PhpValue me)
            {
                var str = new PhpString(me.String); // upgrade to mutable string
                var arr = str.EnsureWritable();     // ensure its internal blob

                me = PhpValue.Create(str);          // copy back new value

                //
                return arr;
            }
            public override IPhpArray GetArrayAccess(ref PhpValue me) => EnsureArray(ref me);
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) { throw new NotSupportedException(); } // TODO: Err
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class MutableStringTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.MutableString;
            public override IPhpArray EnsureArray(ref PhpValue me)
            {
                // ensure blob is lazily copied
                var blob = me.MutableStringBlob;
                if (blob.IsShared)
                {
                    me._obj.blob = blob = blob.ReleaseOne();
                }
                //
                return blob;
            }
            public override IPhpArray GetArrayAccess(ref PhpValue me) => me.MutableStringBlob;
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) { throw new NotSupportedException(); } // TODO: Err
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class ClassTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Object;
            public override IPhpArray EnsureArray(ref PhpValue me) => Operators.EnsureArray(me.Object);
            public override IPhpArray GetArrayAccess(ref PhpValue me) => Operators.EnsureArray(me.Object);
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet)
            {
                if (me.Object is IPhpArray arr)
                {
                    return Operators.EnsureItemAlias(arr, index, quiet);
                }

                if (!quiet) // NOTE: PHP does not report this error (?)
                {
                    PhpException.Throw(PhpError.Error, Resources.ErrResources.object_used_as_array, me.Object.GetPhpTypeInfo().Name);
                }

                return new PhpAlias(PhpValue.Null);
            }
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class ArrayTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.PhpArray;
            public override IPhpArray EnsureArray(ref PhpValue me) => me.Array; // EnsureWritable() called lazily when writing
            public override IPhpArray GetArrayAccess(ref PhpValue me) => me.Array;
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => me.Array.EnsureItemAlias(index, quiet);
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        sealed class AliasTable : TypeTable
        {
            public override PhpTypeCode Type => PhpTypeCode.Alias;
            public override IPhpArray EnsureArray(ref PhpValue me) => me.Alias.Value.EnsureArray();
            public override IPhpArray GetArrayAccess(ref PhpValue me) => me.Alias.Value.GetArrayAccess();
            public override PhpAlias EnsureItemAlias(ref PhpValue me, PhpValue index, bool quiet) => me.Alias.Value.EnsureItemAlias(index, quiet);
        }
    }
}
