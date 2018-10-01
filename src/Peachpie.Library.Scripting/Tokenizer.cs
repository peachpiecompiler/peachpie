using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    [PhpExtension("tokenizer")]
    public static class Tokenizer
    {
        // TODO: T_*** constants

        /// <summary>
        /// Get the symbolic name of a given PHP token.
        /// </summary>
        public static string token_name(int token)
        {
            return ((Devsense.PHP.Syntax.Tokens)token).ToString();
        }
    }
}
