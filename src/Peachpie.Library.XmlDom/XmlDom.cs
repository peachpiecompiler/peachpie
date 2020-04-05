using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

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
    /// Enumerates who-knows-what. (TODO)
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
    [PhpExtension("dom")]
    public static class XmlDom
    {
        #region Constants

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
        /// Error code not part of the DOM specification. Meant for PHP errors.
        /// </summary>
        public const int DOM_PHP_ERR = (int)ExceptionCode.PhpError;

        /// <summary>
        /// Index or size is negative, or greater than the allowed value. 
        /// </summary>
        public const int DOM_INDEX_SIZE_ERR = (int)ExceptionCode.IndexOutOfBounds;

        /// <summary>
        /// The specified range of text does not fit into a string.
        /// </summary>
        public const int DOMSTRING_SIZE_ERR = (int)ExceptionCode.StringTooLong;

        /// <summary>
        /// A node is inserted somewhere it doesn't belong.
        /// </summary>
        public const int DOM_HIERARCHY_REQUEST_ERR = (int)ExceptionCode.BadHierarchy;

        /// <summary>
        /// A node is used in a different document than the one that created it.
        /// </summary>
        public const int DOM_WRONG_DOCUMENT_ERR = (int)ExceptionCode.WrongDocument;

        /// <summary>
        /// An invalid or illegal character is specified, such as in a name.
        /// </summary>
        public const int DOM_INVALID_CHARACTER_ERR = (int)ExceptionCode.InvalidCharacter;

        /// <summary>
        /// Data is specified for a node which does not support data.
        /// </summary>
        public const int DOM_NO_DATA_ALLOWED_ERR = (int)ExceptionCode.DataNotAllowed;

        /// <summary>
        /// An attempt is made to modify an object where modifications are not allowed.
        /// </summary>
        public const int DOM_NO_MODIFICATION_ALLOWED_ERR = (int)ExceptionCode.DomModificationNotAllowed;

        /// <summary>
        /// An attempt is made to reference a node in a context where it does not exist.
        /// </summary>
        public const int DOM_NOT_FOUND_ERR = (int)ExceptionCode.NotFound;

        /// <summary>
        /// The implementation does not support the requested type of object or operation.
        /// </summary>
        public const int DOM_NOT_SUPPORTED_ERR = (int)ExceptionCode.NotSupported;

        /// <summary>
        /// An attempt is made to add an attribute that is already in use elsewhere.
        /// </summary>
        public const int DOM_INUSE_ATTRIBUTE_ERR = (int)ExceptionCode.AttributeInUse;

        /// <summary>
        /// An attempt is made to use an object that is not, or is no longer, usable.
        /// </summary>
        public const int DOM_INVALID_STATE_ERR = (int)ExceptionCode.InvalidState;

        /// <summary>
        /// An invalid or illegal string is specified.
        /// </summary>
        public const int DOM_SYNTAX_ERR = (int)ExceptionCode.SyntaxError;

        /// <summary>
        /// An attempt is made to modify the type of the underlying object.
        /// </summary>
        public const int DOM_INVALID_MODIFICATION_ERR = (int)ExceptionCode.ModificationNotAllowed;

        /// <summary>
        /// An attempt is made to create or change an object in a way which is incorrect with
        /// regard to namespaces.
        /// </summary>
        public const int DOM_NAMESPACE_ERR = (int)ExceptionCode.NamespaceError;

        /// <summary>
        /// A parameter or an operation is not supported by the underlying object.
        /// </summary>
        public const int DOM_INVALID_ACCESS_ERR = (int)ExceptionCode.InvalidAccess;

        /// <summary>
        /// A call to a method such as <B>insertBefore</B> or <B>removeChild</B> would make the
        /// node invalid with respect to &quot;partial validity&quot;, this exception would be
        /// raised and the operation would not be done. 
        /// </summary>
        public const int DOM_VALIDATION_ERR = (int)ExceptionCode.ValidationError;

        #endregion

        /// <summary>
        /// Converts a <see cref="SimpleXMLElement"/> object to a <see cref="DOMElement"/>.
        /// </summary>
        /// <param name="node">The <see cref="SimpleXMLElement"/> node.</param>
        /// <returns>The DOMElement node added or FALSE if any errors occur.</returns>
        [return: CastToFalse]
        public static DOMElement dom_import_simplexml(SimpleXMLElement node) => (DOMElement)DOMNode.Create(node.XmlElement);
    }
}
