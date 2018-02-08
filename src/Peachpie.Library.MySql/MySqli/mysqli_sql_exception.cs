using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;
using Pchp.Library.Spl;

namespace Peachpie.Library.MySql.MySqli
{
    /// <summary>
    /// The mysqli exception handling class.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(Constants.ExtensionName)]
    public class mysqli_sql_exception : RuntimeException
    {
        /// <summary>
        /// Constructs the object without initialization.
        /// </summary>
        [PhpFieldsOnlyCtor]
        protected mysqli_sql_exception() { }

        /// <summary>
        /// Constructs the object.
        /// </summary>
        public mysqli_sql_exception(string message = "", long code = 0, Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }
}
