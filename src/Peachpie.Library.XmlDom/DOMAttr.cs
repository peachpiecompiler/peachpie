using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Represents an attribute in the <see cref="DOMElement"/> object. 
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMAttr : DOMNode
    {
        #region Fields and Properties

        protected internal XmlAttribute XmlAttribute
        {
            get { return (XmlAttribute)XmlNode; }
            set { XmlNode = value; }
        }

        private string _name;
        private string _value;

        /// <summary>
        /// Returns the name of the attribute.
        /// </summary>
        public override string nodeName => (IsAssociated ? base.nodeName : _name);

        /// <summary>
        /// Returns or sets the value of the attribute.
        /// </summary>
        public override string nodeValue
        {
            get
            {
                return (IsAssociated ? base.nodeValue : _value);
            }
            set
            {
                this._value = value;
                if (IsAssociated) base.nodeValue = this._value;
            }
        }

        /// <summary>
        /// Returns the namespace URI of the attribute.
        /// </summary>
        public override string namespaceURI => (IsAssociated ? base.namespaceURI : null);

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.Attribute"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.Attribute;

        /// <summary>
        /// Returns the name of the attribute.
        /// </summary>
        public string name => this.nodeName;

        /// <summary>
        /// Returns or sets the value of this attribute
        /// </summary>
        public string value
        {
            get { return (string)this.nodeValue; }
            set { this.nodeValue = value; }
        }

        /// <summary>
        /// Always returns <B>true</B> as in PHP 5.1.6.
        /// </summary>
        public bool specified => true;

        /// <summary>
        /// Returns the <see cref="DOMElement"/> to which this attribute belongs.
        /// </summary>
        public DOMElement ownerElement => (IsAssociated ? (DOMElement)Create(XmlAttribute.OwnerElement) : null);

        /// <summary>
        /// Not implemented in PHP 5.1.6.
        /// </summary>
        public object schemaTypeInfo => null;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DOMAttr()
        { }

        internal DOMAttr(XmlAttribute/*!*/ xmlAttribute)
        {
            this.XmlAttribute = xmlAttribute;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMAttr(XmlAttribute);
            else
            {
                DOMAttr copy = new DOMAttr();
                copy.__construct(this._name, this._value);
                return copy;
            }
        }

        public DOMAttr(string name, string value = null)
        {
            __construct(name, value);
        }

        /// <summary>
        /// Initializes a new <see cref="DOMAttr"/> object.
        /// </summary>
        public virtual void __construct(string name, string value = null)
        {
            // just save up the name and value for later XmlAttribute construction
            this._name = name;
            this._value = value;
        }

        #endregion

        #region Hierarchy

        internal protected override void Associate(XmlDocument/*!*/ document)
        {
            if (!IsAssociated)
            {
                XmlAttribute attr = document.CreateAttribute(_name);
                if (_value != null) attr.Value = _value;

                XmlAttribute = attr;
            }
        }

        #endregion

        #region Validation

        ///// <summary>
        ///// Checks if attribute is a defined ID.
        ///// </summary>
        ///// <returns><B>True</B> or <B>false</B>.</returns>
        //public bool isId()
        //{
        //    IXmlSchemaInfo schema_info = XmlNode.SchemaInfo;
        //    if (schema_info != null)
        //    {
        //        return (schema_info.SchemaType.TypeCode == XmlTypeCode.Id);
        //    }
        //    else return false;
        //}

        #endregion
    }
}
