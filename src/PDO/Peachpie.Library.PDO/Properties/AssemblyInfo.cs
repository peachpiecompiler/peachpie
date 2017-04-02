using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTrademark("")]

[assembly: Pchp.Core.PhpExtension("pdo", Registrator = typeof(Peachpie.Library.PDO.PDORegistrator))]