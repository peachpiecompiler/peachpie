using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

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

/// <summary>
/// This iterator allows to unset and modify values and keys while iterating over Arrays and Objects.
/// 
/// When you want to iterate over the same array multiple times you need to instantiate ArrayObject
/// and let it create ArrayIterator instances that refer to it either by using foreach or by calling
/// its getIterator() method manually.
/// </summary>
public class ArrayIterator : Iterator, Traversable, ArrayAccess, SeekableIterator, Countable
{
    #region Fields & Properties

    readonly protected Context _ctx;

    PhpArray _array;
    OrderedDictionary.Enumerator _arrayEnumerator;    // lazily instantiated so we can rewind() once when needed
    bool isArrayIterator => _array != null;

    object _dobj = null;
    IEnumerator<KeyValuePair<PhpValue, PhpValue>> dobjEnumerator = null;    // lazily instantiated so we can rewind() once when needed
    bool isObjectIterator => _dobj != null;

    bool _isValid = false;

    /// <summary>
    /// Instantiate new PHP array's enumerator and advances its position to the first element.
    /// </summary>
    /// <returns><c>True</c> whether there is an first element.</returns>
    void InitArrayIteratorHelper()
    {
        Debug.Assert(_array != null);

        _arrayEnumerator = new OrderedDictionary.Enumerator(_array);
        _isValid = _arrayEnumerator.MoveFirst();
    }

    /// <summary>
    /// Instantiate new object's enumerator and advances its position to the first element.
    /// </summary>
    /// <returns><c>True</c> whether there is an first element.</returns>
    void InitObjectIteratorHelper()
    {
        Debug.Assert(_dobj != null);

        //this.dobjEnumerator = dobj.InstancePropertyIterator(null, false);   // we have to create new enumerator (or implement InstancePropertyIterator.Reset)
        //this.isValid = this.dobjEnumerator.MoveNext();
        throw new NotImplementedException();
    }

    #endregion

    #region Constructor

