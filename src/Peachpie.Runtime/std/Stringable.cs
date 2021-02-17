using System.ComponentModel;
using Pchp.Core;

/// <summary>
/// Built-in interface for classes that implement the __toString().
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
[PhpType(PhpTypeAttribute.InheritName, MinimumLangVersion = "8.0"), PhpExtension("Core")]
public interface Stringable
{
    /// <summary>
    /// Converts object to string.
    /// </summary>
    string __toString();
}
