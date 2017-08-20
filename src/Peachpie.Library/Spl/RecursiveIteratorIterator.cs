using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    [PhpType(PhpTypeAttribute.InheritName)]
    public class RecursiveIteratorIterator : OuterIterator
    {
        #region Fields

        internal protected /*readonly*/ Iterator _iterator;
        internal protected int _maxDepth = -1;
        internal protected /*readonly*/ int _flags;
        internal protected readonly List<Iterator> _iterators = new List<Iterator>();

        internal protected IEnumerator<KeyValuePair<PhpValue, PhpValue>> _enumerator;
        internal protected bool _valid;

        #endregion

        #region Helpers

        internal protected int level => (_iterators.Count > 0) ? (_iterators.Count - 1) : (0);
        internal protected Iterator GetIterator() => _iterators.Count != 0 ? _iterators[_iterators.Count - 1] : _iterator;
        internal protected Iterator GetIterator(int level)
        {
            if (level == 0) return _iterator;
            if (level > 0 && level < _iterators.Count) return _iterators[level];
            return null;
        }

        internal protected IEnumerator<KeyValuePair<PhpValue, PhpValue>> EnsureEnumerator()
        {
            if (_enumerator == null)
            {
                _enumerator = Enumerate();
                _valid = _enumerator.MoveNext();
            }

            //
            return _enumerator;
        }

        internal protected IEnumerator<KeyValuePair<PhpValue, PhpValue>> Enumerate()
        {
            // rewind if necessary

            if (_iterators.Count != 0)
            {
                // pop level if any
                while (_iterators.Count > 1)
                {
                    endChildren();
                    _iterators.RemoveAt(_iterators.Count - 1);
                }
            }
            else
            {
                // initialize level 0
                _iterators.Add(_iterator);
            }

            var it = _iterator;

            // start iteration

            _iterator.rewind(); // rewind level 0 iterator
            beginIteration();


            for (; ; )
            {
                if (it.valid())
                {
                    if (callHasChildren() && (_maxDepth < 0 || _maxDepth > level))
                    {
                        if ((_flags & SELF_FIRST) != 0)
                        {
                            nextElement();
                            yield return it.KeyValuePair();
                        }

                        var r = callGetChildren();
                        if (r != null)
                        {
                            _iterators.Add(r);
                            r.rewind();
                            beginChildren();
                            it = r;
                        }
                        else
                        {
                            it.next();
                        }
                    }
                    else
                    {
                        nextElement();
                        yield return it.KeyValuePair();
                        it.next();
                    }
                }
                else if (level == 0)
                {
                    break;
                }
                else
                {
                    endChildren();
                    _iterators.RemoveAt(_iterators.Count - 1);
                    it = GetIterator();

                    if ((_flags & CHILD_FIRST) != 0)
                    {
                        nextElement();
                        yield return it.KeyValuePair();
                    }

                    it.next();
                }
            }

            endIteration();
        }

        #endregion

        /// <summary>
        /// The default. Lists only leaves in iteration.
        /// </summary>
        public const int LEAVES_ONLY = 0;

        /// <summary>
        /// Lists leaves and parents in iteration with parents coming first.
        /// </summary>
        public const int SELF_FIRST = 1;

        /// <summary>
        /// Lists leaves and parents in iteration with leaves coming first.
        /// </summary>
        public const int CHILD_FIRST = 2;

        /// <summary>
        /// Whether to ignore exceptions thrown in calls to RecursiveIteratorIterator::getChildren().
        /// </summary>
        public const int CATCH_GET_CHILD = 16;

        [PhpFieldsOnlyCtor]
        protected RecursiveIteratorIterator() { }

        public RecursiveIteratorIterator(Traversable iterator, int mode = LEAVES_ONLY, int flags = 0)
        {
            __construct(iterator, mode, flags);
        }

        public virtual void __construct(Traversable iterator, int mode = LEAVES_ONLY, int flags = 0)
        {
            _iterator = (Iterator)iterator ?? throw new ArgumentNullException(nameof(iterator));
            _flags = mode | flags;
        }

        public virtual void beginIteration() { }
        public virtual void endIteration() { }
        public virtual void beginChildren() { }
        public virtual void endChildren() { }
        public virtual void nextElement() { }

        public virtual RecursiveIterator callGetChildren()
        {
            try
            {
                return GetIterator() is RecursiveIterator r ? r.getChildren() : null;

                // TODO: zend_throw_exception(spl_ce_UnexpectedValueException, "Objects returned by RecursiveIterator::getChildren() must implement RecursiveIterator", 0);
            }
            catch
            {
                if ((_flags & CATCH_GET_CHILD) == 0)
                {
                    throw;
                }
            }

            return null;
        }

        public virtual bool callHasChildren()
        {
            return GetIterator() is RecursiveIterator r && r.hasChildren();
        }

        public virtual int getDepth() => level;
        [return: CastToFalse] // -1 => FALSE
        public virtual long getMaxDepth() => _maxDepth;
        public virtual void setMaxDepth(int max_depth = -1)
        {
            if (max_depth < -1)
            {
                throw new OutOfRangeException("Parameter max_depth must be >= -1");
            }

            _maxDepth = max_depth;
        }
        public virtual Iterator getSubIterator(int level = -1) => GetIterator(level < 0 ? this.level : level);

        #region OuterIterator

        public virtual Iterator getInnerIterator() => GetIterator();

        public virtual void rewind()
        {
            _enumerator = null;
            EnsureEnumerator();
        }

        public virtual void next()
        {
            if (_enumerator == null)
            {
                EnsureEnumerator();
            }
            else if (_valid)
            {
                _valid = _enumerator.MoveNext();
            }
        }

        public virtual bool valid()
        {
            EnsureEnumerator();
            return _valid;
        }

        public virtual PhpValue key()
        {
            EnsureEnumerator();
            return _valid ? _enumerator.Current.Key : PhpValue.Null;
        }

        public virtual PhpValue current()
        {
            EnsureEnumerator();
            return _valid ? _enumerator.Current.Value : PhpValue.Null;
        }

        #endregion
    }
}
