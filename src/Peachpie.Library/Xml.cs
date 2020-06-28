using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Pchp.Core;

namespace Pchp.Library
{
    [PhpExtension("xml")]
    public static class PhpXml
    {
        #region Constants

        [PhpHidden]
        public enum XmlParserError
        {
            XML_ERROR_NONE = 0,
            XML_ERROR_GENERIC = 1,
            XML_ERROR_NO_MEMORY = 1,
            XML_ERROR_SYNTAX = 1,
            XML_ERROR_NO_ELEMENTS = 1,
            XML_ERROR_INVALID_TOKEN = 1,
            XML_ERROR_UNCLOSED_TOKEN = 1,
            XML_ERROR_PARTIAL_CHAR = 1,
            XML_ERROR_TAG_MISMATCH = 1,
            XML_ERROR_DUPLICATE_ATTRIBUTE = 1,
            XML_ERROR_JUNK_AFTER_DOC_ELEMENT = 1,
            XML_ERROR_PARAM_ENTITY_REF = 1,
            XML_ERROR_UNDEFINED_ENTITY = 1,
            XML_ERROR_RECURSIVE_ENTITY_REF = 1,
            XML_ERROR_ASYNC_ENTITY = 1,
            XML_ERROR_BAD_CHAR_REF = 1,
            XML_ERROR_BINARY_ENTITY_REF = 1,
            XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF = 1,
            XML_ERROR_MISPLACED_XML_PI = 1,
            XML_ERROR_UNKNOWN_ENCODING = 1,
            XML_ERROR_INCORRECT_ENCODING = 1,
            XML_ERROR_UNCLOSED_CDATA_SECTION = 1,
            XML_ERROR_EXTERNAL_ENTITY_HANDLING = 1,
        }

        public const int XML_ERROR_NONE = (int)XmlParserError.XML_ERROR_NONE;
        public const int XML_ERROR_GENERIC = (int)XmlParserError.XML_ERROR_GENERIC;
        public const int XML_ERROR_NO_MEMORY = (int)XmlParserError.XML_ERROR_NO_MEMORY;
        public const int XML_ERROR_SYNTAX = (int)XmlParserError.XML_ERROR_SYNTAX;
        public const int XML_ERROR_NO_ELEMENTS = (int)XmlParserError.XML_ERROR_NO_ELEMENTS;
        public const int XML_ERROR_INVALID_TOKEN = (int)XmlParserError.XML_ERROR_INVALID_TOKEN;
        public const int XML_ERROR_UNCLOSED_TOKEN = (int)XmlParserError.XML_ERROR_UNCLOSED_TOKEN;
        public const int XML_ERROR_PARTIAL_CHAR = (int)XmlParserError.XML_ERROR_PARTIAL_CHAR;
        public const int XML_ERROR_TAG_MISMATCH = (int)XmlParserError.XML_ERROR_TAG_MISMATCH;
        public const int XML_ERROR_DUPLICATE_ATTRIBUTE = (int)XmlParserError.XML_ERROR_DUPLICATE_ATTRIBUTE;
        public const int XML_ERROR_JUNK_AFTER_DOC_ELEMENT = (int)XmlParserError.XML_ERROR_JUNK_AFTER_DOC_ELEMENT;
        public const int XML_ERROR_PARAM_ENTITY_REF = (int)XmlParserError.XML_ERROR_PARAM_ENTITY_REF;
        public const int XML_ERROR_UNDEFINED_ENTITY = (int)XmlParserError.XML_ERROR_UNDEFINED_ENTITY;
        public const int XML_ERROR_RECURSIVE_ENTITY_REF = (int)XmlParserError.XML_ERROR_RECURSIVE_ENTITY_REF;
        public const int XML_ERROR_ASYNC_ENTITY = (int)XmlParserError.XML_ERROR_ASYNC_ENTITY;
        public const int XML_ERROR_BAD_CHAR_REF = (int)XmlParserError.XML_ERROR_BAD_CHAR_REF;
        public const int XML_ERROR_BINARY_ENTITY_REF = (int)XmlParserError.XML_ERROR_BINARY_ENTITY_REF;
        public const int XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF = (int)XmlParserError.XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF;
        public const int XML_ERROR_MISPLACED_XML_PI = (int)XmlParserError.XML_ERROR_MISPLACED_XML_PI;
        public const int XML_ERROR_UNKNOWN_ENCODING = (int)XmlParserError.XML_ERROR_UNKNOWN_ENCODING;
        public const int XML_ERROR_INCORRECT_ENCODING = (int)XmlParserError.XML_ERROR_INCORRECT_ENCODING;
        public const int XML_ERROR_UNCLOSED_CDATA_SECTION = (int)XmlParserError.XML_ERROR_UNCLOSED_CDATA_SECTION;
        public const int XML_ERROR_EXTERNAL_ENTITY_HANDLING = (int)XmlParserError.XML_ERROR_EXTERNAL_ENTITY_HANDLING;

