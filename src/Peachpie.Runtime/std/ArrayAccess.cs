using System.ComponentModel;
using Pchp.Core;

/// <summary>
/// Interface to provide accessing objects as arrays.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
[PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
public interface ArrayAccess
{
    /// <summary>
    /// Returns the value at specified offset.
    /// </summary>
    PhpValue offsetGet(PhpValue offset);

    /// <summary>
    /// Assigns a value to the specified offset.
    /// </summary>
    void offsetSet(PhpValue offset, PhpValue value);

    /// <summary>
    /// Unsets an offset.
    /// </summary>
    void offsetUnset(PhpValue offset);

    /// <summary>
    /// Whether an offset exists.
    /// </summary>
    /// <remarks>This method is executed when using isset() or empty().</remarks>
    bool offsetExists(PhpValue offset);
}
