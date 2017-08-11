using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.Library.XmlDom
{
    #region Constants

    /// <summary>
    /// Enumerates possible DOM node types.
    /// </summary>
    public enum NodeType
    {
        Element = 1,
        Attribute = 2,
        Text = 3,
        CharacterDataSection = 4,
        EntityReference = 5,
        Entity = 6,
        ProcessingInstruction = 7,
        Comment = 8,
        Document = 9,
        DocumentType = 10,
        DocumentFragment = 11,
        Notation = 12,
        HtmlDocument = 13,
        Dtd = 14,
        ElementDecl = 15,
        AttributeDecl = 16,
        EntityDecl = 17,
        NamespaceDecl = 18,
        LocalNamespace = 18
    }

    /// <summary>
    /// Enumerates who-knows-what.
    /// </summary>
    public enum AttributeType
    {
        CharacterData = 1,
        Id = 2,
        IdReference = 3,
        IdReferences = 4,
        Entity = 5,
        Token = 7,
        Tokens = 8,
        Enumeration = 9,
        Notation = 10
    }

    #endregion

    /// <summary>
    /// Implements constants and functions.
    /// </summary>
    public static class XmlDom
    {
        public const int XML_ELEMENT_NODE = (int)NodeType.Element;
        public const int XML_ATTRIBUTE_NODE = (int)NodeType.Attribute;
        public const int XML_TEXT_NODE = (int)NodeType.Text;
        public const int XML_CDATA_SECTION_NODE = (int)NodeType.CharacterDataSection;
        public const int XML_ENTITY_REF_NODE = (int)NodeType.EntityReference;
        public const int XML_ENTITY_NODE = (int)NodeType.Entity;
        public const int XML_PI_NODE = (int)NodeType.ProcessingInstruction;
        public const int XML_COMMENT_NODE = (int)NodeType.Comment;
        public const int XML_DOCUMENT_NODE = (int)NodeType.Document;
        public const int XML_DOCUMENT_TYPE_NODE = (int)NodeType.DocumentType;
        public const int XML_DOCUMENT_FRAG_NODE = (int)NodeType.DocumentFragment;
        public const int XML_NOTATION_NODE = (int)NodeType.Notation;
        public const int XML_HTML_DOCUMENT_NODE = (int)NodeType.HtmlDocument;
        public const int XML_DTD_NODE = (int)NodeType.Dtd;
        public const int XML_ELEMENT_DECL_NODE = (int)NodeType.ElementDecl;
        public const int XML_ATTRIBUTE_DECL_NODE = (int)NodeType.AttributeDecl;
        public const int XML_ENTITY_DECL_NODE = (int)NodeType.EntityDecl;
        public const int XML_NAMESPACE_DECL_NODE = (int)NodeType.NamespaceDecl;
        public const int XML_LOCAL_NAMESPACE = (int)NodeType.LocalNamespace;

        public const int XML_ATTRIBUTE_CDATA = (int)AttributeType.CharacterData;
        public const int XML_ATTRIBUTE_ID = (int)AttributeType.Id;
        public const int XML_ATTRIBUTE_IDREF = (int)AttributeType.IdReference;
        public const int XML_ATTRIBUTE_IDREFS = (int)AttributeType.IdReferences;
        public const int XML_ATTRIBUTE_ENTITY = (int)AttributeType.Entity;
        public const int XML_ATTRIBUTE_NMTOKEN = (int)AttributeType.Token;
        public const int XML_ATTRIBUTE_NMTOKENS = (int)AttributeType.Tokens;
        public const int XML_ATTRIBUTE_ENUMERATION = (int)AttributeType.Enumeration;
        public const int XML_ATTRIBUTE_NOTATION = (int)AttributeType.Notation;

        /// <summary>
        /// Converts a <see cref="SimpleXMLElement"/> object to a <see cref="DOMElement"/>.
        /// </summary>
        //public static DOMElement dom_import_simplexml(SimpleXMLElement node) => DOMNode.Create(node.XmlElement);
    }
}
