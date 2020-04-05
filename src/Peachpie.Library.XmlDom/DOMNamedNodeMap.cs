using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;

// TODO: Enable multiple simultaneous iterations

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMNamedNodeMap : Traversable, Iterator
    {
        #region Nested struct: MapKey

        private protected readonly struct MapKey : IEquatable<MapKey>
        {
            public readonly string NamespaceUri;
            public readonly string/*!*/ LocalName;

            public MapKey(string namespaceUri, string/*!*/ localName)
            {
                Debug.Assert(localName != null);

                this.NamespaceUri = namespaceUri;
                this.LocalName = localName;
            }

            public override int GetHashCode()
            {
                int code = LocalName.GetHashCode();
                if (NamespaceUri != null) code ^= NamespaceUri.GetHashCode();
                return code;
            }

            #region IEquatable<MapKey> Members

            public bool Equals(MapKey other)
            {
                return (NamespaceUri == other.NamespaceUri && LocalName == other.LocalName);
            }

            #endregion
        }

        #endregion

        #region CLR Fields

        /// <summary>
        /// Hash map of the names to the nodex.
        /// </summary>
        private protected Dictionary<MapKey, DOMNode>/*!*/ _map;

        /// <summary>
        /// List of the nodes.
        /// </summary>
        private protected List<DOMNode>/*!*/ _list;

        /// <summary>
        /// Current element index.
        /// </summary>
        private protected int _element;

        #endregion

        #region Properties

        /// <summary>
        /// The number of nodes in the map. The range of valid child node indices is 0 to <see cref="length"/> - 1 inclusive.
        /// </summary>
        public int length => _map.Count;

        /// <summary>
        /// The number of nodes in the map.
        /// </summary>
        public int count() => _map.Count;

        #endregion

        #region Construction

        public DOMNamedNodeMap()
        {
            _map = new Dictionary<MapKey, DOMNode>();
            _list = new List<DOMNode>();
            _element = 0;
        }

        #endregion

        #region Item access

        internal protected void AddNode(DOMNode/*!*/ node)
        {
            Debug.Assert(node != null);

            _map.Add(new MapKey(node.namespaceURI, node.localName), node);
            _list.Add(node);
        }

        /// <summary>
        /// Retrieves a node specified by name.
        /// </summary>
        /// <param name="name">The (local) name of the node to retrieve.</param>
        /// <returns>A node with the specified (local) node name or <B>null</B> if no node is found.</returns>
        public DOMNode getNamedItem(string name)
        {
            if (name == null) return null;

            // try null namespace first
            if (_map.TryGetValue(new MapKey(null, name), out var item)) return item;

            // iterate and take the first that fits
            foreach (var pair in _map)
            {
                if (pair.Key.LocalName == name) return pair.Value;
            }

            return null;
        }

        /// <summary>
        /// Not implemented in PHP 5.1.6.
        /// </summary>
        public DOMNode setNamedItem(DOMNode item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented in PHP 5.1.6.
        /// </summary>
        public DOMNode removeNamedItem(string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves a node specified by an index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The node or <B>null</B> if <paramref name="index"/> is invalid.</returns>
        public DOMNode item(int index) => (index < 0 || index >= _list.Count) ? null : _list[index];

        /// <summary>
        /// Retrieves a node specified by local name and namespace URI.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="localName">The local name.</param>
        /// <returns>A node with the specified local name and namespace URI, or <B>null</B> if no node is found.</returns>
        public DOMNode getNamedItemNS(string namespaceUri, string localName)
        {
            if (localName == null) return null;

            if (_map.TryGetValue(new MapKey(namespaceUri, localName), out var item)) return item;
            else return null;
        }

        /// <summary>
        /// Not implemented in PHP 5.1.6.
        /// </summary>
        public DOMNode setNamedItemNS(DOMNode item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented in PHP 5.1.6.
        /// </summary>
        public DOMNode removeNamedItemNS(string namespaceUri, string localName)
        {
            throw new NotImplementedException();
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

        PhpValue Iterator.key() => PhpValue.Create(_list[_element].localName);

        PhpValue Iterator.current() => PhpValue.FromClass(_list[_element]);

        #endregion
    }
}
