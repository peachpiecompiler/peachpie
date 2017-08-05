using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Pchp.Core.Reflection;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// The Seekable iterator.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
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
    [PhpType(PhpTypeAttribute.InheritName)]
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
    [PhpType(PhpTypeAttribute.InheritName)]
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
    [PhpType(PhpTypeAttribute.InheritName)]
    public class ArrayIterator : Iterator, Traversable, ArrayAccess, SeekableIterator, Countable
    {
        #region Fields & Properties

        readonly protected Context _ctx;

        PhpArray _array;// PHP compatibility: private PhpArray storage;
        internal protected OrderedDictionary.Enumerator _arrayEnumerator;    // lazily instantiated so we can rewind() once when needed
        bool isArrayIterator => _array != null;

        object _dobj = null;
        IEnumerator<KeyValuePair<IntStringKey, PhpValue>> _dobjEnumerator = null;    // lazily instantiated so we can rewind() once when needed
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

            _dobjEnumerator = TypeMembersUtils.EnumerateVisibleInstanceFields(_dobj).GetEnumerator();   // we have to create new enumerator (or implement InstancePropertyIterator.Reset)
            _isValid = _dobjEnumerator.MoveNext();
        }

        #endregion

        #region Constructor

        public ArrayIterator(Context/*!*/ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            _ctx = ctx;
        }

        public ArrayIterator(Context/*!*/ctx, PhpValue array, int flags = 0)
            : this(ctx)
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
            else if ((_dobj = array.AsObject()) != null)
            {
                //InitObjectIteratorHelper();   // lazily to avoid one additional allocation
            }
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
                return _array.DeepCopy();

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
            if (isArrayIterator)
            {
                // arrayEnumerator initialized in __construct()
            }
            else
            {
                if (isObjectIterator && _dobjEnumerator == null)
                    InitObjectIteratorHelper();
            }
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
                _isValid = _dobjEnumerator.MoveNext();
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
                    return PhpValue.Create(_dobjEnumerator.Current.Key);
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
                    return _dobjEnumerator.Current.Value;
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
            {
                return _array.GetItemValue(index);
            }
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
            {
                return index.TryToIntStringKey(out var iskey) && _array.ContainsKey(iskey);
            }
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
            else if (isObjectIterator)
                return TypeMembersUtils.FieldsCount(_dobj);

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
    /// This iterator allows to unset and modify values and keys while iterating over Arrays and Objects in the same way
    /// as the <see cref="ArrayIterator"/>. Additionally it is possible to iterate over the current iterator entry.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class RecursiveArrayIterator : ArrayIterator, RecursiveIterator
    {
        // TODO: Add and use the CHILD_ARRAYS_ONLY constant when flags are functional

        [PhpFieldsOnlyCtor]
        protected RecursiveArrayIterator(Context/*!*/ctx) : base(ctx)
        {
        }

        public RecursiveArrayIterator(Context/*!*/ctx, PhpValue array, int flags = 0)
            : this(ctx)
        {
            __construct(ctx, array, flags);
        }

        public RecursiveArrayIterator getChildren()
        {
            if (!valid())
            {
                return null;
            }

            var elem = current();
            var type = GetType();
            if (type == typeof(RecursiveArrayIterator))
            {
                return new RecursiveArrayIterator(_ctx, elem);
            }
            else
            {
                // We create an instance of the current type, if used from a subclass
                return (RecursiveArrayIterator)_ctx.Create(default(RuntimeTypeHandle), type.GetPhpTypeInfo(), elem);
            }
        }

        RecursiveIterator RecursiveIterator.getChildren() => getChildren();

        public bool hasChildren()
        {
            var elem = current();
            return (elem.IsArray || elem.IsObject) && !elem.IsEmpty;
        }
    }

    /// <summary>
    /// The EmptyIterator class for an empty iterator.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
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

    /// <summary>
    /// This iterator wrapper allows the conversion of anything that is Traversable into an Iterator.
    /// It is important to understand that most classes that do not implement Iterators have reasons
    /// as most likely they do not allow the full Iterator feature set.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class IteratorIterator : OuterIterator
    {
        /// <summary>
        /// Object to iterate on.
        /// </summary>
        internal protected Traversable _iterator;

        /// <summary>
        /// Enumerator over the <see cref="iterator"/>.
        /// </summary>
        protected IPhpEnumerator _enumerator;

        /// <summary>
        /// Wheter the <see cref="_enumerator"/> is in valid state (initialized and not at the end).
        /// </summary>
        internal protected bool _valid = false;

        [PhpFieldsOnlyCtor]
        protected IteratorIterator() { }

        public IteratorIterator(Traversable iterator, string classname = null) => __construct(iterator, classname);

        public virtual void __construct(Traversable iterator, string classname = null)
        {
            if (classname != null)
            {
                //var downcast = ctx.ResolveType(PhpVariable.AsString(classname), null, this.iterator.TypeDesc, null, ResolveTypeFlags.ThrowErrors);

                PhpException.ArgumentValueNotSupported(nameof(classname), classname);
                throw new NotImplementedException();
            }

            _iterator = iterator;
            _valid = false;
            _enumerator = null;

            if (iterator is Iterator)
            {
                // ok
            }
            else
            {
                PhpException.InvalidArgument(nameof(iterator));
            }

            //rewind(context);  // not in PHP, performance reasons (foreach causes rewind() itself)
        }

        public virtual Iterator getInnerIterator()
        {
            return (Iterator)_iterator;
        }

        internal protected void rewindImpl()
        {
            if (_iterator != null)
            {
                // we can make use of standard foreach enumerator
                _enumerator = Operators.GetForeachEnumerator(_iterator, true, default(RuntimeTypeHandle));

                //
                _valid = _enumerator.MoveNext();
            }
        }

        public virtual void rewind()
        {
            rewindImpl();
        }

        public virtual void next()
        {
            // init iterator first (this skips the first element as on PHP)
            if (_enumerator == null)
            {
                rewind();
            }

            // enumerator can be still null, if iterator is null
            _valid = _enumerator != null && _enumerator.MoveNext();
        }

        public virtual bool valid()
        {
            return _valid;
        }

        public virtual PhpValue key()
        {
            return (_enumerator != null && _valid) ? _enumerator.CurrentKey : PhpValue.Void;
        }

        public virtual PhpValue current()
        {
            return (_enumerator != null && _valid) ? _enumerator.CurrentValue : PhpValue.Void;
        }

        // TODO: hide this method to not be visible by PHP code, make this behaviour internal
        //public virtual PhpValue __call(ScriptContext context, object name, object args)
        //{
        //    var methodname = PhpVariable.AsString(name);
        //    var argsarr = args as PhpArray;

        //    if (this.iterator == null || argsarr == null)
        //    {
        //        PhpException.UndefinedMethodCalled(this.TypeName, methodname);
        //        return null;
        //    }

        //    // call the method on internal iterator, as in PHP,
        //    // only PHP leaves $this to self (which is potentionally dangerous and not correctly typed)
        //    context.Stack.AddFrame((ICollection)argsarr.Values);
        //    return this.iterator.InvokeMethod(methodname, null, context);
        //}
    }

    /// <summary>
    /// The InfiniteIterator allows one to infinitely iterate over an iterator without having to manually rewind the iterator upon reaching its end.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class InfiniteIterator : IteratorIterator
    {
        [PhpFieldsOnlyCtor]
        protected InfiniteIterator() { }

        public InfiniteIterator(Traversable iterator) : base(iterator)
        {
        }

        /// <summary>
        /// Moves the inner Iterator forward and rewinds it if underlaying iterator reached the end.
        /// </summary>
        public override void next()
        {
            base.next();

            if (!_valid)
            {
                // as it is in PHP, calls non-overridable implementation of rewind()
                rewindImpl();
            }
        }
    }

    /// <summary>
    /// This abstract iterator filters out unwanted values.
    /// This class should be extended to implement custom iterator filters.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public abstract class FilterIterator : IteratorIterator
    {
        [PhpFieldsOnlyCtor]
        protected FilterIterator() { }

        public FilterIterator(Traversable iterator) : base(iterator)
        {
        }

        public override void rewind()
        {
            base.rewind();
            SkipNotAccepted();
        }

        public override void next()
        {
            base.next();
            SkipNotAccepted();
        }

        private void SkipNotAccepted()
        {
            if (_enumerator != null)
            {
                // skip not accepted elements
                while (_valid && !this.accept())
                {
                    _valid = _enumerator.MoveNext();
                }
            }
        }

        /// <summary>
        /// Returns whether the current element of the iterator is acceptable through this filter.
        /// </summary>
        public abstract bool accept();
    }

    /// <summary>
    /// This abstract iterator filters out unwanted values.
    /// This class should be extended to implement custom iterator filters.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class LimitIterator : IteratorIterator
    {
        internal protected long _position, _offset, _max;

        [PhpFieldsOnlyCtor]
        protected LimitIterator() { }

        public LimitIterator(Traversable iterator, long offset = 0, long count = -1) => __construct(iterator, offset, count);

        public override sealed void __construct(Traversable iterator, string classname = null) => __construct(iterator, 0, -1);

        public virtual void __construct(Traversable iterator, long offset = 0, long count = -1)
        {
            base.__construct(iterator);

            if (offset < 0) throw new OutOfRangeException();

            _offset = offset;
            _max = count >= 0 ? offset + count : long.MaxValue;
        }

        public override void rewind()
        {
            base.rewind();

            // skips offset
            for (var n = _offset; n > 0 && _valid; n--)
            {
                base.next();
            }

            _position = _offset;
        }

        public override void next()
        {
            if (++_position < _max && _valid)
            {
                base.next();
            }
            else
            {
                _valid = false;
            }
        }

        public virtual long getPosition() => _position;

        public virtual long seek(long position)
        {
            if (position < _offset || position >= _max)
            {
                throw new OutOfBoundsException();
            }

            //
            if (position != _position)
            {
                //if (_iterator is SeekableIterator seekable)  // undocumented PHP behavior
                //{
                //    seekable.seek(position);  // TODO: this would not move our _enumerator
                //    _position = position;
                //    _valid = seekable.valid();
                //}
                //else
                {
                    // rewind & forward
                    if (position < _position)
                    {
                        rewind();
                    }

                    // forward
                    while (position > _position && _valid)
                    {
                        next();
                    }
                }
            }

            //
            return _position;
        }
    }

    /// <summary>
    /// An Iterator that iterates over several iterators one after the other.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class AppendIterator : IteratorIterator, OuterIterator
    {
        /// <summary>
        /// Current item in <see cref="_array"/>;
        /// </summary>
        protected internal KeyValuePair<IntStringKey, Iterator> _index;

        [PhpFieldsOnlyCtor]
        protected AppendIterator() { }

        public AppendIterator(Context ctx) => __construct(ctx);

        /// <summary>
        /// Wrapped <see cref="_array"/> into PHP iterator.
        /// Compatibility with <see cref="getArrayIterator"/>.
        /// </summary>
        protected internal ArrayIterator ArrayIterator => (ArrayIterator)_iterator;
        protected internal Iterator InnerIterator => isValidImpl() ? _index.Value : null;

        protected internal void EnsureInitialized()
        {
            if (_index.Value == null && ArrayIterator.valid())
            {
                _index = new KeyValuePair<IntStringKey, Iterator>(ArrayIterator.key().ToIntStringKey(), (Iterator)ArrayIterator.current().AsObject());
            }
        }

        protected internal bool isValidImpl()
        {
            EnsureInitialized();
            return _index.Value != null;
        }

        /// <summary>
        /// Array of appended iterators.
        /// </summary>
        protected internal PhpArray _array;

        //public sealed override void __construct(Traversable iterator, string classname = null)
        //{
        //    throw new InvalidOperationException();
        //}

        public virtual void __construct(Context ctx)
        {
            base.__construct(new ArrayIterator(ctx, (PhpValue)(_array = PhpArray.NewEmpty())));
        }

        public virtual void append(Iterator iterator)
        {
            _array.Add(PhpValue.FromClass(iterator ?? throw new ArgumentNullException(nameof(iterator))));

            if (_array.Count == 1) { rewindImpl(); }
        }

        /// <summary>
        /// This method gets the ArrayIterator that is used to store the iterators added with <see cref="append"/>.
        /// </summary>
        public virtual ArrayIterator getArrayIterator() => ArrayIterator;

        public virtual int getIteratorIndex() => isValidImpl() ? _index.Key.Integer : 0/*should be NULL*/;

        public override Iterator getInnerIterator() => InnerIterator;

        public override void rewind()
        {
            rewindImpl();
            _index = default(KeyValuePair<IntStringKey, Iterator>);
        }

        public override void next()
        {
            if (isValidImpl())
            {
                InnerIterator.next();
            }
            else
            {
                return;
            }

            while (isValidImpl() && InnerIterator.valid() == false)
            {
                // next iterator
                base.next();

                // reset index
                _index = default(KeyValuePair<IntStringKey, Iterator>);
            }
        }

        public override PhpValue current()
        {
            return isValidImpl() ? _index.Value.current() : PhpValue.Void;
        }

        public override PhpValue key()
        {
            return isValidImpl() ? _index.Value.key() : PhpValue.Void;
        }

        public override bool valid() => isValidImpl();
    }

    /// <summary>
    /// This abstract iterator filters out unwanted values for a RecursiveIterator.
    /// This class should be extended to implement custom filters.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public abstract class RecursiveFilterIterator : FilterIterator, RecursiveIterator
    {
        readonly protected Context _ctx;

        [PhpFieldsOnlyCtor]
        protected RecursiveFilterIterator(Context ctx)
        {
            _ctx = ctx;
        }

        public RecursiveFilterIterator(Context ctx, RecursiveIterator iterator) : this(ctx)
        {
            __construct(iterator);
        }

        public override void __construct(Traversable iterator, string classname = null)
        {
            if (!(iterator is RecursiveIterator))
            {
                PhpException.InvalidArgument(nameof(iterator));
            }

            base.__construct(iterator, classname);
        }

        public RecursiveFilterIterator getChildren()
        {
            var childrenIt = ((RecursiveIterator)getInnerIterator()).getChildren();
            object result = _ctx.Create(default(RuntimeTypeHandle), this.GetPhpTypeInfo(), PhpValue.FromClass(childrenIt));

            return (RecursiveFilterIterator)result;
        }

        RecursiveIterator RecursiveIterator.getChildren() => getChildren();

        public bool hasChildren()
        {
            return ((RecursiveIterator)getInnerIterator()).hasChildren();
        }
    }
}