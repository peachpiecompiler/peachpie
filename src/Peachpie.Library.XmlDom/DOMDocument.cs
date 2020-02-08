using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Represents an entire HTML or XML document; serves as the root of the document tree.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMDocument : DOMNode
    {
        #region Fields and Properties

        internal XmlDocument XmlDocument
        {
            get
            {
                return XmlNode as XmlDocument ?? XmlNode?.OwnerDocument;
            }
            set
            {
                XmlNode = value;
            }
        }

        private bool _formatOutput;
        private bool _validateOnParse;
        internal bool _isHtmlDocument;

        /// <summary>
        /// Returns &quot;#document&quot;.
        /// </summary>
        public override string nodeName => "#document";

        /// <summary>
        /// Returns <B>null</B>.
        /// </summary>
        public override string nodeValue
        {
            get { return null; }
            set { }
        }

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.Document"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.Document;

        /// <summary>
        /// Returns the node containing the DOCTYPE declaration.
        /// </summary>
        public DOMDocumentType doctype
        {
            get
            {
                var doc_type = XmlDocument.DocumentType;
                return (DOMDocumentType)DOMNode.Create(doc_type);
            }
        }

        /// <summary>
        /// Returns the DOM implementation.
        /// </summary>
        public DOMImplementation implementation => new DOMImplementation();

        /// <summary>
        /// Returns the root element of this document.
        /// </summary>
        public DOMElement documentElement
        {
            get
            {
                XmlElement root = XmlDocument.DocumentElement;
                return (DOMElement)Create(root);
            }
        }

        /// <summary>
        /// Returns the encoding of this document.
        /// </summary>
        public string actualEncoding => this.encoding;

        /// <summary>
        /// Returns the encoding of this document.
        /// </summary>
        public string xmlEncoding => this.encoding;

        /// <summary>
        /// Returns or set the encoding of this document.
        /// </summary>
        public string encoding
        {
            get
            {
                var decl = GetXmlDeclaration();
                return decl?.Encoding;
            }
            set
            {
                var decl = GetXmlDeclaration();
                if (decl != null)
                {
                    decl.Encoding = value;
                }
                else
                {
                    decl = XmlDocument.CreateXmlDeclaration("1.0", value, null);
                    XmlDocument.InsertBefore(decl, XmlDocument.FirstChild);
                }
            }
        }

        /// <summary>
        /// Returns or sets the standalone flag of this document.
        /// </summary>
        public bool xmlStandalone
        {
            get { return this.standalone; }
            set { this.standalone = value; }
        }

        /// <summary>
        /// Returns or sets the standalone flag of this document.
        /// </summary>
        public bool standalone
        {
            get
            {
                XmlDeclaration decl = GetXmlDeclaration();
                return (decl == null || (decl.Standalone != "no"));
            }
            set
            {
                string stand = (value ? "yes" : "no");

                var decl = GetXmlDeclaration();
                if (decl != null)
                {
                    decl.Standalone = stand;
                }
                else
                {
                    decl = XmlDocument.CreateXmlDeclaration("1.0", null, stand);
                    XmlDocument.InsertBefore(decl, XmlDocument.FirstChild);
                }
            }
        }

        /// <summary>
        /// Returns or sets the XML version of this document.
        /// </summary>
        public string xmlVersion
        {
            get { return this.version; }
            set { this.version = value; }
        }

        /// <summary>
        /// Returns or sets the XML version of this document.
        /// </summary>
        public string version
        {
            get
            {
                XmlDeclaration decl = GetXmlDeclaration();
                return (decl == null ? "1.0" : decl.Version);
            }
            set
            {
                XmlDeclaration decl = GetXmlDeclaration();
                if (decl != null)
                {
                    XmlDeclaration new_decl = XmlDocument.CreateXmlDeclaration(value, decl.Encoding, decl.Standalone);
                    XmlDocument.ReplaceChild(new_decl, decl);
                }
                else
                {
                    decl = XmlDocument.CreateXmlDeclaration(value, null, null);
                    XmlDocument.InsertBefore(decl, XmlDocument.FirstChild);
                }
            }
        }

        /// <summary>
        /// Returns <B>true</B>.
        /// </summary>
        public bool strictErrorChecking
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// Returns the base URI of this document.
        /// </summary>
        public string documentURI
        {
            get { return XmlDocument.BaseURI; }
            set { }
        }

        /// <summary>
        /// Returns <B>null</B>.
        /// </summary>
        public DOMConfiguration config => null;

        /// <summary>
        /// Returns or sets whether XML is formatted by <see cref="save(string,int)"/> and <see cref="saveXML(DOMNode)"/>.
        /// </summary>
        public bool formatOutput
        {
            get { return _formatOutput; }
            set { _formatOutput = value; }
        }

        /// <summary>
        /// Returns of sets whether XML is validated against schema by <see cref="load(DOMDocument,string,int)"/> and
        /// <see cref="loadXML(DOMDocument,string,int)"/>.
        /// </summary>
        public bool validateOnParse
        {
            get { return _validateOnParse; }
            set { _validateOnParse = value; }
        }

        /// <summary>
        /// Returns <B>false</B>.
        /// </summary>
        public bool resolveExternals
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Returns or sets whether whitespace should be preserved by this XML document.
        /// </summary>
        public bool preserveWhiteSpace
        {
            get { return XmlDocument.PreserveWhitespace; }
            set { XmlDocument.PreserveWhitespace = value; }
        }

        /// <summary>
        /// Returns <B>false</B>.
        /// </summary>
        public bool recover
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Returns <B>false</B>.
        /// </summary>
        public bool substituteEntities
        {
            get { return false; }
            set { }
        }

        #endregion

        #region Construction

        public DOMDocument(string version = null, string encoding = null)
        {
            this.XmlDocument = PhpXmlDocument.Create();

            __construct(version, encoding);
        }

        internal DOMDocument(XmlDocument xmlDocument)
        {
            this.XmlDocument = xmlDocument;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields) => new DOMDocument(XmlDocument);

        public virtual void __construct(string version = null, string encoding = null)
        {
            // To prevent problems from subsequent calls
            if (XmlDocument.HasChildNodes)
            {
                return;
            }

            // append the corresponding XML declaration to the document
            XmlDocument.AppendChild(XmlDocument.CreateXmlDeclaration(version ?? "1.0", encoding, string.Empty));
        }

        #endregion

        #region Node factory

        /// <summary>
        /// Creates an element with the specified name and inner text.
        /// </summary>
        /// <param name="tagName">The qualified name of the element.</param>
        /// <param name="value">The inner text (value) of the element.</param>
        /// <returns>A new <see cref="DOMElement"/>.</returns>
        public virtual DOMElement createElement(string tagName, string value = null)
        {
            XmlElement element = XmlDocument.CreateElement(tagName);
            if (value != null) element.InnerText = value;
            return new DOMElement(element);
        }

        /// <summary>
        /// Creates a new document fragment.
        /// </summary>
        /// <returns>A new <see cref="DOMDocumentFragment"/>.</returns>
        public virtual DOMDocumentFragment createDocumentFragment()
        {
            XmlDocumentFragment fragment = XmlDocument.CreateDocumentFragment();
            return new DOMDocumentFragment(fragment);
        }

        /// <summary>
        /// Creates a new text node with the specified text.
        /// </summary>
        /// <param name="data">The text for the text node.</param>
        /// <returns>A new <see cref="DOMText"/>.</returns>
        public virtual DOMText createTextNode(string data)
        {
            XmlText text = XmlDocument.CreateTextNode(data);
            return new DOMText(text);
        }

        /// <summary>
        /// Creates a comment node containing the specified data.
        /// </summary>
        /// <param name="data">The comment data.</param>
        /// <returns>A new <see cref="DOMComment"/>.</returns>
        public virtual DOMComment createComment(string data)
        {
            XmlComment comment = XmlDocument.CreateComment(data);
            return new DOMComment(comment);
        }

        /// <summary>
        /// Creates a CDATA section containing the specified data.
        /// </summary>
        /// <param name="data">The content of the new CDATA section.</param>
        /// <returns>A new <see cref="DOMCdataSection"/>.</returns>
        public virtual DOMCdataSection createCDATASection(string data)
        {
            XmlCDataSection cdata = XmlDocument.CreateCDataSection(data);
            return new DOMCdataSection(cdata);
        }

        /// <summary>
        /// Creates a processing instruction with the specified name and data.
        /// </summary>
        /// <param name="target">The name of the processing instruction.</param>
        /// <param name="data">The data for the processing instruction.</param>
        /// <returns>A new <see cref="DOMProcessingInstruction"/>.</returns>
        public virtual DOMProcessingInstruction createProcessingInstruction(string target, string data = null)
        {
            XmlProcessingInstruction pi = XmlDocument.CreateProcessingInstruction(target, data);
            return new DOMProcessingInstruction(pi);
        }

        /// <summary>
        /// Creates an attribute with the specified name.
        /// </summary>
        /// <param name="name">The qualified name of the attribute.</param>
        /// <returns>A new <see cref="DOMAttr"/>.</returns>
        public virtual DOMAttr createAttribute(string name)
        {
            XmlAttribute attribute = XmlDocument.CreateAttribute(name);
            return new DOMAttr(attribute);
        }

        /// <summary>
        /// Creates an entity reference with the specified name.
        /// </summary>
        /// <param name="name">The name of the entity reference.</param>
        /// <returns>A new <see cref="DOMEntityReference"/>.</returns>
        public DOMEntityReference createEntityReference(string name)
        {
            XmlEntityReference entref = XmlDocument.CreateEntityReference(name);
            return new DOMEntityReference(entref);
        }

        /// <summary>
        /// Creates an element with the specified namespace URI and qualified name.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI of the element.</param>
        /// <param name="qualifiedName">The qualified name of the element.</param>
        /// <param name="value">The inner text (value) of the element.</param>
        /// <returns>A new <see cref="DOMElement"/>.</returns>
        public virtual DOMElement createElementNS(string namespaceUri, string qualifiedName, string value = null)
        {
            XmlElement element = XmlDocument.CreateElement(qualifiedName, namespaceUri);
            if (value != null) element.InnerText = value;
            return new DOMElement(element);
        }

        /// <summary>
        /// Creates an attribute with the specified namespace URI and qualified name.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI of the attribute.</param>
        /// <param name="qualifiedName">The qualified name of the attribute.</param>
        /// <returns>A new <see cref="DOMAttr"/>.</returns>
        public virtual DOMAttr createAttributeNS(string namespaceUri, string qualifiedName)
        {
            XmlAttribute attribute = XmlDocument.CreateAttribute(qualifiedName, namespaceUri);
            return new DOMAttr(attribute);
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

            // enumerate elements in the default namespace
            foreach (XmlNode node in XmlDocument.GetElementsByTagName(name))
            {
                var dom_node = DOMNode.Create(node);
                if (dom_node != null) list.AppendNode(dom_node);
            }

            // enumerate all namespaces
            XPathNavigator navigator = XmlDocument.CreateNavigator();
            XPathNodeIterator iterator = navigator.Select("//namespace::*[not(. = ../../namespace::*)]");

            while (iterator.MoveNext())
            {
                string prefix = iterator.Current.Name;
                if (!String.IsNullOrEmpty(prefix) && prefix != "xml")
                {
                    // enumerate elements in this namespace
                    foreach (XmlNode node in XmlDocument.GetElementsByTagName(name, iterator.Current.Value))
                    {
                        var dom_node = DOMNode.Create(node);
                        if (dom_node != null) list.AppendNode(dom_node);
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

            foreach (XmlNode node in XmlDocument.GetElementsByTagName(localName, namespaceUri))
            {
                var dom_node = DOMNode.Create(node);
                if (dom_node != null) list.AppendNode(dom_node);
            }

            return list;
        }

        /// <summary>
        /// Gets the first element with the matching ID attribute.
        /// </summary>
        /// <param name="elementId">The attribute ID to match.</param>
        /// <returns>A <see cref="DOMElement"/>.</returns>
        public virtual DOMElement getElementById(string elementId)
        {
            XmlElement element = XmlDocument.GetElementById(elementId);
            return element != null ? new DOMElement(element) : null;
        }

        #endregion

        #region Hierarchy

        /// <summary>
        /// Imports a node from another document to the current document.
        /// </summary>
        /// <param name="importedNode">The node being imported.</param>
        /// <param name="deep"><B>True</B> to perform deep clone; otheriwse <B>false</B>.</param>
        /// <returns>The imported <see cref="DOMNode"/>.</returns>
        public virtual DOMNode importNode(DOMNode importedNode, bool deep = false)
        {
            if (importedNode.IsAssociated)
            {
                return DOMNode.Create(XmlDocument.ImportNode(importedNode.XmlNode, deep));
            }
            else
            {
                importedNode.Associate(XmlDocument);
                return importedNode;
            }
        }

        /// <summary>
        /// Not implemented in PHP 7.1.1.
        /// </summary>
        public virtual DOMNode adoptNode(DOMNode source)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Puts the entire XML document into a &quot;normal&quot; form.
        /// </summary>
        public virtual void normalizeDocument() => XmlDocument.Normalize();

        /// <summary>
        /// Not implemented in PHP 7.1.1.
        /// </summary>
        public virtual void renameNode(DOMNode node, string namespaceUri, string qualifiedName)
        {
            throw new NotImplementedException();
        }

        private XmlDeclaration GetXmlDeclaration() => (XmlDocument.FirstChild as XmlDeclaration);

        /// <summary>
        /// Register extended class used to create base node type.
        /// </summary>
        /// <param name="baseclass">The DOM class that you want to extend.</param>
        /// <param name="extendedclass">Your extended class name. If NULL is provided, any previously
        /// registered class extending <paramref name="baseclass"/> will be removed.</param>
        /// <returns>Returns <b>true</b> on success or <b>false</b> on failure.</returns>
        public virtual bool registerNodeClass(string baseclass, string extendedclass)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Load and Save

        /// <summary>
        /// To be used as a static method.
        /// Loads the XML document from the specified URL.
        /// </summary>
        /// <returns>returns a <see cref="DOMDocument"/> or <c>FALSE</c> on failure.</returns>
        [Obsolete]
        [return: CastToFalse]
        public static DOMDocument load(Context ctx, string fileName)
        {
            var document = new DOMDocument();

            if (document.load(ctx, fileName))
            {
                return document;
            }
            else
            {
                return null; // FALSE
            }
        }

        /// <summary>
        /// Loads the XML document from the specified URL.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="fileName">URL for the file containing the XML document to load.</param>
        /// <param name="options">Undocumented.</param>
        /// <returns><b>True</b> on success or <b>false</b> on failure.</returns>
        public virtual bool load(Context ctx, string fileName, int options = 0)
        {
            // TODO: this method can be called both statically and via an instance

            _isHtmlDocument = false;

            using (PhpStream stream = PhpStream.Open(ctx, fileName, "rt"))
            {
                if (stream == null) return false;

                try
                {
                    XmlReaderSettings settings = new XmlReaderSettings() { DtdProcessing = DtdProcessing.Parse };

                    // validating XML reader
                    if (this._validateOnParse)
                    {
#pragma warning disable 618
                        settings.ValidationType = ValidationType.Auto;
#pragma warning restore 618
                    }
                    XmlDocument.Load(XmlReader.Create(stream.RawStream, settings, XIncludeHelper.UriResolver(fileName, ctx.WorkingDirectory)));
                }
                catch (XmlException e)
                {
                    PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, fileName);
                    return false;
                }
                catch (IOException e)
                {
                    PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, fileName);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Loads the XML document from the specified string.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="xmlString">The XML string.</param>
        /// <param name="options">Undocumented.</param>
        /// <returns><b>True</b> on success or <b>false</b> on failure.</returns>
        public virtual bool loadXML(Context ctx, string xmlString, int options = 0)
        {
            // TODO: this method can be called both statically and via an instance

            return loadXMLInternal(ctx, xmlString, options, false);
        }

        /// <summary>
        /// Loads provided XML string into this <see cref="DOMDocument"/>.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="xmlString">String representing XML document.</param>
        /// <param name="options">PHP options.</param>
        /// <param name="isHtml">Whether the <paramref name="xmlString"/> represents XML generated from HTML document (then it may contain some invalid XML characters).</param>
        /// <returns></returns>
        private bool loadXMLInternal(Context ctx, string xmlString, int options, bool isHtml)
        {
            this._isHtmlDocument = isHtml;

            var stream = new StringReader(xmlString);

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings() { DtdProcessing = DtdProcessing.Parse };

                // validating XML reader
                if (this._validateOnParse)
                {
#pragma warning disable 618
                    settings.ValidationType = ValidationType.Auto;
#pragma warning restore 618
                }

                // do not check invalid characters in HTML (XML)
                if (isHtml)
                {
                    settings.CheckCharacters = false;
                }

                // load the document
                this.XmlDocument.Load(XmlReader.Create(stream, settings));

                // done
                return true;
            }
            catch (XmlException e)
            {
                PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_ERROR, 0, e.LineNumber, e.LinePosition, e.Message, null);
                return false;
            }
            catch (IOException e)
            {
                PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Saves the XML document to the specified stream.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="fileName">The location of the file where the document should be saved.</param>
        /// <param name="options">Unsupported.</param>
        /// <returns>The number of bytes written or <B>false</B> on error.</returns>
        public virtual PhpValue save(Context ctx, string fileName, int options = 0)
        {
            using (PhpStream stream = PhpStream.Open(ctx, fileName, StreamOpenMode.WriteText))
            {
                if (stream == null) return PhpValue.Create(false);

                try
                {
                    // direct stream write indents
                    if (_formatOutput)
                    {
                        XmlDocument.Save(stream.RawStream);
                    }
                    else
                    {
                        var settings = new XmlWriterSettings()
                        {
                            Encoding = Utils.GetNodeEncoding(ctx, XmlNode)
                        };

                        using (var writer = System.Xml.XmlWriter.Create(stream.RawStream, settings))
                        {
                            XmlDocument.Save(writer);
                        }
                    }
                }
                catch (XmlException e)
                {
                    PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, fileName);
                    return PhpValue.False;
                }
                catch (IOException e)
                {
                    PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, fileName);
                    return PhpValue.False;
                }

                // TODO:
                return PhpValue.Create(stream.RawStream.CanSeek ? stream.RawStream.Position : 1);
            }
        }

        /// <summary>
        /// Returns the string representation of this document.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="node">The node to dump (the entire document if <B>null</B>).</param>
        /// <param name="options">Unsupported.</param>
        /// <returns>The string representation of the document / the specified node or <B>false</B>.</returns>
        [return: CastToFalse]
        public virtual PhpString saveXML(Context ctx, DOMNode node = null, int options = 0)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (SaveXMLInternal(ctx, stream, node))
                {
                    return new PhpString(stream.ToArray());
                }
                else
                {
                    return default(PhpString);
                }
            }
        }

        /// <summary>
        /// Saves this document to a given stream.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="outStream">The output stream.</param>
        /// <param name="node">The node to dump (the entire document if <B>null</B>).</param>
        /// <param name="omitXmlDeclaration">Whether to skip the opening XML declaration.</param>
        /// <returns>True for success, false for failure.</returns>
        private bool SaveXMLInternal(Context ctx, Stream outStream, DOMNode node = null, bool omitXmlDeclaration = false)
        {
            XmlNode xml_node;

            if (node == null)
            {
                xml_node = XmlDocument;
            }
            else
            {
                xml_node = node.XmlNode;

                if (xml_node.OwnerDocument != XmlDocument && xml_node != XmlNode)
                {
                    DOMException.Throw(ExceptionCode.WrongDocument);
                    return false;
                }
            }

            var settings = new XmlWriterSettings()
            {
                NewLineHandling = NewLineHandling.None,
                Encoding = Utils.GetNodeEncoding(ctx, xml_node),
                Indent = _formatOutput,
                ConformanceLevel = node == null ? ConformanceLevel.Document : ConformanceLevel.Fragment,
                OmitXmlDeclaration = omitXmlDeclaration
            };

            // use a XML writer and set its Formatting property to Formatting.Indented
            using (var writer = System.Xml.XmlWriter.Create(outStream, settings))
            {
                xml_node.WriteTo(writer);
            }

            return true;
        }

        /// <summary>
        /// Processes HTML errors, if any.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="htmlDoc"><see cref="HtmlAgilityPack.HtmlDocument"/> instance to process errors from.</param>
        /// <param name="filename">HTML file name or <c>null</c> if HTML has been loaded from a string.</param>
        private void CheckHtmlErrors(Context ctx, HtmlAgilityPack.HtmlDocument htmlDoc, string filename)
        {
            Debug.Assert(htmlDoc != null);

            foreach (var error in htmlDoc.ParseErrors)
            {
                switch (error.Code)
                {
                    case HtmlAgilityPack.HtmlParseErrorCode.EndTagNotRequired:
                    case HtmlAgilityPack.HtmlParseErrorCode.TagNotOpened:
                        break;
                    default:
                        PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_ERROR, 0, error.Line, error.LinePosition, "(" + error.Code.ToString() + ")" + error.Reason, filename);
                        break;
                }
            }
        }

        /// <summary>
        /// To be used as a static method.
        /// Loads HTML from a string.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="source">String containing HTML document.</param>
        /// <returns>returns a <see cref="DOMDocument"/> or <c>FALSE</c> on failure.</returns>
        [Obsolete]
        [return: CastToFalse]
        public static DOMDocument loadHTML(Context ctx, string source)
        {
            var document = new DOMDocument();

            if (document.loadHTML(ctx, source))
            {
                return document;
            }
            else
            {
                return null; // FALSE
            }
        }

        /// <summary>
        /// Loads HTML from a string.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="source">String containing HTML document.</param>
        /// <param name="options">Unsupported.</param>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public virtual bool loadHTML(Context ctx, string source, int options = 0)
        {
            if (string.IsNullOrEmpty(source))
            {
                PhpException.InvalidArgument(nameof(source), Pchp.Library.Resources.Resources.arg_null_or_empty);
                return false;
            }

            return loadHTML(ctx, new StringReader(source), null);
        }

        /// <summary>
        /// Loads HTML from a file.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="sourceFile">Path to a file containing HTML document.</param>
        /// <param name="options">Unsupported.</param>
        public virtual bool loadHTMLFile(Context ctx, string sourceFile, int options = 0)
        {
            using (PhpStream stream = PhpStream.Open(ctx, sourceFile, "rt"))
            {
                if (stream == null) return false;

                return loadHTML(ctx, new StreamReader(stream.RawStream), sourceFile);
            }
        }

        /// <summary>
        /// Load HTML DOM from given <paramref name="stream"/>.
        /// </summary>
        private protected bool loadHTML(Context ctx, TextReader stream, string filename, int options = 0)
        {
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();

            // setup HTML parser
            htmlDoc.OptionOutputAsXml = true;
            //htmlDoc.OptionOutputOriginalCase = true;  // NOTE: we need lower-cased names because of XPath queries
            //htmlDoc.OptionFixNestedTags = true;
            htmlDoc.OptionCheckSyntax = false;
            htmlDoc.OptionUseIdAttribute = false;   // only needed when XPath navigator is used on htmlDoc
            htmlDoc.OptionWriteEmptyNodes = true;

            // load HTML (from string or a stream)
            htmlDoc.Load(stream);

            CheckHtmlErrors(ctx, htmlDoc, filename);

            //// save to string as XML
            //using (var sw = new StringWriter())
            //{
            //    htmlDoc.Save(sw);

            //    // load as XML
            //    return loadXMLInternal(ctx, sw.ToString(), 0, true);
            //}

            this.XmlDocument.LoadHtml(htmlDoc);
            return true;
        }

        /// <summary>
        /// Dumps the internal document into a string using HTML formatting.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="node">Optional parameter to output a subset of the document.</param>
        /// <returns>Returns the HTML, or FALSE if an error occurred.</returns>
        [return: CastToFalse]
        public virtual PhpString saveHTML(Context ctx, DOMNode node = null)
        {
            using (var ms = new MemoryStream())
            {
                if (node == null && XmlDocument?.DocumentType == null)
                {
                    // we are saving the whole document and there is no DOCTYPE,
                    // output the default DOCTYPE:
                    OutputDefaultHtmlDoctype(ms);
                }

                SaveXMLInternal(ctx, ms, node, omitXmlDeclaration: true);

                return new PhpString(ms.ToArray());
            }
        }

        /// <summary>
        /// Dumps the internal document into a file using HTML formatting.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="file">The path to the saved HTML document.</param>
        public virtual PhpValue saveHTMLFile(Context ctx, string file)
        {
            using (PhpStream stream = PhpStream.Open(ctx, file, "wt"))
            {
                if (stream == null)
                {
                    return PhpValue.False;
                }

                if (XmlDocument?.DocumentType == null)
                {
                    OutputDefaultHtmlDoctype(stream.RawStream);
                }

                SaveXMLInternal(ctx, stream.RawStream, null, omitXmlDeclaration: true);

                // TODO:
                return PhpValue.Create(stream.RawStream.CanSeek ? stream.RawStream.Position : 1);
            }
        }

        private void OutputDefaultHtmlDoctype(Stream outStream)
        {
            using (var sw = new StreamWriter(outStream, Encoding.ASCII, bufferSize: 128, leaveOpen: true))
            {
                // HTML 4.01 Transitional
                sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\" \"http://www.w3.org/TR/REC-html40/loose.dtd\">");
                sw.Flush();
            }
        }

        #endregion

        #region XInclude

        /// <summary>
        /// Substitutes XIncludes in a DOMDocument Object
        /// </summary>
        /// <returns>Returns the number of XIncludes in the document, -1 if some processing failed, or FALSE if there were no substitutions.</returns>
        public virtual PhpValue xinclude(Context ctx, int options = 0)
        {
            // TODO: xinclude options

            return new XIncludeHelper(ctx).Include(XmlDocument);
        }
        #endregion

        #region Validation

        /// <summary>
        /// Not implemented (System.Xml does not support post-load DTD validation).
        /// </summary>
        public virtual bool validate()
        {
            //PhpException.Throw(PhpError.Warning, Resources.PostLoadDtdUnsupported);
            throw new DOMException(Resources.PostLoadDtdUnsupported);
        }

        /// <summary>
        /// Validates the document against the specified XML schema.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="schemaFile">URL for the file containing the XML schema to load.</param>
        /// <param name="flags">Unsupported.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual bool schemaValidate(Context ctx, string schemaFile, int flags = 0)
        {
            if ((flags & PhpLibXml.LIBXML_SCHEMA_CREATE) == PhpLibXml.LIBXML_SCHEMA_CREATE)
            {
                PhpException.Throw(PhpError.Warning, Resources.SchemaCreateUnsupported);
            }

            XmlSchema schema;

            using (PhpStream stream = PhpStream.Open(ctx, schemaFile, "rt"))
            {
                if (stream == null) return false;

                try
                {
                    schema = XmlSchema.Read(stream.RawStream, null);
                }
                catch (XmlException e)
                {
                    PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, e.Message, schemaFile);
                    return false;
                }
                catch (IOException e)
                {
                    PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_ERROR, 0, 0, 0, e.Message, schemaFile);
                    return false;
                }
            }

            XmlDocument.Schemas.Add(schema);
            try
            {
                XmlDocument.Validate(null);
            }
            catch (XmlException)
            {
                return false;
            }
            finally
            {
                XmlDocument.Schemas.Remove(schema);
            }
            return true;
        }

        /// <summary>
        /// Validates the document against the specified XML schema.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="schemaString">The XML schema string.</param>
        /// <param name="flags">Unsupported.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual bool schemaValidateSource(Context ctx, string schemaString, int flags = 0)
        {
            if ((flags & PhpLibXml.LIBXML_SCHEMA_CREATE) == PhpLibXml.LIBXML_SCHEMA_CREATE)
            {
                PhpException.Throw(PhpError.Warning, Resources.SchemaCreateUnsupported);
            }

            XmlSchema schema;

            try
            {
                schema = XmlSchema.Read(new System.IO.StringReader(schemaString), null);
            }
            catch (XmlException e)
            {
                PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, e.Message, null);
                return false;
            }

            XmlDocument.Schemas.Add(schema);
            try
            {
                XmlDocument.Validate(null);
            }
            catch (XmlException)
            {
                return false;
            }
            finally
            {
                XmlDocument.Schemas.Remove(schema);
            }
            return true;
        }

        /// <summary>
        /// Not implemented (TODO: will need a Relax NG validator for this).
        /// </summary>
        public virtual bool relaxNGValidate(string schemaFile)
        {
            PhpException.Throw(PhpError.Warning, Resources.RelaxNGUnsupported);
            return true;
        }

        /// <summary>
        /// Not implemented (TODO: will need a Relax NG validator for this).
        /// </summary>
        public virtual bool relaxNGValidateSource(string schema)
        {
            PhpException.Throw(PhpError.Warning, Resources.RelaxNGUnsupported);
            return true;
        }

        #endregion
    }
}
