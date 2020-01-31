using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// This extension represents a writer that provides a non-cached, forward-only means of generating
    /// streams or files containing XML data.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("xmlwriter")]
    public class XMLWriter
    {
        #region Constants

        private const string DefaultXmlVersion = "1.0";

        #endregion

        #region Fields and properties

        System.Xml.XmlWriter _writer;
        MemoryStream _memoryStream;
        PhpStream _uriPhpStream;

        static XmlWriterSettings DefaultSettings = new XmlWriterSettings()
        {
            Encoding = new UTF8Encoding(false),     // Disable BOM
            Indent = true,
            NewLineChars = "\n",
            WriteEndDocumentOnClose = true
        };

        #endregion

        #region Methods

        public bool endAttribute() => CheckedCall(_writer.WriteEndAttribute);

        public bool endCdata()
        {
            PhpException.FunctionNotSupported(nameof(endCdata));
            return false;
        }

        public bool endComment()
        {
            PhpException.FunctionNotSupported(nameof(endComment));
            return false;
        }

        public bool endDocument()
        {
            PhpException.FunctionNotSupported(nameof(endDocument));
            return false;
        }

        public bool endDtdAttlist()
        {
            PhpException.FunctionNotSupported(nameof(endDtdAttlist));
            return false;
        }

        public bool endDtdElement()
        {
            PhpException.FunctionNotSupported(nameof(endDtdElement));
            return false;
        }

        public bool endDtdEntity()
        {
            PhpException.FunctionNotSupported(nameof(endDtdEntity));
            return false;
        }

        public bool endDtd()
        {
            PhpException.FunctionNotSupported(nameof(endDtd));
            return false;
        }

        public bool endElement()
        {
            PhpException.FunctionNotSupported(nameof(endElement));
            return false;
        }

        public bool endPi()
        {
            PhpException.FunctionNotSupported(nameof(endPi));
            return false;
        }

        public PhpValue flush(bool empty = true)
        {
            if (!CheckedCall(_writer.Flush))
            {
                return false;
            }

            PhpValue result;
            if (_memoryStream != null)
            {
                if (empty)
                {
                    // TODO: Handle situation with writing after flushing
                    _writer.Close();
                }

                result = _memoryStream.ToArray();
            }
            else
            {
                try
                {
                    result = _uriPhpStream.RawStream.Position;
                }
                catch (NotSupportedException)
                {
                    PhpException.Throw(PhpError.Warning, Resources.XmlWritterNumberOfBytesUnsupported);
                    result = 0;
                }

                if (empty)
                {
                    // TODO: Handle situation with writing after flushing
                    _writer.Close();
                }
            }

            return result;
        }

        public bool fullEndElement() => CheckedCall(_writer.WriteFullEndElement);

        public bool openMemory()
        {
            Clear();

            _memoryStream = new MemoryStream();
            _writer = System.Xml.XmlWriter.Create(_memoryStream, DefaultSettings);
            return true;
        }

        public bool openUri(Context ctx, string uri)
        {
            Clear();

            _uriPhpStream = PhpStream.Open(ctx, uri, "wb");
            if (_uriPhpStream == null)
            {
                return false;
            }

            try
            {
                _writer = System.Xml.XmlWriter.Create(_uriPhpStream.RawStream, DefaultSettings);
                return true;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        public PhpString outputMemory(bool flush = true)
        {
            if (_memoryStream == null)
            {
                return PhpString.Empty;
            }

            if (flush)
            {
                this.flush();
            }

            return new PhpString(_memoryStream.ToArray());
        }

        public bool setIndentString(string indentString) => CheckedCall(() => _writer.Settings.IndentChars = indentString);

        public bool setIndent(bool indent) => CheckedCall(() => _writer.Settings.Indent = indent);

        public bool startAttributeNs(string prefix, string name, string uri) => CheckedCall(() => _writer.WriteStartAttribute(prefix, name, uri));

        public bool startAttribute(string name) => CheckedCall(() => _writer.WriteStartAttribute(name));

        public bool startCdata()
        {
            PhpException.FunctionNotSupported(nameof(startCdata));
            return false;
        }

       public bool startComment()
        {
            PhpException.FunctionNotSupported(nameof(startComment));
            return false;
        }

        public bool startDocument(string version = DefaultXmlVersion, string encoding = null, string standalone = null)
        {
            if (version != DefaultXmlVersion)
            {
                PhpException.ArgumentValueNotSupported(nameof(version), version);
            }

            if (encoding != null && !string.Equals(encoding, "utf-8", StringComparison.CurrentCultureIgnoreCase))
            {
                PhpException.ArgumentValueNotSupported(nameof(encoding), encoding);
            }

            if (string.IsNullOrEmpty(standalone))
            {
                return CheckedCall(() => _writer.WriteStartDocument());
            }
            else
            {
                return CheckedCall(() => _writer.WriteStartDocument(standalone == "yes"));
            }
        }

        public bool startDtdAttlist()
        {
            PhpException.FunctionNotSupported(nameof(startDtdAttlist));
            return false;
        }

        public bool startDtdElement()
        {
            PhpException.FunctionNotSupported(nameof(startDtdElement));
            return false;
        }

        public bool startDtdEntity()
        {
            PhpException.FunctionNotSupported(nameof(startDtdEntity));
            return false;
        }

        public bool startDtd()
        {
            PhpException.FunctionNotSupported(nameof(startDtd));
            return false;
        }

        public bool startElementNs(string prefix, string name, string uri) => CheckedCall(() => _writer.WriteStartElement(prefix, name, uri));

        public bool startElement(string name) => CheckedCall(() => _writer.WriteStartElement(name));

        public bool startPi()
        {
            PhpException.FunctionNotSupported(nameof(startPi));
            return false;
        }

    public bool text(string content) => CheckedCall(() => _writer.WriteString(content));

        public bool writeAttributeNs(string prefix, string name, string uri, string content) =>
            CheckedCall(() => _writer.WriteAttributeString(prefix, name, uri, content));

        public bool writeAttribute(string name, string content) => CheckedCall(() => _writer.WriteAttributeString(name, content));

        public bool writeCdata(string content) => CheckedCall(() => _writer.WriteCData(content));

        public bool writeComment(string content) => CheckedCall(() => _writer.WriteComment(content));

        public bool writeDtdAttlist()
        {
            PhpException.FunctionNotSupported(nameof(writeDtdAttlist));
            return false;
        }

        public bool writeDtdElement()
        {
            PhpException.FunctionNotSupported(nameof(writeDtdElement));
            return false;
        }

        public bool writeDtdEntity()
        {
            PhpException.FunctionNotSupported(nameof(writeDtdEntity));
            return false;
        }

        public bool writeDtd()
        {
            PhpException.FunctionNotSupported(nameof(writeDtd));
            return false;
        }

        public bool writeElementNs(string prefix, string name, string uri, string content = null) =>
            CheckedCall(() => _writer.WriteElementString(prefix, name, uri, content));

        public bool writeElement(string name, string content = null) => CheckedCall(() => _writer.WriteElementString(name, content));

        public bool writePi(string target, string content) => CheckedCall(() => _writer.WriteProcessingInstruction(target, content));

        public bool writeRaw(string content) => CheckedCall(() => _writer.WriteRaw(content));

        #endregion

        #region Implementation

        private void Clear()
        {
            _memoryStream = null;
            _uriPhpStream?.Dispose();
            _uriPhpStream = null;
            _writer?.Dispose();
            _writer = null;
        }

        private bool CheckedCall(Action operation)
        {
            if (_writer == null)
            {
                return false;
            }

            try
            {
                operation();
                return true;
            }
            catch (ArgumentException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
            catch (InvalidOperationException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
        }

        #endregion
    }
}
