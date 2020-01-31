using System.ComponentModel;
using Pchp.Core;

/// <summary>
/// Interface for external iterators or objects that can iterate themselves internally.
/// </summary>
/// <remarks>
/// Note that contrary to the .NET framework enumerating interfaces,
/// calling <c>rewind</c> positions the iterator on the first element, so <c>next</c>
/// shall not be called until the first element is retrieved.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
[PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
public interface Iterator : Traversable
{
    /// <summary>
    /// Rewinds the iterator to the first element.
    /// </summary>
    void rewind();

    /// <summary>
    /// Moves forward to next element.
    /// </summary>
    void next();

    /// <summary>
    /// Checks if there is a current element after calls to <see cref="rewind"/> or <see cref="next"/>.
    /// </summary>
    /// <returns><c>bool</c>.</returns>
    bool valid();

    /// <summary>
    /// Returns the key of the current element.
    /// </summary>
    PhpValue key();

    /// <summary>
    /// Returns the current element (value).
    /// </summary>
    PhpValue current();
}

/// <summary>
/// Interface to create an external iterator.
/// </summary>
/// <remarks>
/// This interface contains only arg-less stubs as signatures should not be restricted.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
[PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
public interface IteratorAggregate : Traversable
{
    /// <summary>
    /// Returns an <see cref="Iterator"/> or another <see cref="IteratorAggregate"/> for
    /// the implementing object.
    /// </summary>
    Traversable getIterator();
}