using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Annotates a script class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ScriptAttribute : Attribute
    {
        /// <summary>
        /// Script path relative to the root.
        /// </summary>
        public string Path { get; private set; }

        public ScriptAttribute(string path)
        {
            this.Path = path;
        }
    }

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
