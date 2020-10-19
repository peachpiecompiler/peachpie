using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
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

        public PhpValue __call(Context ctx, string name, PhpArray arguments) => this.CallOnInner(ctx, name, arguments);

        #endregion
    }

    /// <summary>
    /// Allows iterating over a <see cref="RecursiveIterator"/> to generate an ASCII graphic tree.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class RecursiveTreeIterator : RecursiveIteratorIterator, OuterIterator
    {
        #region Constants

        public const int BYPASS_CURRENT = 4;
        public const int BYPASS_KEY = 8;
        public const int PREFIX_LEFT = 0;
        public const int PREFIX_MID_HAS_NEXT = 1;
        public const int PREFIX_MID_LAST = 2;
        public const int PREFIX_END_HAS_NEXT = 3;
        public const int PREFIX_END_LAST = 4;
        public const int PREFIX_RIGHT = 5;

        private const int PREFIX_MAX = PREFIX_RIGHT;

        #endregion

        #region Fields

        protected Context _ctx; // NOTE: well-known field pattern, ignored by runtime and used by compiler if needed

        private string[] _prefix =
        {
            "",     // PREFIX_LEFT
            "| ",   // PREFIX_MID_HAS_NEXT
            "  ",   // PREFIX_MID_LAST
            "|-",   // PREFIX_END_HAS_NEXT
            "\\-",  // PREFIX_END_LAST
            ""      // PREFIX_RIGHT
        };
        private string _postfix = "";

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected RecursiveTreeIterator(Context ctx)
        {
            _ctx = ctx;
        }

        public RecursiveTreeIterator(Context ctx, Traversable it, int flags = BYPASS_KEY, int cit_flags = CachingIterator.CATCH_GET_CHILD, int mode = SELF_FIRST) :
            this(ctx)
        {
            __construct(it, flags, cit_flags, mode);
        }

        public sealed override void __construct(Traversable iterator, int mode = 0, int flags = 0)
        {
            __construct(iterator, BYPASS_KEY, CATCH_GET_CHILD, mode);
        }

        public virtual void __construct(Traversable it, int flags = BYPASS_KEY, int cit_flags = CachingIterator.CATCH_GET_CHILD, int mode = SELF_FIRST)
        {
            if (it is IteratorAggregate ia)
            {
                it = ia.getIterator();
            }

            if (!(it is Iterator))
            {
                PhpException.InvalidArgument(nameof(it));
            }

            _flags = flags;

            var cachingIt = new RecursiveCachingIterator(_ctx, (Iterator)it, cit_flags);
            base.__construct(cachingIt, mode);
        }

        #endregion

        /// <summary>
        /// Sets a part of the prefix used in the graphic tree.
        /// </summary>
        /// <param name="part">One of the RecursiveTreeIterator::PREFIX_* constants.</param>
        /// <param name="prefix">The value to assign to the part of the prefix specified in <paramref name="part"/>.</param>
        public virtual void setPrefixPart(int part, string prefix)
        {
            if (part < 0 || part > PREFIX_MAX)
            {
                throw new OutOfRangeException();
            }

            _prefix[part] = prefix;
        }

        /// <summary>
        /// Gets the string to place in front of current element.
        /// </summary>
        public virtual string getPrefix()
        {
            var result = new StringBuilder(_prefix[PREFIX_LEFT]);

            int depth = getDepth();
            bool hasNext;
            for (int i = 0; i < depth; ++i)
            {
                hasNext = ((CachingIterator)getSubIterator(i)).hasNext();
                result.Append(_prefix[hasNext ? PREFIX_MID_HAS_NEXT : PREFIX_MID_LAST]);
            }

            hasNext = ((CachingIterator)getSubIterator(depth)).hasNext();
            result.Append(_prefix[hasNext ? PREFIX_END_HAS_NEXT : PREFIX_END_LAST]);

            result.Append(_prefix[PREFIX_RIGHT]);
            return result.ToString();
        }

        /// <summary>
        /// Sets postfix as used in <see cref="getPostfix"/>.
        /// </summary>
        public virtual void setPostfix(string postfix) => _postfix = postfix;

        /// <summary>
        /// Gets the string to place after the current element.
        /// </summary>
        public virtual string getPostfix() => _postfix;

        /// <summary>
        /// Gets the part of the tree built for the current element.
        /// </summary>
        public virtual string getEntry()
        {
            var current = getInnerIterator().current();
            return current.IsArray ? "Array" : current.ToString(_ctx);
        }

        /// <summary>
        /// Gets the current element prefixed and postfixed.
        /// </summary>
        public override PhpValue current()
        {
            if ((_flags & BYPASS_CURRENT) != 0)
            {
                return getSubIterator(getDepth()).current();
            }

            return getPrefix() + getEntry() + _postfix;
        }

        /// <summary>
        /// Gets the current key prefixed and postfixed.
        /// </summary>
        public override PhpValue key()
        {
            var key = getSubIterator(getDepth()).key();
            if ((_flags & BYPASS_KEY) != 0)
            {
                return key;
            }

            return getPrefix() + key.ToString(_ctx) + _postfix;
        }
    }
}
