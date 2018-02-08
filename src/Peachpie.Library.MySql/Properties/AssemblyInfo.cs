using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTrademark("")]

// annotates this library as a php extension,
// all its public static methods with compatible signatures will be seen as global functions to php scope
[assembly: Pchp.Core.PhpExtension("mysql", "mysqli")]
