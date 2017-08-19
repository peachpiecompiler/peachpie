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

        internal protected Traversable _iterator;
        internal protected int _maxDepth = -1;
        internal protected int _flags;
        internal protected readonly List<Iterator> _iterators = new List<Iterator>();

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
            _iterator = iterator;
            _flags = mode | flags;
        }

        public virtual void beginChildren() { }
        public virtual void beginIteration() { }
        public virtual void endChildren() { }
        public virtual void endIteration() { }

        public virtual RecursiveIterator callGetChildren() { throw new NotImplementedException(); }
        public virtual bool callHasChildren() { throw new NotImplementedException(); }

        public virtual int getDepth() { throw new NotImplementedException(); }
        public virtual long getMaxDepth() { throw new NotImplementedException(); }
        public virtual void setMaxDepth(long max_depth = -1) { throw new NotImplementedException(); }
        public virtual RecursiveIterator getSubIterator(long level = -1) { throw new NotImplementedException(); }

        #region OuterIterator

        public Iterator getInnerIterator()
        {
            throw new NotImplementedException();
        }

        public void rewind()
        {
            throw new NotImplementedException();
        }

        public void next()
        {
            throw new NotImplementedException();
        }

        public bool valid()
        {
            throw new NotImplementedException();
        }

        public PhpValue key()
        {
            throw new NotImplementedException();
        }

        public PhpValue current()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
