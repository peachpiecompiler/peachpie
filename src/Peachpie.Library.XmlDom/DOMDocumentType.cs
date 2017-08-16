using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

// TODO: Enable XmlDocumentType and its usage when available (netstandard2.0)

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName)]
    public class DOMDocumentType : DOMNode
    {
        #region Fields and Properties

        //internal XmlDocumentType XmlDocumentType
        //{
        //    get { return (XmlDocumentType)XmlNode; }
        //    set { XmlNode = value; }
        //}

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
                //DOMNamedNodeMap map = new DOMNamedNodeMap();

                //foreach (XmlNode entity in XmlDocumentType.Entities)
                //{
                //    var node = DOMNode.Create(entity);
                //    if (node != null) map.AddNode(node);
                //}

                //return map;
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns a map of the entities declared by this document type.
        /// </summary>
        public DOMNamedNodeMap notations
        {
            get
            {
                //DOMNamedNodeMap map = new DOMNamedNodeMap();

                //foreach (XmlNode notation in XmlDocumentType.Notations)
                //{
                //    var node = DOMNode.Create(notation);
                //    if (node != null) map.AddNode(node);
                //}

                //return map;
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns the value of the public identifier of this document type.
        /// </summary>
        public string publicId => throw new NotImplementedException(); //XmlDocumentType?.PublicId ?? _publicId;

        /// <summary>
        /// Gets the value of the system identifier on this document type.
        /// </summary>
        public string systemId => throw new NotImplementedException(); //XmlDocumentType?.SystemId ?? _systemId;

        /// <summary>
        /// Gets the value of the DTD internal subset on this document type.
        /// </summary>
        public string internalSubset => throw new NotImplementedException(); //XmlDocumentType?.InternalSubset;

        #endregion

        #region Construction

        public DOMDocumentType()
        { }

        //internal DOMDocumentType(XmlDocumentType/*!*/ xmlDocumentType)
        //{
        //    this.XmlDocumentType = xmlDocumentType;
        //}

        internal DOMDocumentType(string qualifiedName, string publicId, string systemId)
        {
            this._qualifiedName = qualifiedName;
            this._publicId = publicId;
            this._systemId = systemId;
        }

        protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            //if (IsAssociated) return new DOMDocumentType(XmlDocumentType);
            //else return new DOMDocumentType(this._qualifiedName, this._publicId, this._systemId);
            throw new NotImplementedException();
        }

        #endregion

        #region Hierarchy

        internal override void Associate(XmlDocument document)
        {
            //if (!IsAssociated)
            //{
            //    XmlDocumentType = document.CreateDocumentType(_qualifiedName, _publicId, _systemId, null);
            //}
            throw new NotImplementedException();
        }

        #endregion
    }
}
