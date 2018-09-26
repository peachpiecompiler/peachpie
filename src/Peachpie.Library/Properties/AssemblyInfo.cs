using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pchp.Library.Phar;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Peachpie.Library")]
[assembly: AssemblyTrademark("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("6b01edaf-56bd-4302-98fd-edd81fa2617e")]

// annotates this library as a php extension,
// all its public static methods with compatible signatures will be seen as global functions to php scope
[assembly: Pchp.Core.PhpExtension("standard", "Core", "session", "ctype", "tokenizer", "date", "pcre", "ereg", "json", "hash", "SPL", "filter")]
[assembly: Pchp.Core.PhpExtension(PharExtension.ExtensionName, Registrator = typeof(PharExtension))]
