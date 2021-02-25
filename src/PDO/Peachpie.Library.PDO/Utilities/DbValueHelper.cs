using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.Library.PDO.Utilities
{
    static class DbValueHelper
    {
        /// <summary>
        /// Gets boxed boolean value.
        /// </summary>
        public static object/*!*/AsObject(this bool value) => value ? s_TrueObject : s_FalseObject;

        static readonly object s_TrueObject = (object)true;

        static readonly object s_FalseObject = (object)false;
    }
}
