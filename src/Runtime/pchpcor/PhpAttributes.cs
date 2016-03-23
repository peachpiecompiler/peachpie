using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Assembly attribute indicating the assembly represents an extension.
    /// When this attribute is used, declared types and methods are not visible to compiler as they are,
    /// instead, only public static members are visible as global declarations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class PhpExtensionAttribute : Attribute
    {
        /// <summary>
        /// Extension name.
        /// </summary>
        readonly string _name;
        
        public PhpExtensionAttribute(string name)
        {
            _name = name;
        }
    }
}