        [PhpHidden]
        public enum XmlOption
        {
            XML_OPTION_CASE_FOLDING,
            XML_OPTION_SKIP_TAGSTART,
            XML_OPTION_SKIP_WHITE,
            XML_OPTION_TARGET_ENCODING
        }

        public const int XML_OPTION_CASE_FOLDING = (int)XmlOption.XML_OPTION_CASE_FOLDING;
        public const int XML_OPTION_SKIP_TAGSTART = (int)XmlOption.XML_OPTION_SKIP_TAGSTART;
        public const int XML_OPTION_SKIP_WHITE = (int)XmlOption.XML_OPTION_SKIP_WHITE;
        public const int XML_OPTION_TARGET_ENCODING = (int)XmlOption.XML_OPTION_TARGET_ENCODING;

        #endregion

        #region XmlParserResource

        /// <summary>
        /// A resource object representing state of XML parsing.
        /// </summary>
        sealed class XmlParserResource : PhpResource
        {
            enum ElementState
            {
                Beginning,
                Interior
            }

            class ElementRecord
            {
                public int Level;
                public string ElementName;
                public ElementState State;
                public PhpArray Attributes;
            }

            class TextRecord
            {
                public string Text;
            }

            #region Fields & Properties

            /// <summary>
            /// Bound <see cref="Context"/>. Cannot be <c>null</c>.
            /// </summary>
            public Context Context => _ctx;
            readonly Context _ctx;

            private Encoding _outputEncoding;
            private bool _processNamespaces;
            private string _namespaceSeparator;
            private Queue<string> _inputQueue;

            /// <summary>
            /// <c>True</c> iff the parser has no not-parsed data left.
            /// </summary>
            internal bool InputQueueIsEmpty { get { return _inputQueue == null || _inputQueue.Count == 0; } }

            public int CurrentLineNumber { get { return _lastLineNumber; } }
            private int _lastLineNumber;

            public int CurrentColumnNumber { get { return _lastColumnNumber; } }
            private int _lastColumnNumber;

            public int CurrentByteIndex { get { return _lastByteIndex; } }
            private int _lastByteIndex;

            public IPhpCallable DefaultHandler { get; set; }

            public IPhpCallable StartElementHandler { get; set; }

            public IPhpCallable EndElementHandler { get; set; }

            public IPhpCallable CharacterDataHandler { get; set; }

            public IPhpCallable StartNamespaceDeclHandler { get; set; }

            public IPhpCallable EndNamespaceDeclHandler { get; set; }

            public IPhpCallable ProcessingInstructionHandler { get; set; }

            public object HandlerObject { get; set; }

            public bool EnableCaseFolding { get; set; }

            public bool EnableSkipWhitespace { get; set; }

            public int ErrorCode { get { return _errorCode; } }
            private int _errorCode;

            #endregion

            #region Helper functions

            void InvokeDefaultHandler(string value = "")
            {
                DefaultHandler?.Invoke(_ctx, PhpValue.FromClass(this), (PhpValue)value);
            }

            internal static XmlParserResource ValidResource(PhpResource handle)
            {
                var xmlParserResource = handle as XmlParserResource;
                if (xmlParserResource != null && xmlParserResource.IsValid)
                {
                    return xmlParserResource;
                }

                PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_xmlresource);
                return null;
            }

            /// <summary>
            /// Convert value into <see cref="IPhpCallable"/> allowing method names declared on <see cref="HandlerObject"/>.
            /// </summary>
            internal IPhpCallable ToCallback(PhpValue value)
            {
                // empty variable
                if (value.IsEmpty)
                {
                    return null;
                }

                // method name given as string:
                if (this.HandlerObject != null)
                {
                    var name = value.ToStringOrNull();
                    if (name != null)
                    {
                        return PhpCallback.Create(this.HandlerObject, name, default(RuntimeTypeHandle));
                    }
                }

                // default PHP callback:
                return value.AsCallable(default(RuntimeTypeHandle));
            }

            #endregion

