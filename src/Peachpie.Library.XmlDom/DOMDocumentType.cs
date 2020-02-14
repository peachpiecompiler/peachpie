using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Each <see cref="DOMDocument"/> has a doctype attribute whose value is either NULL
    /// or a <see cref="DOMDocumentType"/> object.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMDocumentType : DOMNode
    {
        #region Fields and Properties

        internal XmlDocumentType XmlDocumentType
        {
            get { return (XmlDocumentType)XmlNode; }
            set { XmlNode = value; }
        }

        private string _qualifiedName;
        private string _publicId;
        private string _systemId;

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.DocumentType"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.DocumentType;

        /// <summary>
        /// Returns the name of this document type.
        /// </summary>
        public string name => this.nodeName;

        /// <summary>
        /// Returns a map of the entities declared by this document type.
        /// </summary>
        public DOMNamedNodeMap entities
        {
            get
            {
                DOMNamedNodeMap map = new DOMNamedNodeMap();

                foreach (XmlNode entity in XmlDocumentType.Entities)
                {
                    var node = DOMNode.Create(entity);
                    if (node != null) map.AddNode(node);
                }

                return map;
            }
        }

        /// <summary>
        /// Returns a map of the entities declared by this document type.
        /// </summary>
        public DOMNamedNodeMap notations
        {
            get
            {
                DOMNamedNodeMap map = new DOMNamedNodeMap();

                foreach (XmlNode notation in XmlDocumentType.Notations)
                {
                    var node = DOMNode.Create(notation);
                    if (node != null) map.AddNode(node);
                }

                return map;
            }
        }

        /// <summary>
        /// Returns the value of the public identifier of this document type.
        /// </summary>
        public string publicId => XmlDocumentType?.PublicId ?? _publicId;

        /// <summary>
        /// Gets the value of the system identifier on this document type.
        /// </summary>
        public string systemId => XmlDocumentType?.SystemId ?? _systemId;

        /// <summary>
        /// Gets the value of the DTD internal subset on this document type.
        /// </summary>
        public string internalSubset => XmlDocumentType?.InternalSubset;

        #endregion

        #region Construction

        public DOMDocumentType()
        { }

        internal DOMDocumentType(XmlDocumentType/*!*/ xmlDocumentType)
        {
            this.XmlDocumentType = xmlDocumentType;
        }

        internal DOMDocumentType(string qualifiedName, string publicId, string systemId)
        {
            this._qualifiedName = qualifiedName;
            this._publicId = publicId;
            this._systemId = systemId;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMDocumentType(XmlDocumentType);
            else return new DOMDocumentType(this._qualifiedName, this._publicId, this._systemId);
        }

        #endregion

        #region Hierarchy

        internal protected override void Associate(XmlDocument document)
        {
            if (!IsAssociated)
            {
                XmlDocumentType = document.CreateDocumentType(_qualifiedName, _publicId, _systemId, null);
            }
        }

        #endregion
    }
}
