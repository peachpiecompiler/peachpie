using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Pchp.Core;
using Pchp.Core.Resources;
using Pchp.Core.Utilities;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// The XMLReader extension is an XML pull parser. The reader acts as a cursor going forward
    /// on the document stream and stopping at each node on the way.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class XMLReader
    {
        #region XmlReader node types

        public const int NONE = 0;
        public const int ELEMENT = 1;
        public const int ATTRIBUTE = 2;
        public const int TEXT = 3;
        public const int CDATA = 4;
        public const int ENTITY_REF = 5;
        public const int ENTITY = 6;
        public const int PI = 7;
        public const int COMMENT = 8;
        public const int DOC = 9;
        public const int DOC_TYPE = 10;
        public const int DOC_FRAGMENT = 11;
        public const int NOTATION = 12;
        public const int WHITESPACE = 13;
        public const int SIGNIFICANT_WHITESPACE = 14;
        public const int END_ELEMENT = 15;
        public const int END_ENTITY = 16;
        public const int XML_DECLARATION = 17;
        public const int LOADDTD = 1;
        public const int DEFAULTATTRS = 2;
        public const int VALIDATE = 3;
        public const int SUBST_ENTITIES = 4;

        #endregion

        #region Fields

        private XmlReader _reader;

        private readonly Dictionary<int, bool> _parserProperties = new Dictionary<int, bool>(4);
        private string _source;
        private string _encoding;
        private int _options;
        private bool _uriSource;

        #endregion

        #region Properties

        /// <summary>
        /// The number of attributes on the node.
        /// </summary>
        public int attributeCount => getAttributeCount();

        /// <summary>
        /// The base URI of the node.
        /// </summary>
        public object baseURI => Active ? _reader.BaseURI : "";

        /// <summary>
        /// Depth of the node in the tree, starting at 0.
        /// </summary>
        public int depth => Active ? _reader.Depth : 0;

        /// <summary>
        /// Indicates if node has attributes.
        /// </summary>
        public bool hasAttributes => Active && _reader.HasAttributes;

        /// <summary>
        /// Indicates if node has a text value.
        /// </summary>
        public bool hasValue => Active && _reader.HasValue;

        /// <summary>
        /// Indicates if attribute is defaulted from DTD.
        /// </summary>
        public bool isDefault => Active && _reader.IsDefault;

        /// <summary>
        /// Indicates if node is an empty element tag.
        /// </summary>
        public bool isEmptyElement => Active && _reader.IsEmptyElement;

        /// <summary>
        /// The local name of the node.
        /// </summary>
        public string localName => Active ? _reader.LocalName : "";

        /// <summary>
        /// The qualified name of the node.
        /// </summary>
        public string name => !Active ? "" : (!string.IsNullOrEmpty(_reader.Name) ? _reader.Name : getNodeTypeName());

        /// <summary>
        /// The URI of the namespace associated with the node.
        /// </summary>
        public string namespaceURI => Active ? _reader.NamespaceURI : "";

        /// <summary>
        /// The node type for the node.
        /// </summary>
        public int nodeType => Active ? (int)_reader.NodeType : 0;

        /// <summary>
        /// The prefix of the namespace associated with the node.
        /// </summary>
        public string prefix => Active ? _reader.Prefix : "";

        /// <summary>
        /// The text value of the node.
        /// </summary>
        public string value => Active ? _reader.Value : "";

        /// <summary>
        /// The xml:lang scope which the node resides.
        /// </summary>
        public string xmlLang => Active ? _reader.XmlLang : "";

        #endregion

        #region Methods

        public bool close()
        {
            if (_reader != null)
            {
                try
                {
                    XmlReader old = _reader;
                    _reader = null;
                    old.Close();
                }
                catch (Exception)
                {
                }
            }

            return true;
        }

        public bool expand(DOMNode basenode = null)
        {
            PhpException.FunctionNotSupported(nameof(expand));
            return false;
        }

        public string getAttribute(string name)
        {
            return (Active && _reader.NodeType == XmlNodeType.Element) ? _reader.GetAttribute(name) : null;
        }

        public string getAttributeNo(int index)
        {
            return (Active && _reader.NodeType == XmlNodeType.Element) ? _reader.GetAttribute(index) : null;
        }

        public string getAttributeNs(string localName, string namespaceURI)
        {
            return (Active && _reader.NodeType == XmlNodeType.Element) ? _reader.GetAttribute(localName, namespaceURI) : null;
        }

        public bool getParserProperty(int property)
        {
            bool oldValue;
            return _parserProperties.TryGetValue(property, out oldValue) && oldValue;
        }

        public bool isValid()
        {
            //TODO: This function is for schema validation.
            return _reader != null && _reader.ReadState != ReadState.Error;
        }

        public bool lookupNamespace(string prefix)
        {
            return Active && _reader.LookupNamespace(prefix) != null;
        }

        public bool moveToAttribute(string name)
        {
            return _reader.MoveToAttribute(name);
        }

        public bool moveToAttributeNo(int index)
        {
            if (!Active || index < 0 || index >= getAttributeCount())
            {
                return false;
            }

            moveToElement();
            moveToFirstAttribute();
            int j = 0;
            while (j < index)
            {
                _reader.MoveToNextAttribute();
                ++j;
            }

            return j < index;
        }

        public bool moveToAttributeNs(string localName, string namespaceURI)
        {
            return Active && _reader.MoveToAttribute(localName, namespaceURI);
        }

        public bool moveToElement()
        {
            return Active && _reader.MoveToElement();
        }

        public bool moveToFirstAttribute()
        {
            return Active && _reader.MoveToFirstAttribute();
        }

        public bool moveToNextAttribute()
        {
            return Active && _reader.MoveToNextAttribute();
        }

        public bool next(string localname = null)
        {
            _reader.Skip();
            if (string.IsNullOrEmpty(localname))
            {
                return !_reader.EOF;
            }

            while (_reader.LocalName != localname && !_reader.EOF)
            {
                _reader.Skip();
            }

            return _reader.LocalName == localname && !_reader.EOF;
        }

        public bool open(Context ctx, string URI, string encoding = null, int options = 0)
        {
            if (string.IsNullOrWhiteSpace(URI))
            {
                PhpException.Throw(PhpError.Warning, Pchp.Library.Resources.Resources.arg_empty, nameof(URI));
                return false;
            }

            _source = FileSystemUtils.AbsolutePath(ctx, URI);
            _uriSource = true;
            _encoding = encoding;
            _options = options;
            return createReader();
        }

        public bool read()
        {
            try
            {
                if (_reader == null ||
                    _reader.ReadState == ReadState.Error ||
                    _reader.ReadState == ReadState.EndOfFile ||
                    _reader.ReadState == ReadState.Closed)
                {
                    return false;
                }

                if (_reader.ReadState == ReadState.Interactive)
                {
                    // Shouldn't Read() return false on Error?
                    return _reader.Read() &&
                           _reader.ReadState != ReadState.Error;
                }

                // Initial state.
                while (_reader.NodeType != XmlNodeType.Element &&
                       _reader.ReadState != ReadState.Error)
                {
                    if (!_reader.Read())
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return false;
            }
        }

        public string readInnerXML()
        {
            return Active ? _reader.ReadInnerXml() : "";
        }

        public string readOuterXML()
        {
            return Active ? _reader.ReadOuterXml() : "";
        }

        public string readString()
        {
            return Active ? _reader.ReadString() : "";
        }

        public bool setParserProperty(int property, bool newValue)
        {
            if (_reader == null || _reader.ReadState != ReadState.Initial)
            {
                return false;
            }

            bool oldValue;
            if (!_parserProperties.TryGetValue(property, out oldValue) ||
                oldValue != newValue)
            {
                _parserProperties[property] = newValue;
                return createReader();
            }

            return true;
        }

        public bool setRelaxNGSchema(string filename)
        {
            PhpException.FunctionNotSupported(nameof(setRelaxNGSchema));
            return false;
        }

        public bool setRelaxNGSchemaSource(string source)
        {
            PhpException.FunctionNotSupported(nameof(setRelaxNGSchemaSource));
            return false;
        }

        public bool setSchema(string filename)
        {
            PhpException.FunctionNotSupported(nameof(setSchema));
            return false;
        }

        public bool xml(string source, string encoding = null, int options = 0)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                PhpException.Throw(PhpError.Warning, Pchp.Library.Resources.Resources.arg_empty, nameof(source));
                return false;
            }

            _source = source;
            _uriSource = false;
            _encoding = encoding;
            _options = options;
            return createReader();
        }

        #endregion

        #region Implementation

        protected bool Active
        {
            get { return _reader != null && _reader.ReadState == ReadState.Interactive; }
        }

        protected int getAttributeCount()
        {
            return _reader != null ? _reader.AttributeCount : 0;
        }

        /// <summary>
        /// HTML-encoded paths are converted into unix path. Probably .Net trying to assume we're a web-server.
        /// Original URI from PHP: file:///Z%3A%5CPhalanger%5CTesting%5CTests%5CXml%5CxmlReader/dtdexample.dtd
        /// Uri.ToString(): file:///Z:/Phalanger/Testing/Tests/Xml/xmlReader/dtdexample.dtd 
        /// Uri.LocalPath: /Z:/Phalanger/Testing/Tests/Xml/xmlReader/dtdexample.dtd
        /// As a workaround, we simply load Uri.ToString() into a new Uri (so the resulting LocalPath is correct).
        /// Result: Z:\Phalanger\Testing\Tests\Xml\xmlReader\dtdexample.dtd
        /// </summary>
        class FileUriResolver : XmlUrlResolver
        {
            public override bool SupportsType(Uri absoluteUri, Type type)
            {
                absoluteUri = new Uri(absoluteUri.ToString());
                return base.SupportsType(absoluteUri, type);
            }

            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                absoluteUri = new Uri(absoluteUri.ToString());
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }

        private XmlReaderSettings createSettings()
        {
            var settings = new XmlReaderSettings();

            settings.CloseInput = true;
            settings.ConformanceLevel = ConformanceLevel.Auto;
            settings.ValidationType = getParserProperty(VALIDATE) ? ValidationType.DTD : ValidationType.None;
            settings.DtdProcessing = getParserProperty(LOADDTD) ? DtdProcessing.Parse : DtdProcessing.Ignore;
            settings.XmlResolver = new FileUriResolver();

            return settings;
        }

        private bool createReader()
        {
            close();
            try
            {
                var settings = createSettings();
                _reader = _uriSource ?
                    XmlReader.Create(_source, settings)
                    : XmlReader.Create(new StringReader(_source), settings);
                return true;
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                close();
            }

            return false;
        }

        private string getNodeTypeName()
        {
            if (_reader == null)
            {
                return "";
            }

            switch ((int)_reader.NodeType)
            {
                case NONE:
                    return "#none";
                case ELEMENT:
                    return "#element";
                case ATTRIBUTE:
                    return "#attribute";
                case TEXT:
                    return "#text";
                case CDATA:
                    return "#cdata";
                case ENTITY_REF:
                    return "#entityref";
                case ENTITY:
                    return "#entity";
                case PI:
                    return "#pi";
                case COMMENT:
                    return "#comment";
                case DOC:
                    return "#doc";
                case DOC_TYPE:
                    return "#doctype";
                case DOC_FRAGMENT:
                    return "#docfragment";
                case NOTATION:
                    return "#notation";
                case WHITESPACE:
                    return "";
                case SIGNIFICANT_WHITESPACE:
                    return "";
                case END_ELEMENT:
                    return "#endelement";
                case END_ENTITY:
                    return "#endentity";
                case XML_DECLARATION:
                    return "#xmldeclaration";
            }

            return "";
        }

        #endregion
    }
}
