using System.ComponentModel;
using Pchp.Core;

/// <summary>
/// Interface for customized serializing.
/// </summary>
/// <remarks>
/// Classes that implement this interface no longer support __sleep() and __wakeup().
/// The method serialize is called whenever an instance needs to be serialized. This does not invoke __destruct()
/// or has any other side effect unless programmed inside the method.
/// 
/// When the data is unserialized the class is known and the appropriate unserialize() method is called as a constructor instead of calling __construct().
/// If you need to execute the standard constructor you may do so in the method.
/// 
/// Note, that when an old instance of a class that implements this interface now,
/// which had been serialized before the class implemeted the interface, is unserialized,
/// __wakeup() is called instead of the serialize method, what might be useful for migration purposes.
/// </remarks>
[PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
public interface Serializable
{
    /// <summary>
    /// Should return the string representation of the object.
    /// </summary>
    /// <returns>Returns the string representation of the object or <c>NULL</c></returns>
    PhpString serialize();

    /// <summary>
    /// Called during unserialization of the object.
    /// </summary>
    /// <param name="serialized">The string representation of the object.</param>
    void unserialize(PhpString serialized);
}
