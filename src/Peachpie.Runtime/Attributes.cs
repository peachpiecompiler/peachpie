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

    /// <summary>
	/// Marks return values of methods implementing PHP functions which returns <B>false</B> on error
	/// but has other return type than <see cref="bool"/> or <see cref="object"/>.
	/// </summary>
	/// <remarks>
	/// Compiler takes care of converting a return value of a method into <B>false</B> if necessary.
	/// An attribute can be applied only on return values of type <see cref="int"/> or <see cref="double"/> (less than 0 is converted to <B>false</B>)
	/// or of a reference type (<B>null</B> is converted to <B>false</B>).
	/// </remarks>
    [AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    public class CastToFalse : Attribute
    {

    }
}
