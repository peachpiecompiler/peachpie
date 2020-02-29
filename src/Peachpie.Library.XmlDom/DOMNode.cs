using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMNode : IPhpPrintable
    {
        #region Fields and Properties

        /// <summary>
        /// Overrides default debug print behavior.
        /// </summary>
        IEnumerable<KeyValuePair<string, PhpValue>> IPhpPrintable.Properties
        {
            get
            {
                // only public properties
                // remove duplicates
                // ignore properties referring to another DOMNode (https://github.com/peachpiecompiler/peachpie/issues/658)

                var set = new HashSet<string>();

                var fields = TypeMembersUtils.EnumerateInstanceFields(
                    instance: this,
                    keyFormatter: TypeMembersUtils.s_propertyName,
                    keyFormatter2: TypeMembersUtils.s_keyToString,
                    predicate: p => p.IsPublic && set.Add(p.PropertyName) && !typeof(DOMNode).IsAssignableFrom(p.PropertyType)
                );

                // TODO: properties containing DOMNode should be listed, but with a dummy value "(object value omitted)"

                return fields;
            }
        }

        [PhpHidden]
        private protected XmlNode _xmlNode;

        [PhpHidden]
        internal protected XmlNode XmlNode
        {
            get
            {
                if (_xmlNode == null) DOMException.Throw(ExceptionCode.InvalidState);
                return _xmlNode;
            }
            set
            {
                _xmlNode = value;
            }
        }

        [PhpHidden]
        internal protected bool IsAssociated => _xmlNode != null;

        /// <summary>
        /// Returns the name of the node (exact meaning depends on the particular subtype).
        /// </summary>
        public virtual string nodeName => XmlNode.Name;

        /// <summary>
        /// Returns or sets the value of the node (exact meaning depends on the particular subtype).
        /// </summary>
        public virtual string nodeValue
        {
            get { return XmlNode.Value; }
            set { XmlNode.Value = value; }
        }

        /// <summary>
        /// Returns the type of the node (to be overriden).
        /// </summary>
        public virtual int nodeType
        {
            get
            {
                if (!IsAssociated) PhpException.Throw(PhpError.Warning, Resources.InvalidStateError);
                else PhpException.Throw(PhpError.Warning, Resources.InvalidNodeType);
                return 0;   // Unreachable
            }
        }

        /// <summary>
        /// Returns the parent of the node.
        /// </summary>
        public DOMNode parentNode
        {
            get
            {
                if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
                return Create(XmlNode.ParentNode);
            }
        }

        /// <summary>
        /// Returns all children of the node.
        /// </summary>
        [NotNull]
        public DOMNodeList childNodes
        {
            get
            {
                var list = new DOMNodeList();
                if (this is DOMDocument doc)
                {
                    // DOMDocument always ignores white nodes and XmlDeclaration
                    // returning just the single root node
                    var node = Create(doc.XmlDocument?.DocumentElement);
                    if (node != null)
                    {
                        list.AppendNode(node);
                    }
                }
                else if (IsAssociated || GetType() == typeof(DOMNode))
                {
                    foreach (XmlNode child in XmlNode.ChildNodes)
                    {
                        var node = Create(child);
                        if (node != null)
                        {
                            list.AppendNode(node);
                        }
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// Returns the first child of the node.
        /// </summary>
        public DOMNode firstChild
        {
            get
            {
                // according to "childNodes"

                if (this is DOMDocument doc)
                {
                    // always the single root node or NULL
                    return Create(doc.XmlDocument?.DocumentElement);
                }
                else if (IsAssociated || GetType() == typeof(DOMNode))
                {
                    // convert first node to DOMNode,
                    // skip eventual XmlDeclaration(s):

                    for (var n = XmlNode.FirstChild; n != null; n = n.NextSibling)
                    {
                        var dn = Create(n);
                        if (dn != null)
                        {
                            return dn;
                        }
                    }
                }

                //
                return null;
            }
        }

        /// <summary>
        /// Returns the last child of the node.
        /// </summary>
        public DOMNode lastChild
        {
            get
            {
                if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
                return Create(XmlNode.LastChild);
            }
        }

        /// <summary>
        /// Returns the previous sibling of the node.
        /// </summary>
        public DOMNode previousSibling
        {
            get
            {
                if (!IsAssociated && GetType() != typeof(DOMNode)) return null;
                return Create(XmlNode.PreviousSibling);
            }
        }

        /// <summary>
        /// Returns the next sibling of the node.
        /// </summary>
        public DOMNode nextSibling
        {
            get
            {
                if (!IsAssociated && GetType() != typeof(DOMNode))
                {
                    return null;
                }

                return Create(XmlNode.NextSibling);
            }
        }

        /// <summary>
        /// Returns a map of attributes of this node (overriden in <see cref="DOMElement"/>).
        /// </summary>
        public virtual DOMNamedNodeMap attributes => null;

        /// <summary>
        /// This function returns the document the current node belongs to.
        /// </summary>
        public DOMDocument ownerDocument => (DOMDocument)Create(XmlNode.OwnerDocument);

        /// <summary>
        /// Returns the namespace URI of the node.
        /// </summary>
        public virtual string namespaceURI
        {
            get
            {
                string uri = XmlNode.NamespaceURI;
                return (uri.Length == 0 ? null : uri);
            }
        }

        /// <summary>
        /// Returns or sets the namespace prefix of the node.
        /// </summary>
        public string prefix
        {
            get
            {
                if (IsAssociated) return XmlNode.Prefix;

                Utils.ParseQualifiedName(nodeName, out var prefix, out _);

                return prefix;
            }
            set
            {
                XmlNode.Prefix = value;
            }
        }

        /// <summary>
        /// Returns the local name of the node.
        /// </summary>
        public string localName
        {
            get
            {
                if (IsAssociated)
                {
                    return XmlNode.LocalName;
                }

                Utils.ParseQualifiedName(nodeName, out _, out var local_name);

                return local_name;
            }
        }

        /// <summary>
        /// Returns the base URI of the node.
        /// </summary>
        public string baseURI
        {
            get
            {
                if (!IsAssociated && GetType() != typeof(DOMNode))
                {
                    return null;
                }

                return XmlNode.BaseURI;
            }
        }

        /// <summary>
        /// Returns or sets the text content of the node.
        /// </summary>
        public string textContent
        {
            get { return XmlNode.InnerText; }
            set { XmlNode.InnerText = value; }
        }

        #endregion

        #region Construction

        [PhpHidden]
        internal protected static DOMNode Create(XmlNode xmlNode)
        {
            if (xmlNode == null) return null;
            switch (xmlNode.NodeType)
            {
                case XmlNodeType.Attribute: return new DOMAttr((XmlAttribute)xmlNode);
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace: return new DOMText((XmlCharacterData)xmlNode); // also see XmlDocument.PreserveWhitespace
                case XmlNodeType.CDATA: return new DOMCdataSection((XmlCDataSection)xmlNode);
                case XmlNodeType.Comment: return new DOMComment((XmlComment)xmlNode);
                case XmlNodeType.Document: return new DOMDocument((XmlDocument)xmlNode);
                case XmlNodeType.DocumentFragment: return new DOMDocumentFragment((XmlDocumentFragment)xmlNode);
                case XmlNodeType.DocumentType: return new DOMDocumentType((XmlDocumentType)xmlNode);
                case XmlNodeType.Element: return new DOMElement((XmlElement)xmlNode);
                case XmlNodeType.Entity: return new DOMEntity((XmlEntity)xmlNode);
                case XmlNodeType.EntityReference: return new DOMEntityReference((XmlEntityReference)xmlNode);
                case XmlNodeType.Notation: return new DOMNotation((XmlNotation)xmlNode);
                case XmlNodeType.ProcessingInstruction: return new DOMProcessingInstruction((XmlProcessingInstruction)xmlNode);
                case XmlNodeType.Text: return new DOMText((XmlText)xmlNode);

                case XmlNodeType.XmlDeclaration:
                default:
                    return null;
            }
        }

        [PhpHidden]
        private protected virtual DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            DOMException.Throw(ExceptionCode.InvalidState);
            return null;
        }

        #endregion

        #region Internal dump routine

        //private IEnumerable<KeyValuePair<VariableName, AttributedValue>> PropertyIteratorHelper()
        //{
        //    return base.PropertyIterator();
        //}

        //protected override IEnumerable<KeyValuePair<VariableName, AttributedValue>> PropertyIterator()
        //{
        //    foreach (KeyValuePair<VariableName, AttributedValue> pair in PropertyIteratorHelper())
        //    {
        //        // filter out "linking" properties to avoid an endless dump :)
        //        switch (pair.Key.ToString())
        //        {
        //            case "parentNode":
        //            case "childNodes":
        //            case "firstChild":
        //            case "lastChild":
        //            case "previousSibling":
        //            case "nextSibling":
        //            case "ownerDocument":
        //            case "documentElement": continue;

        //            default: yield return pair; break;
        //        }
        //    }
        //}

        #endregion

        #region Hierarchy

        [PhpHidden]
        internal protected virtual void Associate(XmlDocument/*!*/ document)
        {
        }

        private protected delegate XmlNode NodeAction(DOMNode/*!*/ newNode, DOMNode auxNode);

        /// <summary>
        /// Performs a child-adding action with error checks.
        /// </summary>
        [PhpHidden]
        private protected XmlNode CheckedChildOperation(DOMNode/*!*/ newNode, DOMNode auxNode, NodeAction/*!*/ action)
        {
            newNode.Associate(XmlNode.OwnerDocument != null ? XmlNode.OwnerDocument : (XmlDocument)XmlNode);

            // check for readonly node
            if (XmlNode.IsReadOnly || (newNode.XmlNode.ParentNode != null && newNode.XmlNode.ParentNode.IsReadOnly))
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return null;
            }

            // check for owner document mismatch
            if (XmlNode.OwnerDocument != null ?
                XmlNode.OwnerDocument != newNode.XmlNode.OwnerDocument :
                XmlNode != newNode.XmlNode.OwnerDocument)
            {
                DOMException.Throw(ExceptionCode.WrongDocument);
                return null;
            }

            XmlNode result;
            try
            {
                result = action(newNode, auxNode);
            }
            catch (InvalidOperationException)
            {
                // the current node is of a type that does not allow child nodes of the type of the newNode node
                // or the newNode is an ancestor of this node. 
                DOMException.Throw(ExceptionCode.BadHierarchy);
                return null;
            }
            catch (ArgumentException)
            {
                // check for newNode == this which System.Xml reports as ArgumentException
                if (newNode.XmlNode == XmlNode) DOMException.Throw(ExceptionCode.BadHierarchy);
                else
                {
                    // the refNode is not a child of this node
                    DOMException.Throw(ExceptionCode.NotFound);
                }
                return null;
            }

            return result;
        }

        /// <summary>
        /// Adds a new child before a reference node.
        /// </summary>
        /// <param name="newNode">The new node.</param>
        /// <param name="refNode">The reference node. If not supplied, <paramref name="newNode"/> is appended
        /// to the children.</param>
        /// <returns>The inserted node.</returns>
        [return: CastToFalse]
        public virtual DOMNode insertBefore(DOMNode newNode, DOMNode refNode = null)
        {
            bool is_fragment;
            if (newNode is DOMDocumentFragment)
            {
                if (!newNode.IsAssociated || !newNode.XmlNode.HasChildNodes)
                {
                    PhpException.Throw(PhpError.Warning, Resources.DocumentFragmentEmpty);
                    return null;
                }
                is_fragment = true;
            }
            else is_fragment = false;

            XmlNode result = CheckedChildOperation(newNode, refNode, delegate (DOMNode _newNode, DOMNode _refNode)
            {
                return XmlNode.InsertBefore(_newNode.XmlNode, (_refNode == null ? null : _refNode.XmlNode));
            });

            if (result == null) return null;
            if (is_fragment) return Create(result);
            else return newNode;
        }

        /// <summary>
        /// Replaces a child node.
        /// </summary>
        /// <param name="newNode">The new node.</param>
        /// <param name="oldNode">The old node.</param>
        /// <returns>The inserted node.</returns>
        [return: CastToFalse]
        public virtual DOMNode replaceChild(DOMNode newNode, DOMNode oldNode)
        {
            XmlNode result = CheckedChildOperation(newNode, oldNode, delegate (DOMNode _newNode, DOMNode _oldNode)
            {
                return XmlNode.ReplaceChild(_newNode.XmlNode, _oldNode.XmlNode);
            });

            if (result == null) return null;
            if (newNode is DOMDocumentFragment) return Create(result);
            else return newNode;
        }

        /// <summary>
        /// Adds a new child at the end of the children.
        /// </summary>
        /// <param name="newNode">The node to add.</param>
        /// <returns>The node added.</returns>
        [return: CastToFalse]
        public virtual DOMNode appendChild(DOMNode newNode)
        {
            bool is_fragment;
            if (newNode is DOMDocumentFragment)
            {
                if (!newNode.IsAssociated || !newNode.XmlNode.HasChildNodes)
                {
                    PhpException.Throw(PhpError.Warning, Resources.DocumentFragmentEmpty);
                    return null;
                }
                is_fragment = true;
            }
            else is_fragment = false;

            XmlNode result = CheckedChildOperation(newNode, null, delegate (DOMNode _newNode, DOMNode _)
            {
                return XmlNode.AppendChild(_newNode.XmlNode);
            });

            if (result == null) return null;
            if (is_fragment) return Create(result);
            else return newNode;
        }

        /// <summary>
        /// Removes a child from the list of children.
        /// </summary>
        /// <param name="oldNode">The node to remove.</param>
        /// <returns>The removed node.</returns>
        [return: CastToFalse]
        public virtual DOMNode removeChild(DOMNode oldNode)
        {
            // check for readonly node
            if (XmlNode.IsReadOnly)
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return null;
            }

            try
            {
                XmlNode.RemoveChild(oldNode.XmlNode);
            }
            catch (ArgumentException)
            {
                DOMException.Throw(ExceptionCode.NotFound);
                return null;
            }

            return oldNode;
        }

        /// <summary>
        /// Checks if the node has children.
        /// </summary>
        /// <returns><B>True</B> if this node has children, <B>false</B> otherwise.</returns>
        public virtual bool hasChildNodes() => XmlNode.HasChildNodes;

        /// <summary>
        /// Checks if the node has attributes.
        /// </summary>
        /// <returns><B>True</B> if this node has attributes, <B>false</B> otherwise.</returns>
        public virtual bool hasAttributes()
        {
            XmlAttributeCollection attrs = XmlNode.Attributes;
            return (attrs != null && attrs.Count > 0);
        }

        #endregion

        #region Namespaces

        /// <summary>
        /// Gets the namespace prefix of the node based on the namespace URI.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns>The prefix of the namespace or <B>null</B>.</returns>
        public virtual string lookupPrefix(string namespaceUri) => XmlNode.GetPrefixOfNamespace(namespaceUri);

        /// <summary>
        /// Gets the namespace URI of the node based on the prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns>The namespace URI or <B>null</B>.</returns>
        public virtual string lookupNamespaceUri(string prefix) => XmlNode.GetNamespaceOfPrefix(prefix);

        /// <summary>
        /// Determines whether the given URI is the default namespace.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual bool isDefaultNamespace(string namespaceUri)
        {
            if (namespaceUri.Length > 0)
            {
                return (XmlNode.GetPrefixOfNamespace(namespaceUri).Length == 0);
            }
            else return false;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Normalizes the node.
        /// </summary>
        public virtual void normalize() => XmlNode.Normalize();

        /// <summary>
        /// Creates a copy of the node.
        /// </summary>
        /// <param name="deep">Indicates whether to copy all descendant nodes. This parameter is
        /// defaulted to <B>false</B>.</param>
        /// <returns>The cloned node.</returns>
        public virtual DOMNode cloneNode(bool deep = false)
        {
            if (IsAssociated) return Create(XmlNode.CloneNode(deep));
            else return CloneObjectInternal(deep);
        }

        /// <summary>
        /// Indicates if two nodes are the same node.
        /// </summary>
        /// <param name="anotherNode">The other node.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual bool isSameNode(DOMNode anotherNode) => (XmlNode == anotherNode.XmlNode);

        /// <summary>
        /// Checks if a feature is supported for the specified version.
        /// </summary>
        /// <param name="feature">The feature to test.</param>
        /// <param name="version">The version number of the <paramref name="feature"/> to test.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual bool isSupported(string feature, string version) => XmlNode.Supports(feature, version);

        #endregion

        #region Not implemented

        /// <summary>
        /// Canonicalize nodes to a string.
        /// </summary>
        /// <param name="exclusive">Enable exclusive parsing of only the nodes matched by
        /// the provided xpath or namespace prefixes.</param>
        /// <param name="with_comments">Retain comments in output.</param>
        /// <param name="xpath">An array of xpaths to filter the nodes by.</param>
        /// <param name="ns_prefixes">An array of namespace prefixes to filter the nodes by.</param>
        /// <returns>Returns canonicalized nodes as a string or FALSE on failure.</returns>
        [return: CastToFalse]
        public string C14N(
            bool exclusive = false,
            bool with_comments = false,
            PhpArray xpath = null,
            PhpArray ns_prefixes = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Canonicalize nodes to a file.
        /// </summary>
        /// <param name="uri">Path to write the output to.</param>
        /// <param name="exclusive">Enable exclusive parsing of only the nodes matched by
        /// the provided xpath or namespace prefixes.</param>
        /// <param name="with_comments">Retain comments in output.</param>
        /// <param name="xpath">An array of xpaths to filter the nodes by.</param>
        /// <param name="ns_prefixes">An array of namespace prefixes to filter the nodes by.</param>
        /// <returns>Number of bytes written or FALSE on failure </returns>
        [return: CastToFalse]
        public int? C14NFile(
            string uri,
            bool exclusive = false,
            bool with_comments = false,
            PhpArray xpath = null,
            PhpArray ns_prefixes = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets line number for where the node is defined. 
        /// </summary>
        /// <returns>Always returns the line number where the node was defined in.</returns>
        public int getLineNo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets an XPath location path for the node.
        /// </summary>
        /// <returns>Returns a <see cref="string"/> containing the XPath, or NULL in case of an error. </returns>
        [return: CastToFalse]
        public string getNodePath()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
