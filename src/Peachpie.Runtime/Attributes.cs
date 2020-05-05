#nullable enable

using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
    /// Annotates a script class from a phar archive.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PharAttribute : Attribute
    {
        /// <summary>
        /// PHAR file name.
        /// </summary>
        public string PharFile { get; private set; }

        public PharAttribute(string pharFile)
        {
            this.PharFile = pharFile;
        }
    }

    /// <summary>
    /// Assembly attribute indicating the assembly represents an extension.
    /// When this attribute is used on an assembly, declared types and methods are not visible to compiler as they are,
    /// instead, only public static members are visible as global declarations.
    /// 
    /// When used on the class, the attribute also annotates extension name and its set of functions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class PhpExtensionAttribute : Attribute
    {
        /// <summary>
        /// Extensions name list.
        /// Cannot be <c>null</c>.
        /// </summary>
        public string[] Extensions
            => _extensions is string name ? new[] { name }
            : _extensions is string[] names ? names
            : Array.Empty<string>();

        /// <summary>
        /// Gets the first specified extension name or <c>null</c>.
        /// </summary>
        public string? FirstExtensionOrDefault
            => _extensions is string name ? name
            : _extensions is string[] names && names.Length != 0 ? names[0]
            : null;

        /// <summary>
        /// <see cref="string"/>, <see cref="string"/>[] or <c>null</c>.
        /// </summary>
        readonly object _extensions;

        /// <summary>
        /// Optional.
        /// Type of class that will be instantiated in order to subscribe to <see cref="Context"/> events and/or perform one-time initialization.
        /// </summary>
        /// <remarks>
        /// The object is used to handle one-time initialization and context life-cycle.
        /// Implement initialization and subscription logic in .ctor.
        /// </remarks>
        public Type? Registrator { get; set; }

        public PhpExtensionAttribute()
        {
            _extensions = Array.Empty<string>();
        }

        public PhpExtensionAttribute(string extension)
        {
            _extensions = extension;
        }

        public PhpExtensionAttribute(params string[] extensions)
        {
            _extensions = extensions;
        }

        public override string ToString()
        {
            return $"Extension: {string.Join(", ", this.Extensions)}";
        }
    }

    /// <summary>
    /// Assembly attribute specifying language option used to compile the assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class TargetPhpLanguageAttribute : Attribute
    {
        /// <summary>
        /// Whether short open tags were enabled to compile the sources.
        /// </summary>
        public bool ShortOpenTag { get; set; }

        /// <summary>
        /// The language version of compiled sources.
        /// </summary>
        public string LanguageVersion { get; set; }

        /// <summary>
        /// Construct the attribute.
        /// </summary>
        public TargetPhpLanguageAttribute(string langVersion, bool shortOpenTag)
        {
            this.ShortOpenTag = shortOpenTag;
            this.LanguageVersion = langVersion;
        }
    }

    /// <summary>
    /// Marks public declarations that won't be visible in the PHP context.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PhpHiddenAttribute : Attribute
    {
    }

    /// <summary>
    /// Indicates to compiler that a symbol should be ignored unless a specified conditional compilation scope is valid.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class PhpConditionalAttribute : Attribute
    {
        public string ConditionString => _scope;
        readonly string _scope;

        public PhpConditionalAttribute(string scope)
        {
            _scope = scope;
        }
    }

    /// <summary>
    /// Marks public class or interface declaration as a PHP type visible to the scripts from extension libraries.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class PhpTypeAttribute : Attribute
    {
        /// <summary>
        /// Optional. Explicitly set type name.
        /// </summary>
        public string? ExplicitTypeName { get; }

        /// <summary>
        /// Indicates how to treat the type name.
        /// </summary>
        public PhpTypeName TypeNameAs { get; }

        /// <summary>
        /// Optional. Relative path to the file where the type is defined.
        /// </summary>
        public string? FileName { get; }

        /// <summary>
        /// - 0: type is not selected to be autoloaded.<br/>
        /// - 1: type is marked to be autoloaded.<br/>
        /// - 2: type is marked to be autoloaded and it is the only unconditional declaration in its source file.<br/>
        /// </summary>
        public byte AutoloadFlag { get; }

        public const byte AutoloadAllow = 1;

        public const byte AutoloadAllowNoSideEffect = 2;

        /// <summary>
        /// Value stating that the type name is inherited from the CLR name excluding its namespace part, see <see cref="PhpTypeName.NameOnly"/>.
        /// It causes CLR type <c>A.B.C.X</c> to appear in PHP as <c>X</c>.
        /// </summary>
        public const PhpTypeName InheritName = PhpTypeName.NameOnly;

        /// <summary>
        /// Value indicating how to treat the type name in PHP.
        /// </summary>
        public enum PhpTypeName : byte
        {
            /// <summary>
            /// Full type name including its namespace name is used.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Namespace of the CLR type is ignored.
            /// </summary>
            NameOnly = 1,

            /// <summary>
            /// The name is set explicitly overriding the CLR's type name.
            /// </summary>
            CustomName = 2,
        }

        /// <summary>
        /// Annotates the PHP type.
        /// </summary>
        public PhpTypeAttribute(PhpTypeName typeNameAs = PhpTypeName.Default)
        {
            Debug.Assert(typeNameAs != PhpTypeName.CustomName);
            TypeNameAs = typeNameAs;
        }

        public PhpTypeAttribute(string phpTypeName, string fileName)
            : this(phpTypeName, fileName, default)
        {
        }

        /// <summary>
        /// Annotates the PHP type.
        /// </summary>
        /// <param name="phpTypeName">The type name that will be used in PHP context instead of CLR type name.</param>
        /// <param name="fileName">Optional relative path to the file where the type is defined.</param>
        /// <param name="autoload">Optional. Specifies if the type can be autoloaded:<br/>
        /// - 0: type is not selected to be autloaded.<br/>
        /// - 1: type is marked to be autoloaded.<br/>
        /// - 2: type is marked to be autoloaded and it is the only unconditional declaration in its source file.<br/>
        /// </param>
        public PhpTypeAttribute(string phpTypeName, string fileName, byte autoload)
        {
            ExplicitTypeName = phpTypeName ?? throw new ArgumentNullException();
            FileName = fileName;
            TypeNameAs = PhpTypeName.CustomName;
            AutoloadFlag = autoload;
        }
    }

    /// <summary>
    /// Specifies real member accessibility as it will appear in declaring class.
    /// </summary>
    /// <remarks>
    /// Some members have to be emitted as public to be accessible from outside but appear non-public in PHP context.
    /// This attribute specifies real visibility of the member - method, property or class constant.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PhpMemberVisibilityAttribute : Attribute
    {
        /// <summary>
        /// Declared member accessibility flag.
        /// </summary>
        public int Accessibility { get; }

        /// <summary>
        /// Initializes the attribute.
        /// </summary>
        public PhpMemberVisibilityAttribute(int accessibility) { this.Accessibility = accessibility; }
    }

    /// <summary>
    /// Denotates a function parameter that will import a special runtime value.
    /// </summary>
    /// <remarks>
    /// This attribute instructs the caller to pass a special value to the parameter.
    /// It is used byt library functions to get additional runtime information.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ImportValueAttribute : Attribute
    {
        /// <summary>
        /// Value to be imported.
        /// </summary>
        public enum ValueSpec
        {
            /// <summary>
            /// Not used.
            /// </summary>
            Error = 0,

            /// <summary>
            /// Current class context.
            /// The parameter must be of type <see cref="RuntimeTypeHandle"/>, <see cref="PhpTypeInfo"/> or <see cref="string"/>.
            /// </summary>
            CallerClass,

            /// <summary>
            /// Current late static bound class (<c>static</c>).
            /// The parameter must be of type <see cref="PhpTypeInfo"/>.
            /// </summary>
            CallerStaticClass,

            /// <summary>
            /// Calue of <c>$this</c> variable or <c>null</c> if variable is not defined.
            /// The parameter must be of type <see cref="object"/>.
            /// </summary>
            This,

            /// <summary>
            /// Provides a reference to the array of local PHP variables.
            /// The parameter must be of type <see cref="PhpArray"/>.
            /// </summary>
            Locals,

            /// <summary>
            /// Provides callers parameters.
            /// The parameter must be of type array of <see cref="PhpValue"/>.
            /// </summary>
            CallerArgs,

            /// <summary>
            /// Provides reference to the current script container.
            /// The parameter must be of type <see cref="RuntimeTypeHandle"/>.
            /// </summary>
            CallerScript,
        }

        public ValueSpec Value { get; }

        public ImportValueAttribute(ValueSpec value)
        {
            this.Value = value;
        }
    }

    /// <summary>
    /// Dummy value, used for special generated .ctor symbols so they have a different signature than the regular .ctor.
    /// </summary>
    public struct DummyFieldsOnlyCtor { }

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

    /// <summary>
    /// Attribute denoting that associated value cannot be <c>null</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class NotNullAttribute : Attribute
    {

    }

    /// <summary>
    /// Attribute specifying the parameter default if cannot be stored in standard metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field)]
    public sealed class DefaultValueAttribute : Attribute
    {
        /// <summary>
        /// The type containing the backing field.
        /// <c>Null</c> indicates the containing type.
        /// </summary>
        public Type? ExplicitType { get; set; }

        /// <summary>
        /// Name of the backing field.
        /// </summary>
        public string FieldName { get; private set; }

        public DefaultValueAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }

    /// <summary>
	/// Marks arguments having by-value argument pass semantics and data of the value can be changed by a callee.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class PhpRwAttribute : Attribute
    {
    }

    /// <summary>
    /// Annotates the DLL uses functions from given static class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ImportPhpFunctionsAttribute : Attribute
    {
        /// <summary>
        /// The type containing exported functions.
        /// </summary>
        public Type ContainerType { get; private set; }

        public ImportPhpFunctionsAttribute(Type tcontainer)
        {
            ContainerType = tcontainer;
        }
    }

    /// <summary>
    /// Annotates the DLL uses constants from given static class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ImportPhpConstantsAttribute : Attribute
    {
        /// <summary>
        /// The type containing exported constants.
        /// </summary>
        public Type ContainerType { get; private set; }

        public ImportPhpConstantsAttribute(Type tcontainer)
        {
            ContainerType = tcontainer;
        }
    }

    /// <summary>
    /// Annotates the DLL imports given type as a PHP type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ImportPhpTypeAttribute : Attribute
    {
        /// <summary>
        /// The imported type.
        /// </summary>
        public Type ImportedType { get; private set; }

        public ImportPhpTypeAttribute(Type tsymbol)
        {
            ImportedType = tsymbol;
        }
    }

    /// <summary>
    /// Reference to another PHP library which scripts and types has to be loaded into the context during runtime.
    /// </summary>
    public sealed class PhpPackageReferenceAttribute : Attribute
    {
        /// <summary>
        /// The Script type from the dependent assembly.
        /// </summary>
        public Type ScriptType { get; }

        public PhpPackageReferenceAttribute(Type scriptType)
        {
            ScriptType = scriptType;
        }
    }
}