            public bool Parse(string input, bool isFinal)
            {
                // the problem is when isFinal == false
                // XmlReader (more precisely XmlTextReaderImpl) synchronously waits for data from underlying stream when Read is called
                // and there is no way to tell whether we have sufficient amount of data for the next Read call
                // and if underlying stream ends prematurely, reader will get into Error state (so these simple workarounds are not possible)

                // current solution caches the data until isFinal == true and then performs the parsing
                // this is not memory efficient (usually this method gets called in a cycle on small chunks to save memory)

                // other way would be to let the reader wait on another thread (in thread pool), which would not be that bad
                // since XmlParser gets freed eventually

                // theoretically the best way would be to implement XmlReader, that would be able to recognize whether there is enough
                // data, but we have not further analyzed this possibility since it seems to result in unappropriate amount of work

                // yet another possible way is to use parser for inner element, and let it come into error state (not tested or thought through)
                // this does not work since inner parser can only be created when the parser reads an element (not in the beginning)

                if (isFinal)
                {
                    if (input == null) input = string.Empty;
                    StringBuilder sb = new StringBuilder(input.Length);

                    if (_inputQueue != null)
                    {
                        foreach (string s in _inputQueue)
                            sb.Append(s);

                        _inputQueue = null;
                    }

                    sb.Append(input);

                    return ParseInternal(sb.ToString(), null, null);
                }
                else
                {
                    //just reset these values - we are still in the beginning
                    _lastLineNumber = 0;
                    _lastColumnNumber = 0;
                    _lastLineNumber = 0;

                    if (!string.IsNullOrEmpty(input))
                    {
                        if (_inputQueue == null)
                            _inputQueue = new Queue<string>();

                        _inputQueue.Enqueue(input);
                    }

                    return true;
                }
            }

            public bool ParseIntoStruct(string input, PhpArray values, PhpArray indices)
            {
                return ParseInternal(input, values, indices);
            }

            private bool ParseInternal(string xml, PhpArray values, PhpArray indices)
            {
                var stringReader = new StringReader(xml);
                var reader = XmlReader.Create(stringReader);
                Stack<ElementRecord> elementStack = new Stack<ElementRecord>();
                TextRecord textChunk = null;

                while (reader.ReadState == ReadState.Initial || reader.ReadState == ReadState.Interactive)
                {
                    try
                    {
                        reader.Read();
                    }
                    catch (XmlException)
                    {
                        _lastLineNumber = ((IXmlLineInfo)reader).LineNumber;
                        _lastColumnNumber = ((IXmlLineInfo)reader).LinePosition;
                        _lastByteIndex = -1;
                        _errorCode = (int)XmlParserError.XML_ERROR_GENERIC;
                        return false;
                    }

                    //these are usually required
                    _lastLineNumber = ((IXmlLineInfo)reader).LineNumber;
                    _lastColumnNumber = ((IXmlLineInfo)reader).LinePosition;

                    // we cannot do this - we could if we had underlying stream, but that would require
                    // encoding string -> byte[] which is pointless


                    switch (reader.ReadState)
                    {
                        case ReadState.Error:
                            //report error
                            break;
                        case ReadState.EndOfFile:
                            //end of file
                            break;
                        case ReadState.Closed:
                        case ReadState.Initial:
                            //nonsense
                            Debug.Fail(null);
                            break;
                        case ReadState.Interactive:
                            //debug step, that prints out the current state of the parser (pretty printed)
                            //Debug_ParseStep(reader);
                            ParseStep(reader, elementStack, ref textChunk, values, indices);
                            break;
                    }

                    if (reader.ReadState == ReadState.Error || reader.ReadState == ReadState.EndOfFile || reader.ReadState == ReadState.Closed)
                        break;
                }

                return true;
            }

