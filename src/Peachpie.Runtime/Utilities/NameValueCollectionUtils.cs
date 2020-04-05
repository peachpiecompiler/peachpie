#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    public static class NameValueCollectionUtils
    {
        /// <summary>
        /// Fixes top level variable name to not contain spaces and dots (as it is in PHP);
        /// </summary>
        public static string EncodeTopLevelName(string/*!*/name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return name.Replace('.', '_').Replace(' ', '_');
        }

        public static PhpArray EnsureItemArray(PhpArray array, IntStringKey key)
        {
            PhpArray result;

            if (key.IsEmpty)
            {
                result = new PhpArray();
                array.Add(result);
            }
            else
            {
                if (array.TryGetValue(key, out var value) && value.IsPhpArray(out var tmp) && tmp != null)
                {
                    result = tmp;
                }
                else
                {
                    result = new PhpArray();
                    array[key] = result;
                }
            }

            //

            return result;
        }

        public static void EnsureItemArray(PhpArray array, IntStringKey item, IntStringKey key, PhpValue value)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (item.IsEmpty) throw new ArgumentException(nameof(item));

            // array[item]: array(key => value)

            var itemarr = (PhpArray)array.EnsureItemArray(item);

            if (key.IsEmpty)
            {
                itemarr.Add(value);
            }
            else
            {
                itemarr[key] = value;
            }
        }

        /// <summary>
        /// Adds a variable to auto-global array.
        /// Duplicit entries are collected into a sub-array item.
        /// The routine respects <c>[subkey]</c> notation to build a hierarchy of sub-arrays.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="name">A unparsed name of variable.</param>
        /// <param name="value">A value to be added.</param>
        /// <param name="subname">A name of intermediate array inserted before the value.</param>
        public static void AddVariable(this PhpArray/*!*/ array, string name, PhpValue value, string? subname = null)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (name == null) throw new ArgumentNullException(nameof(name));

            Debug.Assert(value != null);

            IntStringKey key;

            // current left and right square brace positions:
            int left, right;

            // checks pattern {var_name}[{key1}][{key2}]...[{keyn}] where var_name is [^[]* and keys are [^]]*:
            left = name.IndexOf('[');
            if (left > 0 && left < name.Length - 1 && (right = name.IndexOf(']', left + 1)) >= 0)
            {
                // the variable name is a key to the "array", dots are replaced by underscores in top-level name:
                key = new IntStringKey(EncodeTopLevelName(name.Substring(0, left)));

                // ensures that all [] operators in the chain except for the last one are applied on an array:
                for (; ; )
                {
                    // adds a level keyed by "key":
                    array = EnsureItemArray(array, key);

                    // adds a level keyed by "subname" (once only):
                    if (subname != null)
                    {
                        array = EnsureItemArray(array, Convert.StringToArrayKey(subname));
                        subname = null;
                    }

                    // next key:
                    key = Convert.StringToArrayKey(name.Substring(left + 1, right - left - 1)); // key can be a number

                    // breaks if ']' is not followed by '[':
                    left = right + 1;
                    if (left == name.Length || name[left] != '[')
                    {
                        break;
                    }

                    // the next right brace:
                    right = name.IndexOf(']', left + 1);
                }

                if (key.Equals(IntStringKey.EmptyStringKey))
                {
                    array.AddValue(value);
                }
                else
                {
                    array.SetItemValue(key, value);
                }
            }
            else
            {
                // no array pattern in variable name, "name" is a top-level key:
                key = new IntStringKey(EncodeTopLevelName(name));

                // inserts a subname on the next level:
                if (subname != null)
                {
                    EnsureItemArray(array, key).SetItemValue(Convert.StringToArrayKey(subname), value);
                }
                else
                {
                    array.SetItemValue(key, value);
                }
            }
        }
    }
}
