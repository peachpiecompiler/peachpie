using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    internal static class SplExtension
    {
        /// <summary>
        /// Name of the SPL extension.
        /// </summary>
        public const string Name = PhpExtensionAttribute.KnownExtensionNames.SPL;

        /// <summary>
        /// Gets key()/current() from the iterator.
        /// </summary>
        public static KeyValuePair<PhpValue, PhpValue> KeyValuePair(this Iterator it)
        {
            return new KeyValuePair<PhpValue, PhpValue>(it.key(), it.current());
        }
    }
}
