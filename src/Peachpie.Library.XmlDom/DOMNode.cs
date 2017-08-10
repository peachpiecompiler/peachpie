using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName)]
    public class DOMNode : IXmlDomNode
    {
        #region IXmlDomNode Members

        XmlNode IXmlDomNode.UnderlyingObject => XmlNode;

        #endregion

        private XmlNode _xmlNode;
        protected internal XmlNode XmlNode
        {
            get
            {
                if (_xmlNode == null) DOMException.Throw(DOMException.ExceptionCode.InvalidState);
                return _xmlNode;
            }
            set
            {
                _xmlNode = value;
            }
        }

        protected internal bool IsAssociated => _xmlNode != null;

        /// <summary>
		/// Returns the name of the node (exact meaning depends on the particular subtype).
		/// </summary>
        public virtual string nodeName => XmlNode.Name;

        /// <summary>
        /// Returns or sets the value of the node (exact meaning depends on the particular subtype).
        /// </summary>
        public virtual string nodeValue
        {
            get
            {
                return XmlNode.Value;
            }
            set
            {
                XmlNode.Value = value;
            }
        }

        internal static IXmlDomNode Create(XmlNode xmlNode)
        {
            if (xmlNode != null)
            {
                switch (xmlNode.NodeType)
                {
                    //case XmlNodeType.Attribute: return new DOMAttr((XmlAttribute)xmlNode);
                    //case XmlNodeType.SignificantWhitespace:
                    //case XmlNodeType.Whitespace: return null;// TODO: new DOMText((XmlCharacterData)xmlNode); // also see XmlDocument.PreserveWhitespace
                    //case XmlNodeType.CDATA: return new DOMCdataSection((XmlCDataSection)xmlNode);
                    //case XmlNodeType.Comment: return new DOMComment((XmlComment)xmlNode);
                    //case XmlNodeType.Document: return new DOMDocument((XmlDocument)xmlNode);
                    //case XmlNodeType.DocumentFragment: return new DOMDocumentFragment((XmlDocumentFragment)xmlNode);
                    //case XmlNodeType.DocumentType: return new DOMDocumentType((XmlDocumentType)xmlNode);
                    //case XmlNodeType.Element: return new DOMElement((XmlElement)xmlNode);
                    //case XmlNodeType.Entity: return new DOMEntity((XmlEntity)xmlNode);
                    //case XmlNodeType.EntityReference: return new DOMEntityReference((XmlEntityReference)xmlNode);
                    //case XmlNodeType.Notation: return new DOMNotation((XmlNotation)xmlNode);
                    //case XmlNodeType.ProcessingInstruction: return new DOMProcessingInstruction((XmlProcessingInstruction)xmlNode);
                    //case XmlNodeType.Text: return new DOMText((XmlText)xmlNode);
                    //case XmlNodeType.XmlDeclaration:
                    default:
                        return null;
                }
            }

            return null;
        }

    }
}
