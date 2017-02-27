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

        const int XML_ERROR_NONE = (int)XmlParserError.XML_ERROR_NONE;
        const int XML_ERROR_GENERIC = (int)XmlParserError.XML_ERROR_GENERIC;
        const int XML_ERROR_NO_MEMORY = (int)XmlParserError.XML_ERROR_NO_MEMORY;
        const int XML_ERROR_SYNTAX = (int)XmlParserError.XML_ERROR_SYNTAX;
        const int XML_ERROR_NO_ELEMENTS = (int)XmlParserError.XML_ERROR_NO_ELEMENTS;
        const int XML_ERROR_INVALID_TOKEN = (int)XmlParserError.XML_ERROR_INVALID_TOKEN;
        const int XML_ERROR_UNCLOSED_TOKEN = (int)XmlParserError.XML_ERROR_UNCLOSED_TOKEN;
        const int XML_ERROR_PARTIAL_CHAR = (int)XmlParserError.XML_ERROR_PARTIAL_CHAR;
        const int XML_ERROR_TAG_MISMATCH = (int)XmlParserError.XML_ERROR_TAG_MISMATCH;
        const int XML_ERROR_DUPLICATE_ATTRIBUTE = (int)XmlParserError.XML_ERROR_DUPLICATE_ATTRIBUTE;
        const int XML_ERROR_JUNK_AFTER_DOC_ELEMENT = (int)XmlParserError.XML_ERROR_JUNK_AFTER_DOC_ELEMENT;
        const int XML_ERROR_PARAM_ENTITY_REF = (int)XmlParserError.XML_ERROR_PARAM_ENTITY_REF;
        const int XML_ERROR_UNDEFINED_ENTITY = (int)XmlParserError.XML_ERROR_UNDEFINED_ENTITY;
        const int XML_ERROR_RECURSIVE_ENTITY_REF = (int)XmlParserError.XML_ERROR_RECURSIVE_ENTITY_REF;
        const int XML_ERROR_ASYNC_ENTITY = (int)XmlParserError.XML_ERROR_ASYNC_ENTITY;
        const int XML_ERROR_BAD_CHAR_REF = (int)XmlParserError.XML_ERROR_BAD_CHAR_REF;
        const int XML_ERROR_BINARY_ENTITY_REF = (int)XmlParserError.XML_ERROR_BINARY_ENTITY_REF;
        const int XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF = (int)XmlParserError.XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF;
        const int XML_ERROR_MISPLACED_XML_PI = (int)XmlParserError.XML_ERROR_MISPLACED_XML_PI;
        const int XML_ERROR_UNKNOWN_ENCODING = (int)XmlParserError.XML_ERROR_UNKNOWN_ENCODING;
        const int XML_ERROR_INCORRECT_ENCODING = (int)XmlParserError.XML_ERROR_INCORRECT_ENCODING;
        const int XML_ERROR_UNCLOSED_CDATA_SECTION = (int)XmlParserError.XML_ERROR_UNCLOSED_CDATA_SECTION;
        const int XML_ERROR_EXTERNAL_ENTITY_HANDLING = (int)XmlParserError.XML_ERROR_EXTERNAL_ENTITY_HANDLING;

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
                if (xmlParserResource != null)
                {
                    return xmlParserResource;
                }

                PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_xmlresource);
                return null;
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

                        if (_processNamespaces && elementName.IndexOf(":") >= 0)
                        {
                            string localName = elementName.Substring(elementName.IndexOf(":") + 1);
                            elementName = reader.NamespaceURI + _namespaceSeparator + localName;
                        }

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

                        if (_processNamespaces && elementName.IndexOf(":") >= 0)
                        {
                            string localName = elementName.Substring(elementName.IndexOf(":") + 1);
                            elementName = reader.NamespaceURI + _namespaceSeparator + localName;
                        }

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

                    values.Add(arrayRecord);

                    if (indices != null)
                    {
                        PhpArray elementIndices;

                        if (!indices.ContainsKey(elementRecord.ElementName))
                        {
                            elementIndices = new PhpArray();
                            indices.Add(elementRecord.ElementName, elementIndices);
                        }
                        else
                            elementIndices = (PhpArray)indices[elementRecord.ElementName];

                        // add the max index (last inserted value)
                        elementIndices.Add(values.MaxIntegerKey);
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
    }
}