    public ArrayIterator(Context/*!*/ctx)
    {
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));
        _ctx = ctx;
    }

    public ArrayIterator(Context/*!*/ctx, PhpValue array, int flags = 0)
        :this(ctx)
    {
        __construct(ctx, array, flags);
    }

    /// <summary>
    /// Constructs the array iterator.
    /// </summary>
    /// <param name="ctx">Runtime context.</param>
    /// <param name="array">The array or object to be iterated on.</param>
    /// <param name="flags">Flags to control the behaviour of the ArrayIterator object. See ArrayIterator::setFlags().</param>
    public virtual void __construct(Context/*!*/ctx, PhpValue array, int flags = 0)
    {
        if ((_array = array.ArrayOrNull()) != null)
        {
            InitArrayIteratorHelper();  // instantiate now, avoid repetitous checks during iteration
        }
        //else if ((this.dobj = array as DObject) != null)
        //{
        //    //InitObjectIteratorHelper();   // lazily to avoid one additional allocation
        //}
        else
        {
            throw new ArgumentException();
            //// throw an PHP.Library.SPL.InvalidArgumentException if anything besides an array or an object is given.
            //Exception.ThrowSplException(
            //    _ctx => new InvalidArgumentException(_ctx, true),
            //    context,
            //    null, 0, null);
        }
    }

    #endregion

    #region ArrayIterator (uasort, uksort, natsort, natcasesort, ksort, asort)

    public virtual void uasort(IPhpCallable cmp_function)
    {
        throw new NotImplementedException();
    }

    public virtual void uksort(IPhpCallable cmp_function)
    {
        throw new NotImplementedException();
    }

    public virtual void natsort()
    {
        throw new NotImplementedException();
    }

    public virtual void natcasesort()
    {
        throw new NotImplementedException();
    }

    public virtual void ksort()
    {
        throw new NotImplementedException();
    }

    public virtual void asort()
    {
        throw new NotImplementedException();
    }

    #endregion

    #region ArrayIterator (getFlags, setFlags, append, getArrayCopy)

    public virtual int getFlags()
    {
        throw new NotImplementedException();
    }

    public virtual object setFlags(int flags)
    {
        throw new NotImplementedException();
    }

    public virtual PhpArray getArrayCopy()
    {
        if (isArrayIterator)
            return new PhpArray(_array);

        throw new NotImplementedException();
    }

    public virtual void append(PhpValue value)
    {
        if (isArrayIterator)
        {
            _array.Add(value);
        }
        else if (isObjectIterator)
        {
            // php_error_docref(NULL TSRMLS_CC, E_RECOVERABLE_ERROR, "Cannot append properties to objects, use %s::offsetSet() instead", Z_OBJCE_P(object)->name);
        }
    }

    #endregion

    #region interface Iterator

    public virtual void rewind()
    {
        if (isArrayIterator)
        {
            _isValid = _arrayEnumerator.MoveFirst();
        }
        else if (isObjectIterator)
        {
            // isValid set by InitObjectIteratorHelper()
            InitObjectIteratorHelper(); // DObject enumerator does not support MoveFirst()
        }
    }

    private void EnsureEnumeratorsHelper()
    {
        if (isObjectIterator && dobjEnumerator == null)
            InitObjectIteratorHelper();

        // arrayEnumerator initialized in __construct()
    }

    public virtual void next()
    {
        if (isArrayIterator)
        {
            _isValid = _arrayEnumerator.MoveNext();
        }
        else if (isObjectIterator)
        {
            EnsureEnumeratorsHelper();
            _isValid = dobjEnumerator.MoveNext();
        }
    }

    public virtual bool valid()
    {
        EnsureEnumeratorsHelper();
        return _isValid;
    }

    public virtual PhpValue key()
    {
        EnsureEnumeratorsHelper();

        if (_isValid)
        {
            if (isArrayIterator)
                return _arrayEnumerator.CurrentKey;
            else if (isObjectIterator)
                return dobjEnumerator.Current.Key;
            else
                Debug.Fail(null);
        }

        return PhpValue.Null;
    }

    public virtual PhpValue current()
    {
        EnsureEnumeratorsHelper();

        if (_isValid)
        {
            if (isArrayIterator)
                return _arrayEnumerator.CurrentValue;
            else if (isObjectIterator)
                return dobjEnumerator.Current.Value;
            else
                Debug.Fail(null);
        }

        return PhpValue.Null;
    }

    #endregion

    #region interface ArrayAccess

    public virtual PhpValue offsetGet(PhpValue index)
    {
        if (isArrayIterator)
            return _array[index.ToIntStringKey()];
        //else if (isObjectIterator)
        //    return _dobj[index];

        return PhpValue.False;
    }

    public virtual void offsetSet(PhpValue index, PhpValue value)
    {
        if (isArrayIterator)
        {
            if (index != null) _array.Add(index, value);
            else _array.Add(value);
        }
        //else if (isObjectIterator)
        //{
        //    _dobj.Add(index, value);
        //}
    }

    public virtual void offsetUnset(PhpValue index)
    {
        throw new NotImplementedException();
    }

    public virtual bool offsetExists(PhpValue index)
    {
        if (isArrayIterator)
            return _array.ContainsKey(index.ToIntStringKey());
        //else if (isObjectIterator)
        //    return _dobj.Contains(index);

        return false;
    }

    #endregion

    #region interface SeekableIterator

    public void seek(long position)
    {
        int currentPosition = 0;
        
        if (position < 0)
        {
            //
        }

        this.rewind();

        while (this.valid() && currentPosition < position)
        {
            this.next();
            currentPosition++;
        }
    }

    #endregion

    #region interface Countable

    public virtual long count()
    {
        if (isArrayIterator)
            return _array.Count;
        //else if (isObjectIterator)
        //    return _dobj.Count;

        return 0;
    }

    #endregion

    #region interface Serializable

    public virtual PhpString serialize()
    {
        throw new NotImplementedException();
    }

    public virtual void unserialize(PhpString data)
    {
        throw new NotImplementedException();
    }

    #endregion
}

/// <summary>
/// The EmptyIterator class for an empty iterator.
/// </summary>
public class EmptyIterator : Iterator, Traversable
{
    public virtual void __construct()
    {
    }

    #region interface Iterator

    public void rewind()
    {
    }

    public void next()
    {
    }

    public virtual bool valid() => false;

    public virtual PhpValue key()
    {
        Debug.Fail("not implemented");
        //Exception.ThrowSplException(
        //    _ctx => new BadMethodCallException(_ctx, true),
        //    context,
        //    CoreResources.spl_empty_iterator_key_access, 0, null);
        return PhpValue.Null;
    }

    public virtual PhpValue current()
    {
        Debug.Fail("not implemented");
        //Exception.ThrowSplException(
        //    _ctx => new BadMethodCallException(_ctx, true),
        //    context,
        //    CoreResources.spl_empty_iterator_value_access, 0, null);
        return PhpValue.Null;
    }

    #endregion
}