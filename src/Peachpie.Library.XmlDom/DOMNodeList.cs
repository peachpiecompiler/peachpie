using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;

// TODO: Enable multiple simultaneous iterations

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName)]
    public class DOMNodeList : Traversable, Iterator
    {
        #region Fields and Properties

        /// <summary>
        /// List of the nodes.
        /// </summary>
        private List<DOMNode>/*!*/ _list;

        /// <summary>
        /// Current element index.
        /// </summary>
        private int _element;

        /// <summary>
        /// The number of nodes in the list. The range of valid child node indices is 0 to length - 1 inclusive.
        /// </summary>
        public int length => _list.Count;

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
            if (index < 0 || index >= _list.Count) return null;
            return _list[index];
        }

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
    }
}
