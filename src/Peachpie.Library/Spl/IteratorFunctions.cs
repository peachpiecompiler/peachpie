using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    [PhpExtension(SplExtension.Name)]
    public static class IteratorFunctions
    {
        /// <summary>
        /// Count the elements in an iterator.
        /// </summary>
        public static int iterator_count(Iterator iterator)
        {
            if (iterator != null)
            {
                // iterate through the iterator:
                int n = 0;

                iterator.rewind();

                while (iterator.valid())
                {
                    n++;
                    iterator.next();
                }

                // return amount of iterated elements:
                return n;
            }
            else
            {
                throw new ArgumentNullException(nameof(iterator));
            }
        }

        /// <summary>
        /// Copy the elements of an iterator into an array.
        /// </summary>
        /// <param name="iterator">The iterator being copied.</param>
        /// <param name="use_keys">Whether to use the iterator element keys as index.</param>
        /// <returns>An array containing the elements of the iterator.</returns>
        public static PhpArray iterator_to_array(Iterator iterator, bool use_keys = true)
        {
            if (iterator == null)
            {
                throw new ArgumentNullException(nameof(iterator));
            }

            var array = new PhpArray();

            iterator.rewind();

            while (iterator.valid())
            {
                var value = iterator.current();
                if (use_keys)
                {
                    array.Add(iterator.key().ToIntStringKey(), value);
                }
                else
                {
                    array.Add(value);
                }

                //
                iterator.next();
            }

            //
            return array;
        }

        /// <summary>
        /// Calls a function for every element in an iterator.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="iterator">The class to iterate over.</param>
        /// <param name="function">The callback function to call on every element.
        /// Note: The function must return <c>TRUE</c> in order to continue iterating over the iterator.</param>
        /// <param name="args">Arguments to pass to the callback function.</param>
        /// <returns>The iteration count.</returns>
        public static int iterator_apply(Context ctx, Iterator iterator, IPhpCallable function, PhpArray args = null)
        {
            if (iterator == null)
            {
                throw new ArgumentNullException(nameof(iterator));
            }

            if (function == null)
            {
                PhpException.ArgumentNull(nameof(function));
                return -1;
            }

            // construct callback arguments
            var args_array = args != null
                ? args.GetValues()
                : Array.Empty<PhpValue>();

            //
            int n = 0;

            iterator.rewind();

            while (iterator.valid())
            {
                if (function.Invoke(ctx, args_array).ToBoolean() == false)
                {
                    break;
                }

                //
                n++;
                iterator.next();
            }

            //
            return n;
        }
    }
}