            private void ParseStep(XmlReader reader, Stack<ElementRecord> elementStack, ref TextRecord textChunk, PhpArray values, PhpArray indices)
            {
                string elementName;
                bool emptyElement;
                ElementRecord currentElementRecord = null;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        elementName = reader.Name;
                        emptyElement = reader.IsEmptyElement;
                        PhpArray attributeArray = new PhpArray();

                        //if (_processNamespaces && elementName.IndexOf(":") >= 0)
                        //{
                        //    string localName = elementName.Substring(elementName.IndexOf(":") + 1);
                        //    elementName = reader.NamespaceURI + _namespaceSeparator + localName;
                        //}

                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                if (_processNamespaces && reader.Name.StartsWith("xmlns:"))
                                {
                                    string namespaceID = reader.Name.Substring(6);
                                    string namespaceUri = reader.Value;

                                    if (StartNamespaceDeclHandler != null)
                                        StartNamespaceDeclHandler.Invoke(_ctx, PhpValue.FromClass(this), (PhpValue)namespaceID, (PhpValue)namespaceUri);

                                    continue;
                                }

                                attributeArray.Add(EnableCaseFolding ? reader.Name.ToUpperInvariant() : reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        // update current top of stack
                        if (elementStack.Count != 0)
                        {
                            currentElementRecord = elementStack.Peek();

                            UpdateValueAndIndexArrays(currentElementRecord, ref textChunk, values, indices, true);

                            if (currentElementRecord.State == ElementState.Beginning)
                                currentElementRecord.State = ElementState.Interior;
                        }

                        // push the element into the stack (needed for parse_into_struct)
                        elementStack.Push(
                            new ElementRecord()
                            {
                                ElementName = elementName,
                                Level = reader.Depth,
                                State = ElementState.Beginning,
                                Attributes = (PhpArray)attributeArray.DeepCopy()
                            });

                        if (StartElementHandler != null)
                            StartElementHandler.Invoke(_ctx, PhpValue.FromClass(this), (PhpValue)(EnableCaseFolding ? elementName.ToUpperInvariant() : elementName), (PhpValue)attributeArray);
                        else
                            InvokeDefaultHandler();

                        if (emptyElement) goto case XmlNodeType.EndElement;    // and end the element immediately (<element/>, XmlNodeType.EndElement will not be called)

                        break;


                    case XmlNodeType.EndElement:
                        elementName = reader.Name;

                        //if (_processNamespaces && elementName.IndexOf(":") >= 0)
                        //{
                        //    string localName = elementName.Substring(elementName.IndexOf(":") + 1);
                        //    elementName = reader.NamespaceURI + _namespaceSeparator + localName;
                        //}

                        // pop the top element record
                        currentElementRecord = elementStack.Pop();

                        UpdateValueAndIndexArrays(currentElementRecord, ref textChunk, values, indices, false);

                        if (EndElementHandler != null)
                            EndElementHandler.Invoke(_ctx, PhpValue.FromClass(this), (PhpValue)(EnableCaseFolding ? elementName.ToUpperInvariant() : elementName));
                        else
                            InvokeDefaultHandler();
                        break;


                    case XmlNodeType.Whitespace:
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        if (textChunk == null)
                        {
                            textChunk = new TextRecord() { Text = reader.Value };
                        }
                        else
                        {
                            textChunk.Text += reader.Value;
                        }

                        if (CharacterDataHandler != null)
                            CharacterDataHandler.Invoke(_ctx, PhpValue.FromClass(this), (PhpValue)reader.Value);
                        else
                            InvokeDefaultHandler(reader.Value);
                        break;

                    case XmlNodeType.ProcessingInstruction:

                        if (ProcessingInstructionHandler != null)
                            ProcessingInstructionHandler.Invoke(_ctx, PhpValue.FromClass(this), (PhpValue)reader.Name, (PhpValue)reader.Value);
                        else
                            InvokeDefaultHandler();
                        break;
                }
            }

            private void UpdateValueAndIndexArrays(ElementRecord elementRecord, ref TextRecord textRecord, PhpArray values, PhpArray indices, bool middle)
            {
                // if we have no valid data in the middle, just end
                if (middle && textRecord == null)
                    return;

                if (!middle && elementRecord.State == ElementState.Interior)
                    UpdateValueAndIndexArrays(elementRecord, ref textRecord, values, indices, true);

                if (values != null)
                {
                    PhpArray arrayRecord = new PhpArray();

                    arrayRecord.Add("tag", elementRecord.ElementName);
                    arrayRecord.Add("level", elementRecord.Level);

                    if (elementRecord.State == ElementState.Beginning)
                        arrayRecord.Add("type", middle ? "open" : "complete");
                    else
                        arrayRecord.Add("type", middle ? "cdata" : "close");

                    if (textRecord != null)
                        arrayRecord.Add("value", textRecord.Text);

                    if (elementRecord.State == ElementState.Beginning && elementRecord.Attributes.Count != 0)
                        arrayRecord.Add("attributes", elementRecord.Attributes);

                    var value_idx = values.Add(arrayRecord) - 1;

                    if (indices != null)
                    {
                        var elementIndices = indices[elementRecord.ElementName].AsArray();

                        if (elementIndices == null)
                        {
                            indices[elementRecord.ElementName] = elementIndices = new PhpArray();
                        }

                        // add the max index (last inserted value)
                        elementIndices.Add(value_idx);
                    }
                }

                textRecord = null;
            }

            public XmlParserResource(Context ctx, Encoding outputEncoding, bool processNamespaces, string namespaceSeparator)
                : base("XmlParser")
            {
                Debug.Assert(ctx != null);

                _ctx = ctx;
                _outputEncoding = outputEncoding;
                _processNamespaces = processNamespaces;
                _namespaceSeparator = namespaceSeparator != null ? namespaceSeparator.Substring(0, 1) : ":";

                EnableCaseFolding = true;
                EnableSkipWhitespace = false;
            }
        }

