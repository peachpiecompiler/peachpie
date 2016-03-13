using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Runtime context for a PHP application.
    /// </summary>
    /// <remarks>
    /// The object represents a current Web request or the application run.
    /// Its instance is passed to all PHP function.
    /// The context is not thread safe.
    /// </remarks>
    public class Context
    {
        #region Echo

        public void Echo(object value)
        {

        }

        public void Echo(string value)
        {

        }

        public void Echo(PhpValue value)
        {

        }

        public void Echo(PhpNumber value)
        {

        }

        #endregion
    }
}
