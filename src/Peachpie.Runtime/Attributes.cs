using Pchp.Core.Reflection;
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
    /// When this attribute is used on an assembly, declared types and methods are not visible to compiler as they are,
    /// instead, only public static members are visible as global declarations.
    /// 
    /// When used on the class, the attribute also annotates extension name and its set of functions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
    public class PhpExtensionAttribute : Attribute
    {
        /// <summary>
        /// Extensions name.
        /// </summary>
        public string[] Extensions => _extensions;
        readonly string[] _extensions;

        public PhpExtensionAttribute(params string[] extensions)
        {
            _extensions = extensions;
        }

        public override string ToString()
        {
            return $"Extension: {string.Join(", ", _extensions)}";
        }
    }

    /// <summary>
    /// Marks public declarations that won't be visible to the compiled PHP script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Method)]
    public sealed class PhpHiddenAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks public class or interface declaration as a PHP type visible to the scripts from extension libraries.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PhpTypeAttribute : Attribute
    {
        readonly string _typename;

        public PhpTypeAttribute(string phpTypeName = null)
        {
            _typename = phpTypeName;
        }
    }

    /// <summary>
    /// Denotates a function parameter of type <see cref="PhpArray"/>
    /// that will be referenced to the array of local PHP variables.
    /// </summary>
    /// <remarks>
    /// The parameter is used to let the function to read or modify caller routine local variables.
    /// The parameter must be of type <see cref="PhpArray"/>.
    /// The parameter must be before regular parameters.</remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ImportLocalsAttribute : Attribute
    {
    }

    /// <summary>
    /// Denotates a function parameter that will be filled with array of callers' parameters.
    /// </summary>
    /// <remarks>
    /// The parameter is used to access calers' arguments.
    /// The parameter must be of type <c>array</c>.
    /// The parameter must be before regular parameters.</remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ImportCallerArgsAttribute : Attribute
    {
    }

    /// <summary>
    /// Denotates a function parameter that will be loaded with current class.
    /// The parameter must be of type <see cref="RuntimeTypeHandle"/>, <see cref="PhpTypeInfo"/> or <see cref="string"/>.
    /// </summary>
    /// <remarks>
    /// The parameter is used to access calers' class context.
    /// The parameter must be before regular parameters.</remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ImportCallerClassAttribute : Attribute
    {
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
    public sealed class CastToFalse : Attribute
    {

    }

    /// <summary>
    /// Marks classes that are declared as trait.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PhpTraitAttribute : Attribute
    {

    }

    /// <summary>
    /// Compiler generated attribute denoting constructor that initializes only fields and calls minimal base .ctor.
    /// Such constructor is used for emitting derived class constructor that calls PHP constructor function by itself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class PhpFieldsOnlyCtorAttribute : Attribute
    {

    }
}