        #endregion

        #region utf8_encode, utf8_decode

        /// <summary>
        /// ISO-8859-1 <see cref="Encoding"/>.
        /// </summary>
        static Encoding/*!*/ISO_8859_1_Encoding
        {
            get
            {
                if (_ISO_8859_1_Encoding == null)
                {
                    _ISO_8859_1_Encoding = Encoding.GetEncoding("ISO-8859-1");
                    Debug.Assert(_ISO_8859_1_Encoding != null);
                }

                return _ISO_8859_1_Encoding;
            }
        }
        static Encoding _ISO_8859_1_Encoding = null;

        /// <summary>
        /// This function encodes the string data to UTF-8, and returns the encoded version. UTF-8 is
        /// a standard mechanism used by Unicode for encoding wide character values into a byte stream.
        /// UTF-8 is transparent to plain ASCII characters, is self-synchronized (meaning it is 
        /// possible for a program to figure out where in the bytestream characters start) and can be
        /// used with normal string comparison functions for sorting and such. PHP encodes UTF-8
        /// characters in up to four bytes.
        /// </summary>
        /// <param name="data">An ISO-8859-1 string. </param>
        /// <returns>Returns the UTF-8 translation of data.</returns>
        //[return:CastToFalse]
        public static string utf8_encode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }

            // this function transforms ISO-8859-1 binary string into UTF8 string
            // since our internal representation is native CLR string - UTF16, we have changed this semantic

            //string encoded;

            //if (!data.ContainsBinayString)
            //{
            //    encoded = (string)data;
            //}
            //else
            //{
            //    // if we got binary string, assume it's ISO-8859-1 encoded string and convert it to System.String
            //    encoded = ISO_8859_1_Encoding.GetString((data).ToBytes);
            //}

            //// return utf8 encoded data
            //return (Configuration.Application.Globalization.PageEncoding == Encoding.UTF8) ?
            //    (object)encoded : // PageEncoding is UTF8, we can keep .NET string, which will be converted to UTF8 byte stream as it would be needed
            //    (object)new PhpBytes(Encoding.UTF8.GetBytes(encoded));   // conversion of string to byte[] would not respect UTF8 encoding, convert it now

            return data;
        }

        /// <summary>
        /// This function decodes data, assumed to be UTF-8 encoded, to ISO-8859-1.
        /// </summary>
        /// <param name="data">An ISO-8859-1 string. </param>
        /// <returns>Returns the UTF-8 translation of data.</returns>
        public static PhpString utf8_decode(string data)
        {
            if (data == null)
            {
                return new PhpString();  // empty (binary) string
            }

            // this function converts the UTF8 representation to ISO-8859-1 representation
            // we assume CLR string (UTF16) as input as it is our internal representation

            // if we got System.String string, convert it from UTF16 CLR representation into ISO-8859-1 binary representation
            return new PhpString(ISO_8859_1_Encoding.GetBytes(data));
        }

        #endregion

        #region xml_parser_create_ns, xml_parser_create, xml_parser_free

        /// <summary>
        /// Creates a new XML parser with XML namespace support and returns a resource handle referencing
        /// it to be used by the other XML functions. 
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="encoding">
        /// The optional encoding specifies the character encoding for the input/output in PHP 4. Starting
        /// from PHP 5, the input encoding is automatically detected, so that the encoding parameter
        /// specifies only the output encoding. In PHP 4, the default output encoding is the same as the
        /// input charset. In PHP 5.0.0 and 5.0.1, the default output charset is ISO-8859-1, while in PHP
        /// 5.0.2 and upper is UTF-8. The supported encodings are ISO-8859-1, UTF-8 and US-ASCII. 
        /// </param>
        /// <param name="namespaceSeparator">
        /// With a namespace aware parser tag parameters passed to the various handler functions will 
        /// consist of namespace and tag name separated by the string specified in seperator.
        /// </param>
        /// <returns>Returns a resource handle for the new XML parser.</returns>
        public static PhpResource xml_parser_create_ns(Context ctx, string encoding = null, string namespaceSeparator = ":")
        {
            return new XmlParserResource(ctx, string.IsNullOrEmpty(encoding) ? Encoding.UTF8 : Encoding.GetEncoding(encoding), true, namespaceSeparator);
        }

        /// <summary>
        /// Creates a new XML parser and returns a resource handle referencing it to be used by the other
        /// XML functions. 
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="encoding">
        /// The optional encoding specifies the character encoding for the input/output in PHP 4. Starting
        /// from PHP 5, the input encoding is automatically detected, so that the encoding parameter
        /// specifies only the output encoding. In PHP 4, the default output encoding is the same as the
        /// input charset. If empty string is passed, the parser attempts to identify which encoding the
        /// document is encoded in by looking at the heading 3 or 4 bytes. In PHP 5.0.0 and 5.0.1, the
        /// default output charset is ISO-8859-1, while in PHP 5.0.2 and upper is UTF-8. The supported
        /// encodings are ISO-8859-1, UTF-8 and US-ASCII. 
        /// </param>
        /// <returns>Returns a resource handle for the new XML parser.</returns>
        public static PhpResource xml_parser_create(Context ctx, string encoding = null)
        {
            return xml_parser_create_ns(ctx, encoding);
        }

        /// <summary>
        /// Frees the given XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to free.</param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it frees the 
        /// parser and returns TRUE.
        /// </returns>
        public static bool xml_parser_free(PhpResource parser)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            // Since .NET hasn't online XML parser, we need the whole XML data to parse them properly.
            // Notice user, he has to parse the XML by passing is_final=true to the last xml_parse function call.
            if (!xmlParser.InputQueueIsEmpty)
                PhpException.Throw(PhpError.Notice, Resources.LibResources.not_parsed_data_left);

            xmlParser.Dispose();
            return true;
        }

        #endregion

        #region xml_parse, xml_parse_into_struct

        /// <summary>
        /// Parses an XML document. The handlers for the configured events are called as many times as 
        /// necessary. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to use.</param>
        /// <param name="data">
        /// Chunk of data to parse. A document may be parsed piece-wise by calling xml_parse() several 
        /// times with new data, as long as the is_final parameter is set and TRUE when the last data is 
        /// parsed. 
        /// </param>
        /// <param name="is_final">If set and TRUE, data is the last piece of data sent in this parse.</param>
        /// <returns>
        /// <para>Returns 1 on success or 0 on failure.</para>
        /// <para>
        /// For unsuccessful parses, error information can be retrieved with xml_get_error_code(), 
        /// xml_error_string(), xml_get_current_line_number(), xml_get_current_column_number() and 
        /// xml_get_current_byte_index(). 
        /// </para>
        /// </returns>
        public static int xml_parse(PhpResource parser, string data, bool is_final = false)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);

            return (xmlParser != null && xmlParser.Parse(data, is_final)) ? 1 : 0;
        }

        /// <summary>
        /// This function parses an XML string into 2 parallel array structures, one (index) containing
        /// pointers to the location of the appropriate values in the values array. These last two 
        /// parameters must be passed by reference. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser. </param>
        /// <param name="data">A string containing the XML data. </param>
        /// <param name="values">An array containing the values of the XML data.</param>
        /// <param name="index">
        /// An array containing pointers to the location of the appropriate values in the $values.
        /// </param>
        /// <returns>
        /// Returns 0 for failure and 1 for success. This is not the same as FALSE and TRUE, be careful
        /// with operators such as ===.
        /// </returns>
        public static int xml_parse_into_struct(PhpResource parser, string data, PhpAlias values, PhpAlias index = null)
        {
            if (values == null)
            {
                PhpException.Throw(PhpError.Warning, "values argument should not be null");
                return 0;
            }

            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null)
            {
                var values_arr = new PhpArray();
                values.Value = (PhpValue)values_arr;

                PhpArray index_arr;
                if (index != null)
                {
                    index.Value = (PhpValue)(index_arr = new PhpArray());
                }
                else
                {
                    index_arr = null;
                }

                return xmlParser.ParseIntoStruct(data, values_arr, index_arr) ? 1 : 0;
            }

            PhpException.Throw(PhpError.Warning, "parser argument should contain valid XML parser");
            return 0;
        }

        #endregion

        #region xml_parser_get_option, xml_parser_set_option

        /// <summary>
        /// Sets an option in an XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to set an option in. </param>
        /// <param name="option">
        /// One of the following options: XML_OPTION_CASE_FOLDING, XML_OPTION_SKIP_TAGSTART,
        /// XML_OPTION_SKIP_WHITE, XML_OPTION_TARGET_ENCODING.
        /// </param>
        /// <param name="value">The option's new value. </param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or if the option could
        /// not be set. Else the option is set and TRUE is returned.
        /// </returns>
        public static bool xml_parser_set_option(PhpResource parser, XmlOption option, PhpValue value)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null)
            {
                switch (option)
                {
                    case XmlOption.XML_OPTION_CASE_FOLDING:
                        xmlParser.EnableCaseFolding = value.ToBoolean();
                        return true;
                    case XmlOption.XML_OPTION_SKIP_WHITE:
                        xmlParser.EnableSkipWhitespace = value.ToBoolean();
                        return true;
                    case XmlOption.XML_OPTION_SKIP_TAGSTART:
                    case XmlOption.XML_OPTION_TARGET_ENCODING:
                    default:
                        PhpException.Throw(PhpError.Warning, "invalid option value");
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an option value from an XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get an option from. </param>
        /// <param name="option">
        /// Which option to fetch. XML_OPTION_CASE_FOLDING and XML_OPTION_TARGET_ENCODING are available.
        /// </param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser or if option isn't valid
        /// (generates also a E_WARNING). Else the option's value is returned. 
        /// </returns>
        public static PhpValue xml_parser_get_option(PhpResource parser, int option)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null)
            {
                switch ((XmlOption)option)
                {
                    case XmlOption.XML_OPTION_CASE_FOLDING:
                        return (PhpValue)xmlParser.EnableCaseFolding;
                    case XmlOption.XML_OPTION_SKIP_WHITE:
                        return (PhpValue)xmlParser.EnableSkipWhitespace;
                    case XmlOption.XML_OPTION_SKIP_TAGSTART:
                    case XmlOption.XML_OPTION_TARGET_ENCODING:
                    default:
                        PhpException.Throw(PhpError.Warning, "invalid option value");
                        return PhpValue.False;
                }
            }
            else
            {
                return PhpValue.False;
            }
        }

        #endregion

        #region xml_error_string, xml_get_error_code

        /// <summary>
        /// Gets the XML parser error string associated with the given code.
        /// </summary>
        /// <param name="code">An error code from xml_get_error_code().</param>
        /// <returns>
        /// Returns a string with a textual description of the error code, or FALSE if no description 
        /// was found.
        /// </returns>
        [return: CastToFalse]
        public static string xml_error_string(XmlParserError code)
        {
            switch (code)
            {
                case XmlParserError.XML_ERROR_GENERIC:
                    return "Generic XML parser error - error strings are not supported yet.";

                case XmlParserError.XML_ERROR_NONE:
                    return "No Error.";

                default:
                    return "Unknown XML parser error.";
            }
        }

        /// <summary>
        /// Gets the XML parser error code. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get error code from.</param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it returns 
        /// one of the error codes.
        /// </returns>
        [return: CastToFalse]
        public static int xml_get_error_code(PhpResource parser)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null)
            {
                return xmlParser.ErrorCode;
            }

            return -1;
        }

        #endregion

        #region xml_get_current_byte_index, xml_get_current_column_number, xml_get_current_line_number

        /// <summary>
        /// Gets the current byte index of the given XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get byte index from.</param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it returns 
        /// which byte index the parser is currently at in its data buffer (starting at 0). 
        /// </returns>
        [return: CastToFalse]
        public static int xml_get_current_byte_index(PhpResource parser)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null)
            {
                return xmlParser.CurrentByteIndex;
            }

            return -1;
        }

        /// <summary>
        /// Gets the current column number of the given XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get column number from. </param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it returns 
        /// which column on the current line (as given by xml_get_current_line_number()) the parser is 
        /// currently at. 
        /// </returns>
        [return: CastToFalse]
        public static int xml_get_current_column_number(PhpResource parser)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null)
            {
                return xmlParser.CurrentColumnNumber;
            }

            return -1;
        }

        /// <summary>
        /// Gets the current line number for the given XML parser. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to get line number from.</param>
        /// <returns>
        /// This function returns FALSE if parser does not refer to a valid parser, or else it returns 
        /// which line the parser is currently at in its data buffer. 
        /// </returns>
        [return: CastToFalse]
        public static int xml_get_current_line_number(PhpResource parser)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null)
            {
                return xmlParser.CurrentLineNumber;
            }

            return -1;
        }

        #endregion

        #region xml_set_object

        /// <summary>
        /// This function allows to use parser inside object. All callback functions could be set with 
        /// xml_set_element_handler() etc and assumed to be methods of object. 
        /// </summary>
        /// <param name="parser">A reference to the XML parser to use inside the object. </param>
        /// <param name="objRef">The object where to use the XML parser.</param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool xml_set_object(PhpResource parser, PhpValue objRef)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null)
            {
                xmlParser.HandlerObject = objRef.AsObject();
                return true;
            }

            return false;
        }

        #endregion

        #region xml_set_default_handler, xml_set_unparsed_entity_decl_handler

        /// <summary>
        /// Sets the default handler function for the XML parser parser.
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up default handler function. 
        /// </param>
        /// <param name="default_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool xml_set_default_handler(PhpResource parser, PhpValue default_handler)
        {
            IPhpCallable callback;
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null && (callback = xmlParser.ToCallback(default_handler)) != null)
            {
                xmlParser.DefaultHandler = callback;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the unparsed entity declaration handler function for the XML parser parser. 
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up unparsed entity declaration handler function. 
        /// </param>
        /// <param name="unparsed_entity_decl_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool SetUnparsedEntityDeclHandler(PhpResource parser, PhpValue unparsed_entity_decl_handler)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            PhpException.FunctionNotSupported("xml_set_unparsed_entity_decl_handler");
            return false;
        }

        #endregion

        #region xml_set_element_handler, xml_set_character_data_handler
        /// <summary>
        /// Sets the element handler functions for the XML parser. start_element_handler and 
        /// end_element_handler are strings containing the names of functions that must exist 
        /// when xml_parse() is called for parser.  
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up start and end element handler functions. 
        /// </param>
        /// <param name="start_element_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <param name="end_element_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>        
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool xml_set_element_handler(PhpResource parser, PhpValue start_element_handler, PhpValue end_element_handler)
        {
            IPhpCallable callback_start, callback_end;
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null && (callback_start = xmlParser.ToCallback(start_element_handler)) != null && (callback_end = xmlParser.ToCallback(end_element_handler)) != null)
            {
                xmlParser.StartElementHandler = callback_start;
                xmlParser.EndElementHandler = callback_end;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the character data handler function for the XML parser parser.  
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up character data handler function.
        /// </param>
        /// <param name="character_data_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool xml_set_character_data_handler(PhpResource parser, PhpValue character_data_handler)
        {
            IPhpCallable callback;
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null && (callback = xmlParser.ToCallback(character_data_handler)) != null)
            {
                xmlParser.CharacterDataHandler = callback;
                return true;
            }

            return false;
        }

        #endregion

        #region xml_set_start_namespace_decl_handler, xml_set_end_namespace_decl_handler

        /// <summary>
        /// Set a handler to be called when a namespace is declared. Namespace declarations occur 
        /// inside start tags. But the namespace declaration start handler is called before the start 
        /// tag handler for each namespace declared in that start tag.  
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser. 
        /// </param>
        /// <param name="start_namespace_decl_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool xml_set_start_namespace_decl_handler(PhpResource parser, PhpValue start_namespace_decl_handler)
        {
            IPhpCallable callback;
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null && (callback = xmlParser.ToCallback(start_namespace_decl_handler)) != null)
            {
                xmlParser.StartNamespaceDeclHandler = callback;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set a handler to be called when leaving the scope of a namespace declaration. This will 
        /// be called, for each namespace declaration, after the handler for the end tag of the 
        /// element in which the namespace was declared. 
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser.
        /// </param>
        /// <param name="end_namespace_decl_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool xml_set_end_namespace_decl_handler(PhpResource parser, PhpValue end_namespace_decl_handler)
        {
            IPhpCallable callback;
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null && (callback = xmlParser.ToCallback(end_namespace_decl_handler)) != null)
            {
                xmlParser.EndNamespaceDeclHandler = callback;

                return true;
            }

            return false;
        }

        #endregion

        #region xml_set_notation_decl_handler, xml_set_processing_instruction_handler, xml_set_external_entity_ref_handler

        /// <summary>
        /// Sets the notation declaration handler function for the XML parser parser. 
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up notation declaration handler function. 
        /// </param>
        /// <param name="notation_decl_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool xml_set_notation_decl_handler(PhpResource parser, PhpValue notation_decl_handler)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            PhpException.FunctionNotSupported("xml_set_notation_decl_handler");
            return false;
        }

        /// <summary>
        /// Sets the processing instruction (PI) handler function for the XML parser parser. 
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up processing instruction (PI) handler function.  
        /// </param>
        /// <param name="processing_instruction_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool xml_set_processing_instruction_handler(PhpResource parser, PhpValue processing_instruction_handler)
        {
            IPhpCallable callback;
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser != null && (callback = xmlParser.ToCallback(processing_instruction_handler)) != null)
            {
                xmlParser.ProcessingInstructionHandler = callback;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the external entity reference handler function for the XML parser parser.  
        /// </summary>
        /// <param name="parser">
        /// A reference to the XML parser to set up external entity reference handler function. 
        /// </param>
        /// <param name="external_entity_ref_handler">
        /// String (or array) containing the name of a function that must exist when xml_parse() is 
        /// called for parser. 
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure. </returns>
        public static bool xml_set_external_entity_ref_handler(PhpResource parser, PhpValue external_entity_ref_handler)
        {
            var xmlParser = XmlParserResource.ValidResource(parser);
            if (xmlParser == null)
                return false;

            PhpException.FunctionNotSupported("xml_set_external_entity_ref_handler");
            return false;
        }
        #endregion
    }
}
