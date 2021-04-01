using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Pchp.Core;
using Pchp.Core.Collections;
using Pchp.Core.Reflection;
using Pchp.Core.Utilities;
using Pchp.Library;
using Pchp.Library.Streams;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Contains implementation of SimpleXML functions.
    /// </summary>
    [PhpExtension("simplexml")]
    public static class SimpleXml
    {
        #region simplexml_load_file

        /// <summary>
        /// Loads an XML file into an object.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="className">The name of the class whose instance should be returned (must extend
        /// <see cref="SimpleXMLElement"/>).</param>
        /// <param name="options">Additional parameters (unsupported).</param>
        /// <returns>An instance of <see cref="SimpleXMLElement"/> or of the class specified by
        /// <paramref name="className"/>, or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static SimpleXMLElement simplexml_load_file(Context ctx, string fileName, string className = null, int options = 0)
        {
            var doc = PhpXmlDocument.Create();

            using (var stream = PhpStream.Open(ctx, fileName, StreamOpenMode.ReadText))
            {
                if (stream == null)
                {
                    return null;
                }

                try
                {
                    doc.Load(stream.RawStream);
                }
                catch (XmlException e)
                {
                    PhpException.Throw(PhpError.Warning, e.Message);
                    return null;
                }
                catch (IOException e)
                {
                    PhpException.Throw(PhpError.Warning, e.Message);
                    return null;
                }
            }

            if (TryResolveType(ctx, className, out var type))
            {
                return SimpleXMLElement.Create(ctx, type, doc.DocumentElement);
            }
            else
            {
                return null; // false
            }
        }

        #endregion

        #region simplexml_load_string

        /// <summary>
        /// Loads a string of XML into an object.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="data">The XML string.</param>
        /// <param name="className">The name of the class whose instance should be returned (must extend
        /// <see cref="SimpleXMLElement"/>).</param>
        /// <param name="options">Additional parameters (unsupported).</param>
        /// <returns>An instance of <see cref="SimpleXMLElement"/> or of the class specified by
        /// <paramref name="className"/>, or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static SimpleXMLElement simplexml_load_string(Context ctx, string data, string className = null, int options = 0)
        {
            var doc = PhpXmlDocument.Create();

            try
            {
                doc.LoadXml(data);
            }
            catch (XmlException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return null;
            }
            catch (IOException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return null;
            }

            if (TryResolveType(ctx, className, out var type))
            {
                return SimpleXMLElement.Create(ctx, type, doc.DocumentElement);
            }
            else
            {
                return null; // false
            }
        }

        #endregion

        #region simplexml_import_dom

        /// <summary>
        /// Converts a <see cref="SimpleXMLElement"/> object to a <see cref="DOMElement"/>.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="node">A <see cref="DOMNode"/>.</param>
        /// <param name="className">The name of the class whose instance should be returned (must extend
        /// <see cref="SimpleXMLElement"/>).</param>
        /// <returns>An instance of <see cref="SimpleXMLElement"/> or of the class specified by
        /// <paramref name="className"/>, or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static SimpleXMLElement simplexml_import_dom(Context ctx, DOMNode node, string className = null)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!node.IsAssociated)
            {
                PhpException.Throw(PhpError.Warning, Resources.SimpleXmlImportNotAssociated);
                return null;
            }

            XmlNode xml_node = node.XmlNode;

            // we can import only elements (root elements if the passed node is a document)
            switch (xml_node.NodeType)
            {
                case XmlNodeType.Document:
                    xml_node = ((XmlDocument)xml_node).DocumentElement;
                    if (xml_node != null)
                    {
                        goto case XmlNodeType.Element;
                    }
                    else
                    {
                        goto default;
                    }

                case XmlNodeType.Element:
                    if (TryResolveType(ctx, className, out var type))
                    {
                        return SimpleXMLElement.Create(ctx, type, (XmlElement)xml_node);
                    }
                    else
                    {
                        return null; // false
                    }

                default:
                    PhpException.Throw(PhpError.Warning, Resources.SimpleXmlInvalidNodeToImport);
                    return null; // false
            }
        }

        #endregion

        /// <summary>
        /// Resolves SimpleXMLElement type, or outputs a warning.
        /// </summary>
        private static bool TryResolveType(Context ctx, string className, out PhpTypeInfo type)
        {
            if (className == null || className.Equals(nameof(SimpleXMLElement), StringComparison.OrdinalIgnoreCase))
            {
                type = PhpTypeInfoExtension.GetPhpTypeInfo<SimpleXMLElement>();
                return true;
            }

            // try to resolve the className
            type = ctx.ResolveType(className, default, true);

            if (type == null)
            {
                // TODO: err
                return false;
            }

            if (type.Type.IsSubclassOf(typeof(SimpleXMLElement)))
            {
                return true;
            }
            else
            {
                // we will not allow className which is not derived from SimpleXMLElement
                PhpException.Throw(PhpError.Warning, Resources.SimpleXmlInvalidClassName, type.Name);
                return false;
            }
        }
    }

    /// <summary>
    /// The one and only class comprising the SimpleXML extension.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension("simplexml")]
    public partial class SimpleXMLElement :
        Traversable, ArrayAccess, Pchp.Library.Spl.Countable,
        IPhpConvertible, IPhpComparable, IPhpCloneable, IEnumerable<(PhpValue Key, PhpValue Value)>,
        IPhpJsonSerializable, IPhpPrintable
    // , Serializable // NOTE: this would be exposed to PHP reflection and type cast
    {
        #region enum IterationType

        /// <summary>
        /// Specifies mostly the iteration (<c>foreach</c>) behavior of a <see cref="SimpleXMLElement"/> instance.
        /// </summary>
        internal enum IterationType
        {
            /// <summary>
            /// The instance represents a nonexistent element.
            /// </summary>
            None,

            /// <summary>
            /// The instance represents an attribute.
            /// </summary>
            Attribute,

            /// <summary>
            /// The instance represents the attribute list of an element.
            /// </summary>
            AttributeList,

            /// <summary>
            /// The instance represents an element and iteration will include its siblings.
            /// </summary>
            Element,

            /// <summary>
            /// The instance represents an element and iteration will include its child elements.
            /// </summary>
            ChildElements,
        }

        #endregion

        #region class IterationNamespace

        internal class IterationNamespace
        {
            /// <summary>
            /// The namespace prefix. If it is not null, the prefix is used.
            /// </summary>
            public string namespacePrefix { get; private set; }

            /// <summary>
            /// The namespace of included elements/attributes. (Namespace of prefix if prefix is used)
            /// This value is always not null valid namespace (or empty string).
            /// </summary>
            public string namespaceUri { get; private set; }

            private IterationNamespace(string prefix, string namespaceUri)
            {
                this.namespacePrefix = prefix;
                this.namespaceUri = namespaceUri;
            }

            /// <summary>
            /// Create namespace iteration type by prefix.
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="relatedNode"></param>
            /// <returns></returns>
            public static IterationNamespace CreateWithPrefix(string prefix, XmlNode relatedNode)
            {
                if (prefix == null) prefix = string.Empty;  // is using prefix, it cannot be null

                return new IterationNamespace(prefix, (relatedNode != null) ? relatedNode.GetNamespaceOfPrefix(prefix) : string.Empty);
            }

            /// <summary>
            /// Create namespace iteration type by prefix.
            /// </summary>
            /// <param name="relatedNode"></param>
            /// <returns></returns>
            public static IterationNamespace CreateWithPrefix(XmlNode/*!*/relatedNode)
            {
                return new IterationNamespace(relatedNode.Prefix, relatedNode.NamespaceURI);
            }

            /// <summary>
            /// Create namespace iteration type by full namespace URI. Attributes with default namespace (with empty prefix) will not be included.
            /// </summary>
            /// <param name="namespaceUri"></param>
            /// <returns></returns>
            public static IterationNamespace CreateWithNamespace(string namespaceUri)
            {
                if (namespaceUri == null) namespaceUri = string.Empty;  // namespaceUri is never null in .NET

                return new IterationNamespace(null, namespaceUri);  // do not use prefix, use the whole namespace (different behavior)
            }

            /// <summary>
            /// Determine if the given XML node has the namespace.
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            public bool IsIn(XmlNode/*!*/node)
            {
                Debug.Assert(node != null, "Argument node cannot be null.");

                if (namespacePrefix != null)
                    return node.Prefix == namespacePrefix;
                else
                    return node.NamespaceURI == namespaceUri;
            }

            /// <summary>
            /// Get the node[prefix:name] or node[name, ns] according to the namespace iteration type.
            /// </summary>
            /// <param name="node"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public XmlElement GetFirstChildIn(XmlNode/*!*/node, string/*!*/name)
            {
                Debug.Assert(node != null, "Argument node cannot be null.");
                Debug.Assert(name != null, "Argument name cannot be null.");

                if (namespacePrefix != null)
                    return node[(namespacePrefix.Length == 0) ? (name) : (namespacePrefix + ":" + name)];
                else
                    return node[name, namespaceUri];
            }

            public XmlAttribute GetAttributeIn(XmlAttributeCollection/*!*/attributes, string/*!*/name)
            {
                Debug.Assert(attributes != null, "Argument attributes cannot be null.");
                Debug.Assert(name != null, "Argument name cannot be null.");

                if (namespacePrefix == null)
                    return attributes[name, namespaceUri];
                else // using prefix !
                    return attributes[(namespacePrefix.Length == 0) ? (name) : (namespacePrefix + ":" + name)]; // prefix:name
            }

        }

        #endregion

        #region Fields and Properties

        /// <summary>
        /// Runtime context. Cannot be <c>null</c>.
        /// </summary>
        readonly protected Context _ctx;

        /// <summary>
        /// A class, which will be used when initializing children. Class which extends SimpleXmlElement HAS to be used. 
        /// Non-null value means, that this instance of <see cref="SimpleXMLElement"/> was initialized with specified class.
        /// </summary>
        private protected PhpTypeInfo _class; // the property is not visible in PHP context

        /// <summary>
        /// Whether the class name represents SimpleXMLElement type.
        /// </summary>
        private protected static bool IsSimpleXMLElement(PhpTypeInfo @class) => @class == null || @class.Type == typeof(SimpleXMLElement);

        /// <summary>
        /// Non-<B>null</B> except for construction (between ctor and <see cref="__construct(string,int,bool)"/>
        /// or <see cref="XmlElement"/> setter invocation).
        /// </summary>
        private protected XmlElement _element;

        internal XmlElement XmlElement
        {
            get
            {
                return _element;
            }
            set
            {
                Debug.Assert(value != null);

                _element = value;

                //namespaceUri = value.GetNamespaceOfPrefix(String.Empty);
                iterationNamespace = IterationNamespace.CreateWithPrefix(value);
            }
        }

        /// <summary>
        /// Lazily created namespace manager used for XPath queries.
        /// </summary>
        private protected XmlNamespaceManager _namespaceManager;

        private protected XmlNamespaceManager namespaceManager
        {
            get
            {
                if (_namespaceManager == null)
                {
                    _namespaceManager = new XmlNamespaceManager(XmlElement.OwnerDocument.NameTable);

                    // initialize the manager with prefixes/URIs from the document
                    foreach (var pair in GetNodeNamespaces(XmlElement, true))
                    {
                        if (pair.Value.IsString(out var uri)) // always
                        {
                            _namespaceManager.AddNamespace(pair.Key.String, uri);
                        }
                    }
                }
                return _namespaceManager;
            }
        }

        /// <summary>
        /// The attribute (if this instance represents an individual attribute).
        /// </summary>
        private protected XmlAttribute XmlAttribute;

        /// <summary>
        /// Specifies iteration behavior of this instance (what it actually represents).
        /// </summary>
        private protected IterationType iterationType;

        /// <summary>
		/// The prefix or namespace URI of the elements/attributes that should be iterated and dumped.
		/// </summary>
        private protected IterationNamespace/*!*/ iterationNamespace;

        /// <summary>
        /// A list of names of elements representing the path in the document that should be added
        /// when a field or item is written to this instance.
        /// </summary>
        /// <remarks>
        /// This field supports <c>$doc->elem1->elem2->elem3 = "value"</c>, which creates <c>elem1</c>,
        /// <c>elem2</c>, and <c>elem3</c> if they do not already exist. Becomes non-<B>null</B> when
        /// an unknown element is read.
        /// </remarks>
        private protected List<string> intermediateElements;

        private protected const string textPropertyName = "0";
        private protected const string attributesPropertyName = "@attributes";

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected SimpleXMLElement(Context ctx)
            : this(ctx, null, IterationType.ChildElements, IterationNamespace.CreateWithPrefix(string.Empty, null))
        {
            _class = this.GetPhpTypeInfo();
        }

        /// <summary>
        /// Public constructor. Constructs the inner <see cref="XmlElement"/> with <paramref name="data"/>.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="data">Xml data.</param>
        /// <param name="options">Options.</param>
        /// <param name="dataIsUrl">Whether data points to URL. Default is false.</param>
        /// <param name="ns">Namespace prefix or URI.</param>
        /// <param name="is_prefix">TRUE if ns is a prefix, FALSE if it's a URI; defaults to FALSE.</param>
        public SimpleXMLElement(Context ctx, PhpString data, int options = 0, bool dataIsUrl = false, string ns = "", bool is_prefix = false)
            : this(ctx)
        {
            __construct(data, options, dataIsUrl, ns, is_prefix);
        }

        internal SimpleXMLElement(Context ctx, XmlElement xmlElement, IterationType iterationType, IterationNamespace/*!*/iterationNamespace)
        {
            Debug.Assert(iterationNamespace != null);

            _ctx = ctx;
            _element = xmlElement;

            this.iterationType = iterationType;
            this.iterationNamespace = iterationNamespace;
        }

        internal SimpleXMLElement(Context ctx, XmlElement/*!*/ xmlElement, IterationType iterationType)
            : this(ctx, xmlElement, iterationType, IterationNamespace.CreateWithPrefix(string.Empty, xmlElement)/*xmlElement.GetNamespaceOfPrefix(String.Empty)*/)
        { }

        internal SimpleXMLElement(Context ctx, XmlElement/*!*/ xmlElement)
            : this(ctx, xmlElement, IterationType.ChildElements, IterationNamespace.CreateWithPrefix(string.Empty, xmlElement)/*xmlElement.GetNamespaceOfPrefix(String.Empty)*/)
        { }

        internal SimpleXMLElement(Context ctx, XmlAttribute/*!*/ xmlAttribute, IterationNamespace/*!*/iterationNamespace)
            : this(ctx, xmlAttribute.OwnerElement, IterationType.Attribute, iterationNamespace)
        {
            this.XmlAttribute = xmlAttribute;
        }

        internal SimpleXMLElement(Context ctx, XmlAttribute/*!*/ xmlAttribute)
            : this(ctx, xmlAttribute.OwnerElement, IterationType.Attribute, IterationNamespace.CreateWithPrefix(string.Empty, xmlAttribute)/*xmlAttribute.GetNamespaceOfPrefix(String.Empty)*/)
        {
            this.XmlAttribute = xmlAttribute;
        }

        public void __construct(PhpString data, int options = 0, bool dataIsUrl = false, string ns = "", bool is_prefix = false)
        {
            // TODO: Merge Load with DOMDocument.loadXMLInternal()

            var doc = PhpXmlDocument.Create();
            PhpStream phpstream = null;
            XmlReader reader = null;

            try
            {
                var settings = new XmlReaderSettings()
                {
                    DtdProcessing = DtdProcessing.Parse,
                    //CloseInput = true,
                };

                if (dataIsUrl)
                {
                    if ((phpstream = PhpStream.Open(_ctx, data.ToString(_ctx), StreamOpenMode.ReadText)) != null)
                    {
                        reader = XmlReader.Create(phpstream.RawStream, settings);
                    }
                }
                else
                {
                    if (data.ContainsBinaryData)
                    {
                        reader = XmlReader.Create(
                            new MemoryStream(data.ToBytes(_ctx)),
                            settings);
                    }
                    else
                    {
                        reader = XmlReader.Create(
                            new StringReader(data.ToString(Encoding.UTF8/*not used, string is already encoded*/)),
                            settings);
                    }
                }

                //
                if (reader != null)
                {
                    doc.Load(reader);
                }
            }
            catch (XmlException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
            }
            finally
            {
                reader?.Dispose();
                phpstream?.Dispose();
            }

            if (doc.DocumentElement == null)
            {
                doc.AppendChild(doc.CreateElement("empty"));
            }

            this.XmlElement = doc.DocumentElement;

            iterationNamespace = is_prefix ? IterationNamespace.CreateWithPrefix(ns, XmlElement) : IterationNamespace.CreateWithNamespace(ns);
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="type">The name of the class to create or <B>null</B>.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(Context ctx, PhpTypeInfo type)
        {
            if (IsSimpleXMLElement(type))
            {
                return new SimpleXMLElement(ctx);
            }

            //// try to resolve the className
            //var type = ctx.ResolveType(type, default, true);
            //if (type == null)
            //{
            //    // TODO: err
            //    return null;
            //}

            // we will not allow className which is not derived from SimpleXMLElement
            if (!type.Type.IsSubclassOf(typeof(SimpleXMLElement)))
            {
                PhpException.Throw(PhpError.Warning, Resources.SimpleXmlInvalidClassName, type.Name);
                return null;
            }

            // protected .ctor( Context ctx ) // does not call __construct
            var instance = (SimpleXMLElement)type.CreateUninitializedInstance(ctx);

            //var instance = (SimpleXMLElement)Activator.CreateInstance(
            //    type.Type,
            //    bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly,
            //    binder: null,
            //    args: new object[] { ctx },
            //    culture: System.Globalization.CultureInfo.InvariantCulture);

            return instance;
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="type">The name of the class to create or <B>null</B>.</param>
        /// <param name="xmlElement">The <see cref="XmlElement"/> to wrap.</param>
        /// <param name="iterationType">Iteration behavior of new instance.</param>
        /// <param name="iterationNamespace">The namespace URI of the elements/attributes that should be iterated and dumped.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(Context ctx, PhpTypeInfo type, XmlElement/*!*/ xmlElement, IterationType iterationType, IterationNamespace/*!*/iterationNamespace)
        {
            if (IsSimpleXMLElement(type))
            {
                return new SimpleXMLElement(ctx, xmlElement, iterationType, iterationNamespace);
            }
            else
            {
                var instance = Create(ctx, type);
                instance.XmlElement = xmlElement;
                instance.iterationType = iterationType;
                instance.iterationNamespace = iterationNamespace;

                return instance;
            }
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="type">The name of the class to create or <B>null</B>.</param>
        /// <param name="xmlElement">The <see cref="XmlElement"/> to wrap.</param>
        /// <param name="iterationType">Iteration behavior of new instance.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(Context ctx, PhpTypeInfo type, XmlElement/*!*/ xmlElement, IterationType iterationType)
        {
            if (IsSimpleXMLElement(type))
            {
                return new SimpleXMLElement(ctx, xmlElement, iterationType);
            }
            else
            {
                var instance = Create(ctx, type);
                instance.XmlElement = xmlElement;
                instance.iterationType = iterationType;

                return instance;
            }
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="xmlElement">The <see cref="XmlElement"/> to wrap.</param>
        /// <param name="type">The name of the class to create or <B>null</B>.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(Context ctx, PhpTypeInfo type, XmlElement/*!*/ xmlElement)
        {
            if (IsSimpleXMLElement(type))
            {
                return new SimpleXMLElement(ctx, xmlElement);
            }
            else
            {
                var instance = Create(ctx, type);
                instance.XmlElement = xmlElement;
                instance.iterationNamespace = IterationNamespace.CreateWithPrefix(string.Empty, null);

                return instance;
            }
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="type">The name of the class to create or <B>null</B>.</param>
        /// <param name="xmlAttribute">The <see cref="XmlElement"/> to wrap.</param>
        /// <param name="iterationNamespace">The namespace URI of the elements/attributes that should be iterated and dumped.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(Context ctx, PhpTypeInfo type, XmlAttribute/*!*/ xmlAttribute, IterationNamespace/*!*/iterationNamespace)
        {
            if (IsSimpleXMLElement(type))
            {
                return new SimpleXMLElement(ctx, xmlAttribute, iterationNamespace);
            }
            else
            {
                var instance = Create(ctx, type);
                instance.XmlElement = xmlAttribute.OwnerElement;
                instance.iterationType = IterationType.Attribute;
                instance.iterationNamespace = iterationNamespace;
                instance.XmlAttribute = xmlAttribute;

                return instance;
            }
        }

        /// <summary>
        /// Creates a new <see cref="SimpleXMLElement"/> or a derived class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="type">The name of the class to create or <B>null</B>.</param>
        /// <param name="xmlAttribute">The <see cref="XmlElement"/> to wrap.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> or a derived class.</returns>
        internal static SimpleXMLElement Create(Context ctx, PhpTypeInfo type, XmlAttribute/*!*/ xmlAttribute)
        {
            if (IsSimpleXMLElement(type))
            {
                return new SimpleXMLElement(ctx, xmlAttribute);
            }
            else
            {
                var instance = Create(ctx, type);
                instance.XmlElement = xmlAttribute.OwnerElement;
                instance.iterationType = IterationType.Attribute;
                instance.XmlAttribute = xmlAttribute;

                return instance;
            }
        }

        #endregion

        #region Internal: IPhpConvertible, IPhpCloneable, IPhpJsonSerializable, IPhpPrintable

        /// <summary>
        /// String representation of the XML element.
        /// </summary>
        /// <returns>XML element content.</returns>
        [PhpHidden]
        public override string ToString()
        {
            if (XmlAttribute != null) return XmlAttribute.Value;

            // concatenate text nodes that are immediate children of this element
            var sb = StringBuilderUtilities.Pool.Get();

            foreach (XmlNode child in XmlElement.ChildNodes)
            {
                string text = GetNodeText(child);
                if (text != null) sb.Append(text);
            }

            return StringBuilderUtilities.GetStringAndReturn(sb);
        }

        /// <summary>
        /// String representation of the XML element.
        /// </summary>
        /// <returns>XML element content.</returns>
        public virtual string __toString() => ToString();

        /// <summary>
        /// Internal to-<see cref="int"/> conversion.
        /// </summary>
        long IPhpConvertible.ToLong() => Pchp.Core.Convert.StringToLongInteger(ToString());

        /// <summary>
        /// Internal to-<see cref="double"/> conversion.
        /// </summary>
        double IPhpConvertible.ToDouble() => Pchp.Core.Convert.StringToDouble(ToString());

        /// <summary>
        /// Internal to-<see cref="bool"/> conversion.
        /// </summary>
        bool IPhpConvertible.ToBoolean()
        {
            switch (this.iterationType)
            {
                case IterationType.Attribute:
                    return true;

                #region modified from this.GetEnumerator()

                case IterationType.Element:
                    {
                        // find at least one sibling:
                        for (XmlNode sibling = XmlElement; sibling != null; sibling = sibling.NextSibling)
                            if (sibling.NodeType == XmlNodeType.Element && sibling.LocalName.Equals(XmlElement.LocalName, StringComparison.Ordinal) && iterationNamespace.IsIn(sibling))
                                return true;
                        return false;
                    }

                case IterationType.ChildElements:
                    {
                        // find at least one child element:
                        foreach (XmlNode child in XmlElement)
                            if (child.NodeType == XmlNodeType.Element && iterationNamespace.IsIn(child))
                                return true;
                        return false;
                    }

                case IterationType.AttributeList:
                    {
                        // find at least one attribute
                        foreach (XmlAttribute attr in XmlElement.Attributes)
                            if (!attr.Name.Equals("xmlns", StringComparison.Ordinal) && iterationNamespace.IsIn(attr))
                                return true;
                        return false;
                    }

                #endregion

                default:
                    // return true iff the instance has at least one property
                    return this.GetEnumerator().MoveNext();
            }
        }

        Pchp.Core.Convert.NumberInfo IPhpConvertible.ToNumber(out PhpNumber number) => Pchp.Core.Convert.ToNumber(ToString(), out number);

        object IPhpConvertible.ToClass() => this;

        private protected IEnumerable<KeyValuePair<string, PhpValue>> Properties
        {
            get
            {
                switch (iterationType)
                {
                    case IterationType.None:
                        yield break;
                    case IterationType.Attribute:
                        yield return new KeyValuePair<string, PhpValue>(textPropertyName, XmlAttribute.Value);
                        yield break;
                }

                var properties = new OrderedDictionary((uint)XmlElement.ChildNodes.Count);
                StringBuilder text = null;

                foreach (XmlNode child in XmlElement.ChildNodes)
                {
                    if (properties.Count == 0)
                    {
                        string text_data = GetNodeText(child);
                        if (text_data != null)
                        {
                            if (text == null) text = new StringBuilder(text_data);
                            else text.Append(text_data);
                        }
                    }

                    if (child.NodeType == XmlNodeType.Element)
                    {
                        if ((iterationType == IterationType.ChildElements || iterationType == IterationType.Element) &&
                            iterationNamespace.IsIn(child))
                        {
                            text = null;
                            var child_value = GetChildElementValue(_ctx, _class, (XmlElement)child);

                            if (properties.TryGetValue(child.LocalName, out var element))
                            {
                                // a next element of this name -> create/add to array
                                if (!element.IsPhpArray(out var array))
                                {
                                    properties[child.LocalName] = array = new PhpArray()
                                    {
                                        element,
                                    };
                                }

                                array.Add(child_value);
                            }
                            else
                            {
                                // the first element of this name
                                properties[child.LocalName] = child_value;
                            }

                        }
                    }

                }

                // yield return attributes (if present)
                var attributes = XmlElement.Attributes;
                if (attributes != null && attributes.Count != 0)
                {
                    var attr_array = new PhpArray(attributes.Count);

                    foreach (XmlAttribute attribute in attributes)
                    {
                        if (iterationNamespace.IsIn(attribute) && attribute.Name != "xmlns")
                        {
                            attr_array[attribute.LocalName] = attribute.Value;
                        }
                    }

                    if (attr_array.Count != 0)
                    {
                        yield return new KeyValuePair<string, PhpValue>(attributesPropertyName, attr_array);
                    }
                }

                // yield return the inner text
                if (text != null)
                {
                    yield return new KeyValuePair<string, PhpValue>(textPropertyName, text.ToString());
                }
                else
                {
                    // yield return all child elements
                    foreach (var pair in properties)
                    {
                        yield return new KeyValuePair<string, PhpValue>(pair.Key.ToString(), pair.Value);
                    }
                }
            }
        }

        IEnumerable<KeyValuePair<string, PhpValue>> IPhpJsonSerializable.Properties => Properties;

        IEnumerable<KeyValuePair<string, PhpValue>> IPhpPrintable.Properties => Properties;

        /// <summary>
        /// Invoked when the instance is being cloned.
        /// </summary>
        object IPhpCloneable.Clone()
        {
            SimpleXMLElement clone;
            if (iterationType == IterationType.Attribute)
            {
                clone = Create(_ctx, _class, XmlAttribute, iterationNamespace);
            }
            else
            {
                clone = Create(_ctx, _class, XmlElement, iterationType, iterationNamespace);
            }

            if (intermediateElements != null) clone.intermediateElements = new List<string>(intermediateElements);
            clone._namespaceManager = _namespaceManager;

            return clone;
        }

        #endregion

        #region Magic methods: Property access

        /// <summary>
        /// Special field containing runtime fields.
        /// </summary>
        /// <remarks>
        /// The field is handled by runtime and is not intended for direct use.
        /// Magic methods for property access are ignored without runtime fields.
        /// </remarks>
        [CompilerGenerated]
        internal PhpArray __peach__runtimeFields = null;

        /// <summary>
        /// Property reading (i.e. child element getter).
        /// </summary>
        public virtual PhpValue __get(string name)
        {
            XmlElement child = iterationNamespace.GetFirstChildIn(XmlElement, name);// XmlElement[name, namespaceUri];

            SimpleXMLElement elem;

            if (child != null)
            {
                elem = Create(_ctx, _class, child, IterationType.Element, iterationNamespace /*operating on the current namespace $element->children('namespace ...')->link*/);
            }
            else
            {
                elem = Create(_ctx, _class, XmlElement, IterationType.None);

                if (intermediateElements != null)
                {
                    elem.intermediateElements = new List<string>(intermediateElements);
                }
                else
                {
                    elem.intermediateElements = new List<string>();
                }

                elem.intermediateElements.Add(name);

            }

            return PhpValue.FromClass(elem);
        }


        /// <summary>
        /// Property writing (i.e. child element setter).
        /// </summary>
        public virtual bool __set(string name, PhpValue value)
        {
            if (name == null) return false;

            BuildUpIntermediateElements();

            XmlElement child = null;

            // try to find the child element of the given local name & namespace URI
            foreach (XmlNode node in XmlElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element &&
                    node.LocalName == name &&
                    iterationNamespace.IsIn(node)/*node.NamespaceURI == namespaceUri*/)
                {
                    if (child != null)
                    {
                        // duplicate!
                        PhpException.Throw(PhpError.Warning, Resources.SimpleXmlAssignmentToDuplicateNodes, name);
                        return false;
                    }
                    else
                    {
                        child = (XmlElement)node;
                    }
                }
            }

            if (child == null)
            {
                child = XmlElement.OwnerDocument.CreateElement(name, iterationNamespace.namespaceUri);
                XmlElement.AppendChild(child);
            }

            // check value type
            if (value.IsObject)
            {
                PhpException.Throw(PhpError.Warning, Resources.SimpleXmlUnsupportedWriteConversion);
                return false;
            }

            child.InnerText = StrictConvert.ToString(value, _ctx);
            return true;
        }

        /// <summary>
        /// Property unsetting (i.e. child element remover).
        /// </summary>
        public virtual bool __unset(string name)
        {
            var to_remove = new ValueList<XmlNode>();

            // remove all child elements of the given local name & namespace URI
            foreach (XmlNode node in XmlElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element &&
                    node.LocalName == name &&
                    iterationNamespace.IsIn(node)/*node.NamespaceURI == namespaceUri*/)
                {
                    to_remove.Add(node);
                }
            }

            if (to_remove.Count == 0)
            {
                return false;
            }
            else
            {
                foreach (var node in to_remove)
                {
                    XmlElement.RemoveChild(node);
                }

                return true;
            }
        }

        /// <summary>
        /// Property isset testing (i.e. child element existence test).
        /// </summary>
        public virtual bool __isset(string name)
        {
            var child = iterationNamespace.GetFirstChildIn(XmlElement, name);// XmlElement[name, namespaceUri];

            return child != null;

            //if (child != null) return Create(className, child);
            //else return null;
        }

        #endregion

        #region IPhpComparable CompareTo

        int IPhpComparable.Compare(PhpValue value)
        {
            string strobj = value.AsString();
            if (strobj != null)
            {
                switch (iterationType)
                {
                    case IterationType.Attribute:
                        return string.CompareOrdinal(XmlAttribute.Value, strobj);
                    case IterationType.Element:
                    case IterationType.ChildElements:
                        return string.CompareOrdinal(GetPhpInnerText(XmlElement), strobj);
                    default:
                        break;
                }
            }
            else if (value.IsObject && value.Object is SimpleXMLElement elem)
            {
                // https://stackoverflow.com/a/17953855
                return object.ReferenceEquals(this, elem) ? 0 : 1;
                // TODO: Figure out how to return false simultaneously for >, <, >= and <=
            }

            // return base.CompareTo(obj, comparer);
            throw new NotImplementedException();
        }

        #endregion

        #region IPhpConvertible ToPhpArray

        /// <summary>
        /// Get inner text, child elements only (not recursive).
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        private protected string GetPhpInnerText(XmlNode child)
        {
            string NodeValue = null;

            foreach (XmlNode x in child.ChildNodes)
                if (x.NodeType == XmlNodeType.Text)
                    NodeValue = NodeValue + x.InnerText;

            return NodeValue;
        }

        /// <summary>
        /// Returns given child node as a SimpleXMLElement, or as a simple string.
        /// It depends on its child nodes. (Because of PHP; node is represented as a string, if it has a child node of type Text)
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        private protected PhpValue GetPhpChildElement(XmlNode child)
        {
            if (child == null || child.NodeType != XmlNodeType.Element || !iterationNamespace.IsIn(child)/*child.NamespaceURI != namespaceUri*/)
                return PhpValue.Null;

            // check if the node contains Text node, return only the string
            string NodeValue = GetPhpInnerText(child);

            if (NodeValue != null)
                return (PhpValue)NodeValue;

            // otherwise
            return PhpValue.FromClass(Create(_ctx, _class, (XmlElement)child));
        }

        /// <summary>
        /// Overrides conversion of SimpleXMLElement to array.
        /// </summary>
        /// <returns></returns>
        PhpArray IPhpConvertible.ToArray()
        {
            PhpArray array = new PhpArray();

            if (XmlAttribute != null)
            {
                array.Add(XmlAttribute.Value);
            }
            else
            {
                foreach (XmlNode child in XmlElement)
                {
                    var childElement = GetPhpChildElement(child);
                    if (childElement.IsNull == false)
                    {
                        if (array.ContainsKey(child.LocalName))
                        {
                            var item = array[child.LocalName];
                            var arrayitem = item.AsArray();
                            if (arrayitem == null)
                            {
                                array[child.LocalName] = (PhpValue)new PhpArray(2)
                                {
                                    item,
                                    childElement,
                                };
                            }
                            else
                            {
                                arrayitem.Add(childElement);
                            }
                        }
                        else
                        {
                            array.Add(child.LocalName, childElement);
                        }
                    }
                }
            }

            return array;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Alias to <see cref="asXML"/>.
        /// </summary>
        public PhpValue saveXML(string fileName = null) => asXML(fileName);

        /// <summary>
		/// Return a well-formed XML string based on this <see cref="SimpleXMLElement"/>.
		/// </summary>
        public PhpValue asXML(string fileName = null)
        {
            bool WriteOperation(Stream stream)
            {
                if (stream == null)
                {
                    return false;
                }

                // determine XML settings
                var isRootNode = XmlElement.ParentNode is XmlDocument; // also (XmlElement.ParentNode.NodeType == XmlNodeType.Document)
                var settings = new XmlWriterSettings()
                {
                    Encoding = Utils.GetNodeEncoding(_ctx, XmlElement),
                    OmitXmlDeclaration = !isRootNode, // allow XML declaration only if node is root element
                    // Indent = ???,
                };

                try
                {
                    // use a XML writer and set its Formatting property to Formatting.Indented
                    using (var writer = System.Xml.XmlWriter.Create(stream, settings))
                    {
                        //writer.Formatting = Formatting.Indented;
                        if (isRootNode) XmlElement.ParentNode.WriteTo(writer);
                        else XmlElement.WriteTo(writer);
                    }
                }
                catch (XmlException e)
                {
                    PhpException.Throw(PhpError.Warning, e.Message);
                    return false;
                }

                return true;
            }

            if (fileName == null)
            {
                // return the XML string
                var stream = new MemoryStream();

                if (WriteOperation(stream))
                {
                    return PhpValue.Create(new PhpString(stream.ToArray()));
                }
                else
                {
                    return PhpValue.False;
                }
            }
            else
            {
                // write XML to the file
                using var stream = PhpStream.Open(_ctx, fileName, StreamOpenMode.WriteText);

                if (stream != null && WriteOperation(stream.RawStream))
                {
                    return PhpValue.True;
                }
                else
                {
                    return PhpValue.False;
                }
            }
        }

        /// <summary>
        /// Runs an XPath query on the XML data.
        /// </summary>
        /// <param name="path">The XPath query string.</param>
        /// <returns>A <see cref="PhpArray"/> of <see cref="SimpleXMLElement"/>s or <B>false</B>.</returns>
        [return: CastToFalse]
        public PhpArray xpath(string path)
        {
            if (iterationType != IterationType.ChildElements && iterationType != IterationType.Element) return null;

            XPathNavigator navigator = XmlElement.CreateNavigator();
            XPathNodeIterator iterator;

            // execute the query
            try
            {
                iterator = navigator.Select(path, namespaceManager);
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return null;
            }

            PhpArray result = new PhpArray();

            // add the returned nodes to the resulting array
            while (iterator.MoveNext())
            {
                XmlNode node = iterator.Current.UnderlyingObject as XmlNode;
                if (node != null)
                {
                    switch (node.NodeType)
                    {
                        case XmlNodeType.Element:
                            {
                                result.Add(Create(_ctx, _class, (XmlElement)node));
                                break;
                            }
                        case XmlNodeType.Attribute:
                            {
                                result.Add(Create(_ctx, _class, (XmlAttribute)node));
                                break;
                            }

                        case XmlNodeType.CDATA:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Text:
                        case XmlNodeType.Whitespace:
                            {
                                result.Add(Create(_ctx, _class, (XmlElement)node.ParentNode));
                                break;
                            }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a prefix/ns context for the next XPath query.
        /// </summary>
        /// <param name="prefix">The namespace prefix.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns><B>True</B> on success, <B>false</B> on error.</returns>
        public bool registerXPathNamespace(string prefix, string namespaceUri)
        {
            try
            {
                namespaceManager.AddNamespace(prefix, namespaceUri);
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Identifies the element's attributes.
        /// </summary>
        /// <param name="ns">Namespace URI or prefix of the attributes to identify.</param>
        /// <param name="isPrefix">If <B>true</B> <paramref name="ns"/> denotes a prefix, if <B>false</B> it
        /// is a namespace URI.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> wrapping the same element but enumerating and
        /// dumping only the matching attributes.</returns>
        public SimpleXMLElement attributes(string ns = null, bool isPrefix = false)
        {
            if (iterationType != IterationType.ChildElements && iterationType != IterationType.Element) return null;

            /*if (isPrefix)
			{
				ns = XmlElement.GetNamespaceOfPrefix(ns);
			}*/

            return Create(_ctx, _class, XmlElement, IterationType.AttributeList, (ns == null) ? iterationNamespace : (isPrefix ? IterationNamespace.CreateWithPrefix(ns, XmlElement) : IterationNamespace.CreateWithNamespace(ns)));
        }

        /// <summary>
        /// Identifies the element's child elements.
        /// </summary>
        /// <param name="ns">Namespace URI or prefix of the elements to identify.</param>
        /// <param name="isPrefix">If <B>true</B> <paramref name="ns"/> denotes a prefix, if <B>false</B> it
        /// is a namespace URI.</param>
        /// <returns>A new <see cref="SimpleXMLElement"/> wrapping the same element but enumerating and
        /// dumping only the matching elements.</returns>
        public SimpleXMLElement children(string ns = null, bool isPrefix = false)
        {
            if (iterationType != IterationType.ChildElements && iterationType != IterationType.Element) return null;

            /*if (isPrefix)
            {
                ns = XmlElement.GetNamespaceOfPrefix(ns);
            }*/

            return Create(_ctx, _class, XmlElement, IterationType.ChildElements, (ns == null) ? iterationNamespace : (isPrefix ? IterationNamespace.CreateWithPrefix(ns, XmlElement) : IterationNamespace.CreateWithNamespace(ns)));
        }

        /// <summary>
        /// Returns namespaces used by children of this node.
        /// </summary>
        /// <param name="recursive">If <B>true</B> returns namespaces used by all children recursively.</param>
        /// <returns>An <see cref="PhpArray"/> keyed by prefix with values being namespace URIs.</returns>
        public PhpArray getNamespaces(bool recursive = false)
        {
            return GetNodeNamespaces(XmlElement, recursive);
        }

        /// <summary>
        /// Returns namespaces used by the document.
        /// </summary>
        /// <param name="recursive">If <B>true</B> returns namespaces used by all nodes recursively.</param>
        /// <returns>An <see cref="PhpArray"/> keyed by prefix with values being namespace URIs.</returns>
        public PhpArray getDocNamespaces(bool recursive = false)
        {
            return GetNodeNamespaces(XmlElement.OwnerDocument, recursive);
        }

        /// <summary>
        /// Gets the name of the XML element.
        /// </summary>
        public string getName()
        {
            return (XmlAttribute != null ? XmlAttribute.LocalName : XmlElement.LocalName);
        }

        /// <summary>
        /// Adds a child element to this XML element.
        /// </summary>
        /// <param name="qualifiedName">The qualified name of the element to add.</param>
        /// <param name="value">The optional element value.</param>
        /// <param name="namespaceUri">The optional element namespace URI.</param>
        /// <returns>The <see cref="SimpleXMLElement"/> of the child.</returns>
        public SimpleXMLElement addChild(string qualifiedName, string value = null, string namespaceUri = null)
        {
            XmlElement child;
            try
            {
                if (namespaceUri == null) namespaceUri = iterationNamespace.namespaceUri;// this.namespaceUri;
                child = XmlElement.OwnerDocument.CreateElement(qualifiedName, namespaceUri);

                if (value != null) child.InnerText = value;

                XmlElement.AppendChild(child);
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return null;
            }

            return Create(_ctx, _class, child);
        }

        /// <summary>
        /// Adds an attribute to this XML element.
        /// </summary>
        /// <param name="qualifiedName">The qualified name of the attribute to add.</param>
        /// <param name="value">The attribute value.</param>
        /// <param name="namespaceUri">The optional attribute namespace URI.</param>
        public void addAttribute(string qualifiedName, string value, string namespaceUri = null)
        {
            try
            {
                var attr = namespaceUri == null
                   ? XmlElement.OwnerDocument.CreateAttribute(qualifiedName)
                   : XmlElement.OwnerDocument.CreateAttribute(qualifiedName, namespaceUri);

                attr.Value = value;

                XmlElement.Attributes.Append(attr);
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Wraps a node or returns its inner text if it is an element containing nothing but text.
        /// </summary>
        private static PhpValue GetChildElementValue(Context ctx, PhpTypeInfo type, XmlElement xmlElement)
        {
            // determine whether all children are text-like and concat them
            StringBuilder text = null;

            foreach (XmlNode child in xmlElement.ChildNodes)
            {
                string child_text = GetNodeText(child);
                if (child_text != null)
                {
                    if (text == null) text = new StringBuilder(child_text);
                    else text.Append(child_text);
                }
                else
                {
                    return PhpValue.FromClass(Create(ctx, type, xmlElement));
                }
            }

            return (text == null ? PhpValue.FromClass(Create(ctx, type, xmlElement)) : PhpValue.Create(text.ToString()));
        }

        /// <summary>
        /// Returns the text data if the supplied node is treated as &quot;text&quot;.
        /// </summary>
        private static string GetNodeText(XmlNode node)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.EntityReference: return "&" + node.Name + ";";

                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace: return node.Value;
            }

            return null;
        }

        /// <summary>
		/// Returns an array of namespaces used by children of the given node (recursively).
		/// </summary>
        private static PhpArray GetNodeNamespaces(XmlNode xmlNode, bool recursive)
        {
            var result = new PhpArray();

            XPathNavigator navigator = xmlNode.CreateNavigator();
            XPathNodeIterator iterator = navigator.Select(recursive ? "//namespace::*" : "/*/namespace::*");

            string default_ns = null;

            while (iterator.MoveNext())
            {
                string prefix = iterator.Current.Name;
                if (prefix != "xml")
                {
                    if (prefix.Length == 0)
                    {
                        // do not add the default namespace into the array yet, should be placed at the beginning once (see later)
                        default_ns = iterator.Current.Value;
                    }
                    else
                    {
                        // there may be duplicates
                        result[prefix] = iterator.Current.Value;
                    }
                }
            }

            // the default ns should be at the beginning of the array
            if (default_ns != null)
            {
                result.Prepend(string.Empty, default_ns);
            }

            return result;
        }

        /// <summary>
        /// Returns the <paramref name="index"/>th sibling with the same local name and namespace URI or <B>null</B>.
        /// </summary>
        private XmlElement GetSiblingForIndex(long index)
        {
            if (index <= 0) return XmlElement;

            // getting index-th element of this name
            XmlNode node = XmlElement;
            while ((node = node.NextSibling) != null)
            {
                if (node.NodeType == XmlNodeType.Element &&
                    node.LocalName == XmlElement.LocalName &&
                    node.NamespaceURI == XmlElement.NamespaceURI) index--;

                if (index == 0) return (XmlElement)node;
            }

            return null;
        }

        /// <summary>
        /// Returns the <param name="index"/>th attribute with the current namespace URI or<B>null</B>.
        /// </summary>
        private XmlAttribute GetAttributeForIndex(long index)
        {
            foreach (XmlAttribute attr in XmlElement.Attributes)
            {
                if (iterationNamespace.IsIn(attr))
                {
                    if (index == 0) return attr;
                    index--;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates elements stored in <see cref="intermediateElements"/> when it turns out that
        /// there will be a write.
        /// </summary>
        /// <remarks><seealso cref="intermediateElements"/></remarks>
        private void BuildUpIntermediateElements()
        {
            if (intermediateElements != null)
            {
                XmlElement element = XmlElement;

                // create all missing elements on the path
                foreach (string element_name in intermediateElements)
                {
                    XmlElement subelement = iterationNamespace.GetFirstChildIn(element, element_name);// element[element_name, namespaceUri];
                    if (subelement == null)
                    {
                        subelement = element.OwnerDocument.CreateElement(element_name, iterationNamespace.namespaceUri/*this.namespaceUri*/);
                        element.AppendChild(subelement);
                    }
                    element = subelement;
                }

                XmlElement = element;
                iterationType = IterationType.Element;

                intermediateElements = null;
            }
        }

        #endregion

        #region IEnumerable<(PhpValue, PhpValue)> Members

        public IEnumerator<(PhpValue Key, PhpValue Value)> GetEnumerator()
        {
            switch (iterationType)
            {
                case IterationType.Element:
                    {
                        // yield return siblings
                        for (XmlNode sibling = XmlElement; sibling != null; sibling = sibling.NextSibling)
                        {
                            if (sibling.NodeType == XmlNodeType.Element && sibling.LocalName.Equals(XmlElement.LocalName, StringComparison.Ordinal) && iterationNamespace.IsIn(sibling) /*sibling.NamespaceURI == namespaceUri*/)
                            {
                                yield return (
                                    (PhpValue)sibling.LocalName,
                                    PhpValue.FromClass(Create(_ctx, _class, (XmlElement)sibling, IterationType.ChildElements, iterationNamespace /* preserve namespaceUri */)));
                            }
                        }
                        break;
                    }

                case IterationType.ChildElements:
                    {
                        // yield return child elements
                        foreach (XmlNode child in XmlElement)
                        {
                            if (child.NodeType == XmlNodeType.Element && iterationNamespace.IsIn(child) /*child.NamespaceURI == namespaceUri*/)
                            {
                                yield return ((PhpValue)child.LocalName, PhpValue.FromClass(Create(_ctx, _class, (XmlElement)child)));
                            }
                            /*object childElement = GetPhpChildElement(child);
                            if (childElement != null)
                                yield return new KeyValuePair<object, object>
                                    (child.LocalName, childElement);
                             */
                        }
                        break;
                    }

                case IterationType.AttributeList:
                    {
                        // yield return attributes
                        foreach (XmlAttribute attr in XmlElement.Attributes)
                        {
                            if (!attr.Name.Equals("xmlns", StringComparison.Ordinal) && iterationNamespace.IsIn(attr)/*attr.NamespaceURI == namespaceUri*/)
                            {
                                yield return ((PhpValue)attr.LocalName, PhpValue.FromClass(Create(_ctx, _class, attr)));
                            }
                        }
                        break;
                    }
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var pair in this)
            {
                yield return new DictionaryEntry(pair.Key.ToClr(), pair.Value.ToClr());
            }
        }

        #endregion

        #region ArrayAccess Members

        public PhpValue offsetGet(PhpValue index)
        {
            if (index.TryToIntStringKey(out IntStringKey key))
            {
                if (key.IsInteger)
                {
                    switch (iterationType)
                    {
                        case IterationType.AttributeList:
                            {
                                // return the index-th attribute
                                var attr = GetAttributeForIndex(key.Integer);
                                return attr != null
                                    ? PhpValue.FromClass(Create(_ctx, _class, attr))
                                    : PhpValue.Null;
                            }

                        case IterationType.ChildElements:
                        case IterationType.Element:
                            {
                                // returning the index-th sibling of the same name
                                var element = GetSiblingForIndex(key.Integer);
                                return element != null
                                    ? PhpValue.FromClass(Create(_ctx, _class, element))
                                    : PhpValue.Null;
                            }
                    }
                }
                else
                {
                    if (iterationType == IterationType.AttributeList ||
                        iterationType == IterationType.ChildElements ||
                        iterationType == IterationType.Element)
                    {
                        // getting an attribute
                        var attr = iterationNamespace.GetAttributeIn(XmlElement.Attributes, key.String);// XmlElement.Attributes[key.String, namespaceUri];
                        return attr != null
                            ? PhpValue.FromClass(Create(_ctx, _class, attr, iterationNamespace))
                            : PhpValue.Null;
                    }
                }
            }

            //
            return PhpValue.Null;
        }

        public void offsetSet(PhpValue index, PhpValue value)
        {
            if (index.TryToIntStringKey(out IntStringKey key))
            {
                BuildUpIntermediateElements();

                if (iterationType == IterationType.AttributeList ||
                    iterationType == IterationType.ChildElements ||
                    iterationType == IterationType.Element)
                {
                    if (value.TypeCode == PhpTypeCode.Object)
                    {
                        PhpException.Throw(PhpError.Warning, Resources.SimpleXmlUnsupportedWriteConversion);
                    }
                    else
                    {
                        var value_str = StrictConvert.ToString(value, _ctx);
                        if (key.IsInteger)
                        {
                            if (iterationType == IterationType.AttributeList)
                            {
                                // setting value of the index-th attribute
                                XmlAttribute attr = GetAttributeForIndex(key.Integer);
                                if (attr != null) attr.Value = value_str;
                            }
                            else
                            {
                                // setting value of the index-th sibling of the same name
                                XmlElement element = GetSiblingForIndex(key.Integer);

                                if (element == null)
                                {
                                    element = XmlElement.OwnerDocument.CreateElement(XmlElement.LocalName, iterationNamespace.namespaceUri);
                                    XmlElement.ParentNode.AppendChild(element);
                                }

                                element.InnerText = value_str;
                            }
                        }
                        else
                        {
                            // setting an attribute
                            XmlAttribute attr = iterationNamespace.GetAttributeIn(XmlElement.Attributes, key.String);// XmlElement.Attributes[key.String, namespaceUri];

                            if (attr == null)
                            {
                                attr = XmlElement.Attributes.Append(XmlElement.OwnerDocument.CreateAttribute(key.String, iterationNamespace.namespaceUri));
                            }

                            attr.Value = value_str;
                        }
                    }
                }
            }
        }

        public void offsetUnset(PhpValue index)
        {
            if (index.TryToIntStringKey(out IntStringKey key))
            {
                if (iterationType == IterationType.AttributeList ||
                    iterationType == IterationType.ChildElements ||
                    iterationType == IterationType.Element)
                {
                    if (key.IsInteger)
                    {
                        if (iterationType == IterationType.AttributeList)
                        {
                            // removing the index-th attribute
                            XmlAttribute attr = GetAttributeForIndex(key.Integer);
                            if (attr != null) XmlElement.Attributes.Remove(attr);
                        }
                        else
                        {
                            // removing the index-th sibling of the same name
                            XmlElement element = GetSiblingForIndex(key.Integer);
                            if (element != null) XmlElement.ParentNode.RemoveChild(element);
                        }
                    }
                    else
                    {
                        // removing an attribute
                        XmlAttribute attr = iterationNamespace.GetAttributeIn(XmlElement.Attributes, key.String);// XmlElement.Attributes[key.String, namespaceUri];
                        if (attr != null)
                            XmlElement.Attributes.Remove(attr);
                    }
                }
            }
        }

        public bool offsetExists(PhpValue index)
        {
            if (index.TryToIntStringKey(out IntStringKey key))
            {
                if (iterationType == IterationType.AttributeList ||
                    iterationType == IterationType.ChildElements ||
                    iterationType == IterationType.Element)
                {
                    if (key.IsInteger)
                    {
                        if (iterationType == IterationType.AttributeList)
                        {
                            // testing the index-th attribute
                            return (GetAttributeForIndex(key.Integer) != null);
                        }
                        else
                        {
                            // testing the index-th sibling of the same name
                            return (GetSiblingForIndex(key.Integer) != null);
                        }
                    }
                    else
                    {
                        // testing an attribute
                        return iterationNamespace.GetAttributeIn(XmlElement.Attributes, key.String) != null;// (XmlElement.Attributes[key.String, namespaceUri] != null);
                    }
                }
            }

            return false; // null ?
        }

        #endregion

        #region SPL.Countable

        /// <summary>
        /// Count children in the element.
        /// </summary>
        /// <returns></returns>
        public virtual long count() => this.Count();

        #endregion

        //#region Serializable

        //PhpString Serializable.serialize()
        //{
        //    throw new Pchp.Library.Spl.Exception(string.Format(Pchp.Library.Resources.Resources.serialization_unsupported_type, nameof(SimpleXMLElement)));
        //}

        //void Serializable.unserialize(PhpString serialized)
        //{
        //    throw new Pchp.Library.Spl.Exception(string.Format(Pchp.Library.Resources.Resources.serialization_unsupported_type, nameof(SimpleXMLElement)));
        //}

        //#endregion
    }

    /// <summary>
    /// The SimpleXMLIterator provides recursive iteration over all nodes of a <see cref="SimpleXMLElement"/> object.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension("simplexml")]
    public class SimpleXMLIterator
        : SimpleXMLElement, Pchp.Library.Spl.RecursiveIterator
    {
        #region Fields

        private SimpleXMLIterator _current = null;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected SimpleXMLIterator(Context ctx) : base(ctx) { }
        public SimpleXMLIterator(Context ctx, string data, int options = 0, bool dataIsUrl = false, string ns = "", bool is_prefix = false) : base(ctx, data, options, dataIsUrl, ns, is_prefix) { }
        internal SimpleXMLIterator(Context ctx, XmlElement element) : base(ctx, element) { }

        #endregion

        #region RecursiveIterator

        /// <summary>
        /// Returns the current element.
        /// </summary>
        /// <returns>Returns a <see cref="SimpleXMLIterator"/> object or NULL on failure.</returns>
        public virtual PhpValue current() => (_current == null) ? PhpValue.Null : PhpValue.FromClass(_current);

        /// <summary>
        /// Returns a <see cref="SimpleXMLIterator"/> object containing sub-elements of the current <see cref="SimpleXMLIterator"/> element.
        /// </summary>
        public virtual Pchp.Library.Spl.RecursiveIterator getChildren() => hasChildren() ? _current : null;

        /// <summary>
        /// Checks whether the current <see cref="SimpleXMLIterator"/> element has sub-elements (XmlElement).
        /// </summary>
        public virtual bool hasChildren()
        {
            if (_current != null && _current.XmlElement.HasChildNodes)
            {
                XmlNode next = _current.XmlElement.FirstChild;
                while (next != null && !(next is XmlElement))
                    next = next.NextSibling;

                return next is XmlElement;
            }
            else
                return false;
        }

        /// <summary>
        /// Gets the XML tag name of the current element.
        /// </summary>
        /// <returns>Returns the XML tag name of the element referenced by the current <see cref="SimpleXMLIterator"/> object or FALSE.</returns>
        public virtual PhpValue key() => (_current == null) ? PhpValue.False : _current.XmlElement.Name;

        /// <summary>
        /// Moves the <see cref="SimpleXMLIterator"/> to the next element.
        /// </summary>
        public virtual void next()
        {
            if (_current != null)
            {
                var next = _current.XmlElement.NextSibling;
                while (next != null && !(next is XmlElement))
                    next = next.NextSibling;

                _current = (next is XmlElement element) ? GetIterator(element) : _current = null;
            }
        }

        /// <summary>
        /// Rewinds the <see cref="SimpleXMLIterator"/> to the first element.
        /// </summary>
        public virtual void rewind()
        {
            if (!XmlElement.HasChildNodes)
            {
                _current = null;
            }
            else
            {
                var firstChild = XmlElement.FirstChild; // Seeks the first XmlElement
                while (firstChild != null && !(firstChild is XmlElement))
                    firstChild = firstChild.NextSibling;

                _current = (firstChild is XmlElement element) ? GetIterator(element) : _current = null;
            }
        }

        /// <summary>
        /// Gets <see cref="SimpleXMLIterator"/> or a derided class.
        /// </summary>
        /// <param name="element">XmlElement of the class.</param>
        /// <returns><see cref="SimpleXMLIterator"/> or a derided class.</returns>
        protected SimpleXMLIterator GetIterator(XmlElement element)
        {
            if (this.GetType() == typeof(SimpleXMLIterator))
            {
                return new SimpleXMLIterator(_ctx, element);
            }
            else
            {
                var res = (SimpleXMLIterator)(this.GetPhpTypeInfo().CreateUninitializedInstance(_ctx));
                res.XmlElement = element;
                return res;
            }
        }

        /// <summary>
        /// Checks if the current element is valid after calls to SimpleXMLIterator::rewind() or SimpleXMLIterator::next().
        /// </summary>
        /// <returns>Returns TRUE if the current element is valid, otherwise FALSE</returns>
        public virtual bool valid() => _current != null;

        #endregion
    }
}
