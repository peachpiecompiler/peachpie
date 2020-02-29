using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMElement : DOMNode
    {
        #region Fields and Properties

        [PhpHidden]
        internal protected XmlElement XmlElement
        {
            get { return (XmlElement)XmlNode; }
            set { XmlNode = value; }
        }

        [PhpHidden]
        private protected string _name;

        [PhpHidden]
        private protected string _value;

        [PhpHidden]
        private protected string _namespaceUri;

        /// <summary>
        /// Returns the name of the element.
        /// </summary>
        public override string nodeName => (IsAssociated ? base.nodeName : _name);

        /// <summary>
        /// Returns or sets the value (inner text) of the element.
        /// </summary>
        public override string nodeValue
        {
            get
            {
                return (IsAssociated ? XmlNode.InnerText : _value);
            }
            set
            {
                this._value = value;
                if (IsAssociated) XmlNode.InnerText = this._value;
            }
        }

        /// <summary>
        /// Returns the namespace URI of the node.
        /// </summary>
        public override string namespaceURI => (IsAssociated ? base.namespaceURI : _namespaceUri);

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.Element"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.Element;

        /// <summary>
        /// Returns a map of attributes of this node (see <see cref="DOMNamedNodeMap"/>).
        /// </summary>
        public override DOMNamedNodeMap attributes
        {
            get
            {
                DOMNamedNodeMap map = new DOMNamedNodeMap();
                if (IsAssociated)
                {
                    foreach (XmlAttribute attr in XmlNode.Attributes)
                    {
                        var node = Create(attr);
                        if (node != null) map.AddNode(node);
                    }
                }
                return map;
            }
        }

        /// <summary>
        /// Returns the tag name.
        /// </summary>
        public string tagName => this.nodeName;

        /// <summary>
        /// Not implemented in PHP 7.1.1, always returns NULL.
        /// </summary>
        public PhpValue schemaTypeInfo => PhpValue.Null;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DOMElement()
        { }

        public DOMElement(string name, string value = null, string namespaceUri = null)
        {
            __construct(name, value, namespaceUri);
        }

        internal DOMElement(XmlElement/*!*/ xmlElement)
        {
            this.XmlElement = xmlElement;
        }

        [PhpHidden]
        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMElement(XmlElement);
            else
            {
                DOMElement copy = new DOMElement();
                copy.__construct(this._name, this._value, this._namespaceUri);
                return copy;
            }
        }

        public virtual void __construct(string name, string value = null, string namespaceUri = null)
        {
            // just save up the name, value, and ns URI for later XmlElement construction
            this._name = name;
            this._value = value;
            this._namespaceUri = namespaceUri;
        }

        #endregion

        #region Hierarchy

        [PhpHidden]
        internal protected override void Associate(XmlDocument/*!*/ document)
        {
            if (!IsAssociated)
            {
                XmlElement elem = document.CreateElement(_name, _namespaceUri);
                if (_value != null) elem.InnerText = _value;

                XmlElement = elem;
            }
        }

        #endregion

        #region Attributes

        /// <summary>
        /// Returns the value of an attribute.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <returns>The attribute value or empty string.</returns>
        public virtual string getAttribute(string name)
        {
            if (IsAssociated)
            {
                XmlAttribute attr = XmlElement.Attributes[name];
                if (attr != null) return attr.Value;
            }

            return String.Empty;
        }

        /// <summary>
        /// Returns the value of an attribute.
        /// </summary>
        /// <param name="namespaceUri">The attribute namespace URI.</param>
        /// <param name="localName">The attribute local name.</param>
        /// <returns>The attribute value or empty string.</returns>
        public virtual string getAttributeNS(string namespaceUri, string localName)
        {
            if (IsAssociated)
            {
                XmlAttribute attr = XmlElement.Attributes[localName, namespaceUri];
                if (attr != null) return attr.Value;
            }

            return String.Empty;
        }

        /// <summary>
        /// Sets the value of a attribute (creates new one if it does not exist).
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The attribute value.</param>
        /// <returns><B>True</B> on success, <B>false</B> on failure.</returns>
        public virtual bool setAttribute(string name, string value)
        {
            if (!IsAssociated)
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return false;
            }

            XmlAttribute attr = XmlElement.Attributes[name];
            if (attr == null)
            {
                attr = XmlNode.OwnerDocument.CreateAttribute(name);
                XmlElement.Attributes.Append(attr);
            }
            attr.Value = value;

            return true;
        }

        /// <summary>
        /// Sets the value of a attribute (creates new one if it does not exist).
        /// </summary>
        /// <param name="namespaceUri">The attribute namespace URI.</param>
        /// <param name="qualifiedName">The attribute qualified name.</param>
        /// <param name="value">The attribute value.</param>
        /// <returns><B>True</B> on success, <B>false</B> on failure.</returns>
        public virtual bool setAttributeNS(string namespaceUri, string qualifiedName, string value)
        {
            if (!IsAssociated)
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return false;
            }

            if (string.IsNullOrEmpty(qualifiedName))
            {
                // Warning: DOMElement::setAttributeNS(): Attribute Name is required
                PhpException.InvalidArgument(nameof(qualifiedName));
                return false;
            }

            // parse the qualified name
            Utils.ParseQualifiedName(qualifiedName, out var prefix, out var local_name);

            var attr = XmlElement.Attributes[local_name, namespaceUri];
            if (attr == null)
            {
                attr = XmlNode.OwnerDocument.CreateAttribute(qualifiedName, namespaceUri);
                XmlElement.Attributes.Append(attr);
            }
            else
            {
                attr.Prefix = prefix;
            }

            attr.Value = value;

            return true;
        }

        /// <summary>
        /// Removes an attribute.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <returns><B>True</B> on success, <B>false</B> on failure.</returns>
        public virtual bool removeAttribute(string name)
        {
            if (!IsAssociated)
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return false;
            }

            var attr = XmlElement.Attributes[name];
            if (attr != null) XmlElement.Attributes.Remove(attr);

            return true;
        }

        /// <summary>
        /// Removes an attribute.
        /// </summary>
        /// <param name="namespaceUri">The attribute namespace URI.</param>
        /// <param name="localName">The attribute local name.</param>
        /// <returns><B>True</B> on success, <B>false</B> on failure.</returns>
        public virtual bool removeAttributeNS(string namespaceUri, string localName)
        {
            if (!IsAssociated)
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return false;
            }

            var attr = XmlElement.Attributes[localName, namespaceUri];
            if (attr != null) XmlElement.Attributes.Remove(attr);

            return true;
        }

        /// <summary>
        /// Returns an attribute node.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <returns>The attribute or <B>false</B>.</returns>
        [return: CastToFalse]
        public virtual DOMAttr getAttributeNode(string name)
        {
            if (IsAssociated)
            {
                var attr = XmlElement.Attributes[name];
                if (attr != null) return new DOMAttr(attr);
            }
            return null;
        }

        /// <summary>
        /// Returns an attribute node.
        /// </summary>
        /// <param name="namespaceUri">The attribute namespace URI.</param>
        /// <param name="localName">The attribute local name.</param>
        /// <returns>The attribute or <B>false</B>.</returns>
        [return: CastToFalse]
        public virtual DOMAttr getAttributeNodeNS(string namespaceUri, string localName)
        {
            if (IsAssociated)
            {
                var attr = XmlElement.Attributes[localName, namespaceUri];
                if (attr != null) return new DOMAttr(attr);
            }
            return null;
        }

        /// <summary>
        /// Adds new attribute node to the element.
        /// </summary>
        /// <param name="attribute">The attribute node.</param>
        /// <returns>Old node if the attribute has been replaced or <B>null</B>.</returns>
        public virtual DOMAttr setAttributeNode(DOMAttr attribute)
        {
            if (!IsAssociated)
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return null;
            }

            attribute.Associate(XmlElement.OwnerDocument);

            if (XmlNode.OwnerDocument != attribute.XmlNode.OwnerDocument)
            {
                DOMException.Throw(ExceptionCode.WrongDocument);
                return null;
            }

            var attr = XmlElement.Attributes[attribute.nodeName];
            if (attr != null)
            {
                XmlElement.Attributes.Remove(attr);
                XmlElement.Attributes.Append(attribute.XmlAttribute);
                return new DOMAttr(attr);
            }
            else
            {
                XmlElement.Attributes.Append(attribute.XmlAttribute);
                return null;
            }
        }

        /// <summary>
        /// Adds new attribute node to the element.
        /// </summary>
        /// <param name="attribute">The attribute node.</param>
        /// <returns>Old node if the attribute has been replaced or <B>null</B>.</returns>
        public virtual DOMAttr setAttributeNodeNS(DOMAttr attribute)
        {
            if (!IsAssociated)
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return null;
            }

            attribute.Associate(XmlElement.OwnerDocument);

            if (XmlNode.OwnerDocument != attribute.XmlNode.OwnerDocument)
            {
                DOMException.Throw(ExceptionCode.WrongDocument);
                return null;
            }

            var attr = XmlElement.Attributes[attribute.localName, attribute.namespaceURI];
            if (attr != null)
            {
                XmlElement.Attributes.Remove(attr);
                XmlElement.Attributes.Append(attribute.XmlAttribute);
                return new DOMAttr(attr);
            }
            else
            {
                XmlElement.Attributes.Append(attribute.XmlAttribute);
                return null;
            }
        }

        /// <summary>
        /// Removes an attribute node from the element.
        /// </summary>
        /// <param name="attribute">The attribute node.</param>
        /// <returns>Old node if the attribute has been removed or <B>null</B>.</returns>
        public virtual DOMAttr removeAttributeNode(DOMAttr attribute)
        {
            if (!IsAssociated)
            {
                DOMException.Throw(ExceptionCode.DomModificationNotAllowed);
                return null;
            }

            var attr = XmlElement.Attributes[attribute.nodeName];
            if (attr == null)
            {
                DOMException.Throw(ExceptionCode.NotFound);
                return null;
            }

            XmlElement.Attributes.Remove(attr);

            return attribute;
        }

        /// <summary>
        /// Checks whether an attribute exists.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual bool hasAttribute(string name)
        {
            if (IsAssociated)
            {
                if (XmlElement.Attributes[name] != null) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether an attribute exists.
        /// </summary>
        /// <param name="namespaceUri">The attribute namespace URI.</param>
        /// <param name="localName">The attribute local name.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual bool hasAttributeNS(string namespaceUri, string localName)
        {
            if (IsAssociated)
            {
                if (XmlElement.Attributes[localName, namespaceUri] != null) return true;
            }
            return false;
        }

        #endregion

        #region Child elements

        /// <summary>
        /// Gets all descendant elements with the matching tag name.
        /// </summary>
        /// <param name="name">The tag name. Use <B>*</B> to return all elements within the element tree.</param>
        /// <returns>A <see cref="DOMNodeList"/>.</returns>
        public virtual DOMNodeList getElementsByTagName(string name)
        {
            DOMNodeList list = new DOMNodeList();

            if (IsAssociated)
            {
                // enumerate elements in the default namespace
                foreach (XmlNode node in XmlElement.GetElementsByTagName(name))
                {
                    var dom_node = DOMNode.Create(node);
                    if (dom_node != null) list.AppendNode(dom_node);
                }

                // enumerate all namespaces
                XPathNavigator navigator = XmlElement.CreateNavigator();
                XPathNodeIterator iterator = navigator.Select("//namespace::*[not(. = ../../namespace::*)]");

                while (iterator.MoveNext())
                {
                    string prefix = iterator.Current.Name;
                    if (!String.IsNullOrEmpty(prefix) && prefix != "xml")
                    {
                        // enumerate elements in this namespace
                        foreach (XmlNode node in XmlElement.GetElementsByTagName(name, iterator.Current.Value))
                        {
                            var dom_node = DOMNode.Create(node);
                            if (dom_node != null) list.AppendNode(dom_node);
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Gets all descendant elements with the matching namespace URI and local name.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="localName">The local name. Use <B>*</B> to return all elements within the element tree.</param>
        /// <returns>A <see cref="DOMNodeList"/>.</returns>
        public virtual DOMNodeList getElementsByTagNameNS(string namespaceUri, string localName)
        {
            DOMNodeList list = new DOMNodeList();

            if (IsAssociated)
            {
                foreach (XmlNode node in XmlElement.GetElementsByTagName(localName, namespaceUri))
                {
                    var dom_node = DOMNode.Create(node);
                    if (dom_node != null) list.AppendNode(dom_node);
                }
            }

            return list;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Not implemented in PHP 5.1.6.
        /// </summary>
        public virtual void setIdAttribute(string name, bool isId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented in PHP 5.1.6.
        /// </summary>
        public virtual void setIdAttributeNS(string namespaceUri, string localName, bool isId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented in PHP 5.1.6.
        /// </summary>
        public virtual void setIdAttributeNode(DOMAttr attribute, bool isId)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
