using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Built-in marker interface.
/// </summary>
public interface Traversable
{
}

/// <summary>
/// Interface for external iterators or objects that can iterate themselves internally.
/// </summary>
/// <remarks>
/// Note that contrary to the .NET framework enumerating interfaces,
/// calling <c>rewind</c> positions the iterator on the first element, so <c>next</c>
/// shall not be called until the first element is retrieved.
/// </remarks>
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
/// The Seekable iterator.
/// </summary>
public interface SeekableIterator : Iterator
{
    /// <summary>
    /// Seeks to a given position in the iterator.
    /// </summary>
    void seek(long position);
}

/// <summary>
/// Interface to create an external iterator.
/// </summary>
/// <remarks>
/// This interface contains only arg-less stubs as signatures should not be restricted.
/// </remarks>
public interface IteratorAggregate : Traversable
{
    /// <summary>
    /// Returns an <see cref="Iterator"/> or another <see cref="IteratorAggregate"/> for
    /// the implementing object.
    /// </summary>
    Traversable getIterator();
}

/// <summary>
/// Classes implementing OuterIterator can be used to iterate over iterators.
/// </summary>
public interface OuterIterator : Iterator
{
    /// <summary>
    /// Returns the inner iterator for the current iterator entry.
    /// </summary>
    /// <returns>The inner <see cref="Iterator"/> for the current entry.</returns>
    Iterator getInnerIterator();
}

/// <summary>
/// Classes implementing RecursiveIterator can be used to iterate over iterators recursively.
/// </summary>
public interface RecursiveIterator : Iterator
{
    /// <summary>
    /// Returns an iterator for the current iterator entry.
    /// </summary>
    /// <returns>An <see cref="RecursiveIterator"/> for the current entry.</returns>
    RecursiveIterator getChildren();

    /// <summary>
    /// Returns if an iterator can be created for the current entry.
    /// </summary>
    /// <returns>Returns TRUE if the current entry can be iterated over, otherwise returns FALSE.</returns>
    bool hasChildren();
}
