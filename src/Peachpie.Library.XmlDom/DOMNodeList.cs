using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Core.Resources;

// TODO: Enable multiple simultaneous iterations

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMNodeList : Traversable, Iterator, ArrayAccess
    {
        #region Fields and Properties

        /// <summary>
        /// List of the nodes.
        /// </summary>
        [PhpHidden]
        readonly List<DOMNode>/*!*/ _list;

        /// <summary>
        /// Current element index.
        /// </summary>
        [PhpHidden]
        int _element;

        /// <summary>
        /// The number of nodes in the list. The range of valid child node indices is 0 to length - 1 inclusive.
        /// </summary>
        public int length => _list.Count;

        /// <summary>
        /// Get number of nodes in the list.
        /// Alias to <see cref="length"/>/
        /// </summary>
        public int count() => _list.Count;

        #endregion

        #region Construction

        public DOMNodeList()
        {
            _list = new List<DOMNode>();
            _element = 0;
        }

        #endregion

        #region Item access

        internal void AppendNode(DOMNode/*!*/ node)
        {
            Debug.Assert(node != null);
            _list.Add(node);
        }

        /// <summary>
        /// Retrieves a node specified by an index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The node or <B>NULL</B> if the <paramref name="index"/> is invalid.</returns>
        public DOMNode item(int index)
        {
            if (!IsIndexValid(index))
            {
                return null;
            }

            return _list[index];
        }

        private bool IsIndexValid(int index) => index >= 0 && index < _list.Count;

        #endregion

        #region Iterator

        void Iterator.rewind()
        {
            _element = 0;
        }

        void Iterator.next()
        {
            _element++;
        }

        bool Iterator.valid() => _element < _list.Count;

        PhpValue Iterator.key() => PhpValue.Create(_element);

        PhpValue Iterator.current() => PhpValue.FromClass(_list[_element]);

        #endregion

        #region ArrayAccess

        PhpValue ArrayAccess.offsetGet(PhpValue offset)
        {
            if (!TryConvertOffset(offset, out int index))
            {
                return PhpValue.Null;
            }

            return (item(index) is DOMNode node) ? PhpValue.FromClass(node) : PhpValue.Null;
        }

        void ArrayAccess.offsetSet(PhpValue offset, PhpValue value)
        {
            // Only read array access is permitted
            PhpException.Throw(PhpError.Error, ErrResources.object_used_as_array, nameof(DOMNodeList));
        }

        void ArrayAccess.offsetUnset(PhpValue offset)
        {
            // Only read array access is permitted
            PhpException.Throw(PhpError.Error, ErrResources.object_used_as_array, nameof(DOMNodeList));
        }

        bool ArrayAccess.offsetExists(PhpValue offset)
        {
            if (!TryConvertOffset(offset, out int index))
            {
                return false;
            }

            return IsIndexValid(index);
        }

        private bool TryConvertOffset(PhpValue offset, out int index)
        {
            if (offset.ToNumber(out var number).HasFlag(Pchp.Core.Convert.NumberInfo.Unconvertible))
            {
                index = -1;
                return false;
            }

            // "3.14" -> 3
            index = (int)number.ToLong();
            return true;
        }

        #endregion
    }
}
