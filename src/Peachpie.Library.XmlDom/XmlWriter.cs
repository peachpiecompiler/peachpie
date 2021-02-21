using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// This extension represents a writer that provides a non-cached, forward-only means of generating
    /// streams or files containing XML data.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("xmlwriter")]
    public class XMLWriter : IDisposable
    {
        #region Constants

        private protected const string DefaultXmlVersion = "1.0";

        #endregion

        #region Fields and properties

        System.Xml.XmlWriter _writer;
        MemoryStream _memoryStream;
        PhpStream _uriPhpStream;

        // Counts nodes created in Comment, PI or CData section in order to close them properly in endComment/Pi/Cdata method.
        private int _unclosedNodesCount = 0;
        // It checks if Dtd section is empty. 
        private bool _dtdStart = false;

        // The State represents a section, where the xmlwriter is situated. e.g. When you call startComment, the xmlwriter changes its state to Comment
        private enum State
        {
            Comment,
            PI, // Processing instruction
            CDATA,
            DTD,
            DtdElement,
            DtdEntity,
            DtdAttlist
        }

        // There can be actually two levels, where the state of xmlwriter can be situated. You can move to DtdElement if you are in DTD and you can move to CDATA if you are in PI.
        Stack<State> _state = new Stack<State>(2);

        static XmlWriterSettings DefaultSettings = new XmlWriterSettings()
        {
            Encoding = new UTF8Encoding(false),     // Disable BOM
            Indent = false,
            NewLineChars = "\n",
            WriteEndDocumentOnClose = true,
            ConformanceLevel = ConformanceLevel.Auto
        };

        #endregion

        void IDisposable.Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }

        #region Methods

        public bool endAttribute()
        {
            if (_state.Count != 0)
            {
                // You can end an attribute in these states if and only if there are incomplete nodes.
                if ((_state.Peek() == State.Comment || _state.Peek() == State.CDATA) && _unclosedNodesCount != 0)
                    return CheckedCall(_writer.WriteEndAttribute);

                return false;
            }

            return CheckedCall(_writer.WriteEndAttribute);
        }

        public bool endCdata()
        {
            if (_state.Count == 0 || _state.Peek() != State.CDATA)
                return false;

            _state.Pop();

            if (_unclosedNodesCount > 0) // If there are unclosed nodes, close them.
            {
                while (_unclosedNodesCount-- > 0)
                    CheckedCall(() => _writer.WriteEndElement());

                CheckedCall(() => _writer.WriteRaw("]]>"));
                return false;
            }

            return CheckedCall(() => _writer.WriteRaw("]]>"));
        }

        public bool endComment()
        {
            if (_state.Count == 0 || _state.Peek() != State.Comment)
                return false;

            _state.Pop();

            if (_unclosedNodesCount > 0) // If there are unclosed nodes, close them.
            {
                while (_unclosedNodesCount-- > 0)
                    CheckedCall(() => _writer.WriteEndElement());

                CheckedCall(() => _writer.WriteRaw("-->"));
                return false;
            }

            return CheckedCall(() => _writer.WriteRaw("-->"));
        }

        public bool endDocument()
        {
            while (_state.Count != 0) // Closes all open sections.
            {
                switch (_state.Peek())
                {
                    case State.Comment:
                        endComment();
                        break;
                    case State.PI:
                        endPi();
                        break;
                    case State.CDATA:
                        endCdata();
                        break;
                    case State.DTD:
                        endDtd();
                        break;
                    case State.DtdElement:
                        endDtdElement();
                        break;
                    case State.DtdEntity:
                        endDtdEntity();
                        break;
                    case State.DtdAttlist:
                        endDtdAttlist();
                        break;
                    default:
                        return false;
                }
            }

            return CheckedCall(() => _writer.WriteEndDocument());
        }

        public bool endDtdAttlist()
        {
            if (_state.Count == 0 || _state.Peek() != State.DtdAttlist)
                return false;

            _state.Pop();

            return CheckedCall(() => _writer.WriteRaw($">"));
        }

        public bool endDtdElement()
        {
            if (_state.Count == 0 || _state.Peek() != State.DtdElement)
                return false;

            _state.Pop();

            return CheckedCall(() => _writer.WriteRaw($">"));
        }

        public bool endDtdEntity()
        {
            if (_state.Count == 0 || _state.Peek() != State.DtdEntity)
                return false;

            _state.Pop();

            return CheckedCall(() => _writer.WriteRaw($"\">"));
        }

        public bool endDtd()
        {
            if (!_state.Contains(State.DTD))
                return false;

            switch (_state.Peek()) // Closes all open Dtd elements. There can't be common elements like tags..
            {
                case State.DTD:
                    break;
                case State.DtdElement:
                    endDtdElement();
                    break;
                case State.DtdEntity:
                    endDtdEntity();
                    break;
                case State.DtdAttlist:
                    endDtdAttlist();
                    break;
                default:
                    return false;
            }

            _state.Pop();

            // Closes dtd section.
            string end = _writer.Settings.Indent ? _writer.Settings.NewLineChars : "";
            end += _dtdStart ? ">" : "]>";
            _dtdStart = false;

            return CheckedCall(() => _writer.WriteRaw(end));
        }

        public bool endElement()
        {
            if (!EndElementHelper())
                return false;

            return CheckedCall(() => _writer.WriteEndElement());
        }

        public bool endPi()
        {
            if (!_state.Contains(State.PI))
                return false;

            if (_state.Peek() == State.CDATA) // Cdata can be placed into a PI section.
                endCdata();

            _state.Pop();

            return CheckedCall(() => _writer.WriteRaw("?>"));
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

        public bool fullEndElement()
        {
            if (!EndElementHelper())
                return false;

            return CheckedCall(() => _writer.WriteFullEndElement());
        }

        public bool openMemory(Context ctx)
        {
            Clear();
            _memoryStream = new MemoryStream();
            _writer = System.Xml.XmlWriter.Create(_memoryStream, DefaultSettings);
            ctx.RegisterDisposable(this);
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
                ctx.RegisterDisposable(this);
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

        public bool setIndentString(string indentString)
        {
            if (_writer.WriteState != WriteState.Start)
                return false;

            // The settings is read-only, but we can create a new xmlwriter if the current xmlwriter haven't written anything yet. 
            var settings = _writer.Settings.Clone();
            settings.IndentChars = indentString;

            if (_uriPhpStream == null)
                _writer = XmlWriter.Create(_memoryStream, settings);
            else
                _writer = XmlWriter.Create(_uriPhpStream.RawStream, settings);

            return true;
        }

        public bool setIndent(bool indent)
        {
            if (_writer.WriteState != WriteState.Start)
                return false;

            // The settings is read-only, but we can create a new xmlwriter if the current xmlwriter haven't written anything yet. 
            var settings = _writer.Settings.Clone();
            settings.Indent = indent;

            if (_uriPhpStream == null)
                _writer = XmlWriter.Create(_memoryStream, settings);
            else
                _writer = XmlWriter.Create(_uriPhpStream.RawStream, settings);

            return true;
        }

        public bool startAttributeNs(string prefix, string name, string uri)
        {
            if (!StartAttributeHelper())
                return false;

            return CheckedCall(() => _writer.WriteStartAttribute(prefix, name, uri));
        }

        public bool startAttribute(string name)
        {
            if (!StartAttributeHelper())
                return false;

            return CheckedCall(() => _writer.WriteStartAttribute(name));
        }

        public bool startCdata()
        {
            if (_state.Count != 0 && _state.Peek() != State.PI)
            {
                PhpException.Throw(PhpError.Warning, Resources.XmlWritterCDataWrongContext);
                return false;
            }

            _state.Push(State.CDATA);

            return CheckedCall(() => _writer.WriteRaw("<![CDATA["));
        }

        public bool startComment()
        {
            if (_state.Count != 0)
                return false;

            _state.Push(State.Comment);

            return CheckedCall(() => _writer.WriteRaw("<!--"));
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
                bool res = CheckedCall(() => _writer.WriteStartDocument());
                if (!_writer.Settings.Indent) // Php writes a new line character after prolog.
                    res &= CheckedCall(() => _writer.WriteRaw(_writer.Settings.NewLineChars));

                return res;
            }
            else
            {
                bool res = CheckedCall(() => _writer.WriteStartDocument(standalone == "yes"));
                if (!_writer.Settings.Indent) // Php writes a new line character after prolog.
                    res &= CheckedCall(() => _writer.WriteRaw(_writer.Settings.NewLineChars));

                return res;
            }
        }

        public bool startDtdAttlist(string name)
        {
            // DTD elements can be only placed in DTD or Default section.
            if (_state.Count != 0 && _state.Peek() != State.DTD)
                return false;

            _state.Push(State.DtdAttlist);

            CheckDtdStartHelper();
            return CheckedCall(() => _writer.WriteRaw($"<!ATTLIST {name} "));
        }

        public bool startDtdElement(string qualifiedName)
        {
            // DTD elements can be only placed in DTD or default section.
            if (_state.Count != 0 && _state.Peek() != State.DTD)
                return false;

            _state.Push(State.DtdElement);

            CheckDtdStartHelper();
            return CheckedCall(() => _writer.WriteRaw($"<!ELEMENT {qualifiedName} "));
        }

        public bool startDtdEntity(string name, bool isparam)
        {
            // DTD elements can be only placed in DTD or default section.
            if (_state.Count != 0 && _state.Peek() != State.DTD)
                return false;

            _state.Push(State.DtdEntity);

            CheckDtdStartHelper();
            return CheckedCall(() => _writer.WriteRaw(isparam ? $"<!ENTITY % {name} \"" : $"<!ENTITY {name} \""));
        }

        public bool startDtd(string qualifiedName, string publicId = null, string systemId = null)
        {
            if (_state.Count != 0 || // DTD can be only placed in default section and prolog.
              (_writer.Settings.ConformanceLevel == ConformanceLevel.Document && _writer.WriteState != WriteState.Prolog && _writer.WriteState != WriteState.Start))
            {
                PhpException.Throw(PhpError.Warning, Resources.XmlWritterDtdInProlog);
                return false;
            }

            if (String.IsNullOrEmpty(systemId) && !String.IsNullOrEmpty(publicId))
            {
                PhpException.Throw(PhpError.Warning, Resources.XmlWritterSystemIdentifier);
                return false;
            }

            // Makes a doctype
            string doctype = $"<!DOCTYPE {qualifiedName}";
            if (!String.IsNullOrEmpty(publicId))
                doctype += _writer.Settings.Indent ? $"{_writer.Settings.NewLineChars}PUBLIC \"{publicId}\"" : $" PUBLIC \"{publicId}\"";
            if (!String.IsNullOrEmpty(systemId))
                doctype += _writer.Settings.Indent ? $"{_writer.Settings.NewLineChars}SYSTEM \"{systemId}\"" : $" SYSTEM \"{systemId}\"";

            CheckDtdStartHelper();
            _state.Push(State.DTD);
            _dtdStart = true;

            return CheckedCall(() => _writer.WriteRaw(doctype));
        }

        public bool startElementNs(string prefix, string name, string uri)
        {
            if (_state.Count != 0)
            {
                if (_state.Peek() == State.Comment || _state.Peek() == State.CDATA)
                {
                    _unclosedNodesCount++;
                    return CheckedCall(() => _writer.WriteStartElement(prefix, name, uri));
                }

                return false;
            }

            return CheckedCall(() => _writer.WriteStartElement(prefix, name, uri));
        }

        public bool startElement(string name)
        {
            if (_state.Count != 0)
            {
                if (_state.Peek() == State.Comment || _state.Peek() == State.CDATA)
                {
                    _unclosedNodesCount++;
                    return CheckedCall(() => _writer.WriteStartElement(name));
                }

                return false;
            }

            return CheckedCall(() => _writer.WriteStartElement(name));
        }

        public bool startPi(string target)
        {
            if (_state.Count != 0)
                return false;

            _state.Push(State.PI);
            return CheckedCall(() => _writer.WriteRaw($"<?{target} "));
        }

        public bool text(string content)
        {
            if (_state.Count == 0 ||
                ((_state.Peek() == State.Comment || _state.Peek() == State.CDATA) && _unclosedNodesCount != 0))
            {
                // Escapes characters
                return CheckedCall(() => _writer.WriteRaw(content.Escape()));
            }

            if (_state.Peek() == State.DTD)
                CheckDtdStartHelper();

            return CheckedCall(() => _writer.WriteRaw(content));
        }

        public bool writeAttributeNs(string prefix, string name, string uri, string content)
        {
            if (!StartAttributeHelper())
                return false;

            // WriteAttributeString does not escape "
            bool res = true;
            res &= CheckedCall(() => _writer.WriteStartAttribute(prefix, name, uri));
            res &= CheckedCall(() => _writer.WriteRaw(content.Escape()));
            res &= CheckedCall(() => _writer.WriteEndAttribute());
            return res;
        }

        public bool writeAttribute(string name, string content)
        {
            if (!StartAttributeHelper())
                return false;

            // WriteAttributeString does not escape "
            bool res = true;
            res &= CheckedCall(() => _writer.WriteStartAttribute(name));
            res &= CheckedCall(() => _writer.WriteRaw(content.Escape()));
            res &= CheckedCall(() => _writer.WriteEndAttribute());
            return res;
        }

        public bool writeCdata(string content)
        {
            if (_state.Count != 0 && _state.Peek() != State.PI)
            {
                PhpException.Throw(PhpError.Warning, Resources.XmlWritterCDataWrongContext);
                return false;
            }

            return CheckedCall(() => _writer.WriteCData(content));
        }

        public bool writeComment(string content)
        {
            if (_state.Count != 0)
                return false;

            return CheckedCall(() => _writer.WriteComment(content));
        }

        public bool writeDtdAttlist(string name, string content)
        {
            // DTD elements can be only placed in DTD or default section.
            if (_state.Count != 0 && _state.Peek() != State.DTD)
                return false;

            CheckDtdStartHelper();
            return CheckedCall(() => _writer.WriteRaw($"<!ATTLIST {name} {content}>"));
        }

        public bool writeDtdElement(string name, string content)
        {
            // DTD elements can be only placed in DTD or default section.
            if (_state.Count != 0 && _state.Peek() != State.DTD)
                return false;

            CheckDtdStartHelper();
            return CheckedCall(() => _writer.WriteRaw($"<!ELEMENT {name} {content}>"));
        }

        public bool writeDtdEntity(string name, string content, bool pe, string pubid, string sysid, string ndataid)
        {
            // DTD elements can be only placed in DTD or default section.
            if (_state.Count != 0 && _state.Peek() != State.DTD)
                return false;

            if (pe)
                return false;

            CheckDtdStartHelper();
            return CheckedCall(() => _writer.WriteRaw($"<!ENTITY {name} PUBLIC \"{pubid}\" \"{sysid}\" NDATA {ndataid}>"));
        }

        public bool writeDtdEntity(string name, string content, bool pe, string pubid, string sysid)
        {
            // DTD elements can be only placed in DTD or default section.
            if (_state.Count != 0 && _state.Peek() != State.DTD)
                return false;

            if (pe)
                return false;

            CheckDtdStartHelper();
            return CheckedCall(() => _writer.WriteRaw($"<!ENTITY {name} PUBLIC \"{pubid}\" \"{sysid}\">"));
        }

        public bool writeDtdEntity(string name, string content, bool pe = false)
        {
            // DTD elements can be only placed in DTD or default section.
            if (_state.Count != 0 && _state.Peek() != State.DTD)
                return false;

            CheckDtdStartHelper();
            return CheckedCall(() => _writer.WriteRaw(pe ? $"<!ENTITY % {name} \"{content}\">" : $"<!ENTITY {name} \"{content}\">"));
        }

        public bool writeDtd(string name, string publicId = null, string systemId = null, string subset = null)
        {
            if (_state.Count != 0 ||
              (_writer.Settings.ConformanceLevel == ConformanceLevel.Document && _writer.WriteState != WriteState.Prolog && _writer.WriteState != WriteState.Start))
            {
                PhpException.Throw(PhpError.Warning, Resources.XmlWritterDtdInProlog);
                return false;
            }

            if (String.IsNullOrEmpty(systemId) && !String.IsNullOrEmpty(publicId))
            {
                PhpException.Throw(PhpError.Warning, Resources.XmlWritterSystemIdentifier);
                return false;
            }

            // Makes doctype
            string doctype = $"<!DOCTYPE {name}";
            if (!String.IsNullOrEmpty(publicId))
                doctype += _writer.Settings.Indent ? $"{_writer.Settings.NewLineChars}PUBLIC \"{publicId}\"" : $" PUBLIC \"{publicId}\"";
            if (!String.IsNullOrEmpty(systemId))
                doctype += _writer.Settings.Indent ? $"{_writer.Settings.NewLineChars}       \"{systemId}\"" : $" \"{systemId}\"";
            if (!String.IsNullOrEmpty(subset))
                doctype += $" [{subset}]";
            doctype += ">";

            CheckDtdStartHelper();

            return CheckedCall(() => _writer.WriteRaw(doctype));
        }

        public bool writeElementNs(string prefix, string name, string uri, string content = null)
        {
            if (_state.Count != 0 && _state.Peek() != State.Comment && _state.Peek() != State.CDATA)
                return false;

            // WriteElementString does not escape "
            bool res = true;
            res &= CheckedCall(() => _writer.WriteStartElement(prefix, name, uri));
            res &= CheckedCall(() => _writer.WriteRaw(content.Escape()));
            res &= CheckedCall(() => _writer.WriteEndElement());
            return res;
        }

        public bool writeElement(string name, string content = null)
        {
            if (_state.Count != 0 && _state.Peek() != State.Comment && _state.Peek() != State.CDATA)
                return false;

            // WriteElementString does not escape "
            bool res = true;
            res &= CheckedCall(() => _writer.WriteStartElement(name));
            res &= CheckedCall(() => _writer.WriteRaw(content.Escape()));
            res &= CheckedCall(() => _writer.WriteEndElement());
            return res;
        }

        public bool writePi(string target, string content)
        {
            if (_state.Count != 0)
                return false;

            return CheckedCall(() => _writer.WriteProcessingInstruction(target, content));
        }

        public bool writeRaw(string content) => CheckedCall(() => _writer.WriteRaw(content));

        #endregion

        #region Implementation

        /// <summary>
        /// It checks the beginning of Dtd and appends "[" if it is a first element in dtd section or a new line if it is placed in prolog as a first element.
        /// </summary>
        private void CheckDtdStartHelper()
        {
            if (_dtdStart) // We are first in DTD section
            {
                _writer.WriteRaw(" [");
                _dtdStart = false;
            }

            if (_writer.Settings.Indent)
            {
                _writer.WriteRaw(_writer.Settings.NewLineChars);

                if (_state.Count != 0 && _state.Peek() == State.DTD)
                    _writer.WriteRaw(" ");
            }
        }

        /// <summary>
        /// Checks a validity of calling startAttribute method.
        /// </summary>
        private bool StartAttributeHelper()
        {
            if (_state.Count != 0)
            {
                if (_state.Peek() == State.Comment || _state.Peek() == State.CDATA)
                {
                    if (_unclosedNodesCount != 0)
                        return true;

                    if (_state.Peek() == State.Comment)
                        endComment();
                    else
                        endCdata();
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks a validity of calling endElement method.
        /// </summary>
        private bool EndElementHelper()
        {
            if (_state.Count != 0)
            {
                // There is only the one correct situation when it closes an element created in section Comment or CDATA.
                if (_state.Peek() == State.Comment || _state.Peek() == State.CDATA)
                {
                    if (_unclosedNodesCount != 0)
                    {
                        _unclosedNodesCount--;
                        return true;
                    }

                    if (_state.Peek() == State.Comment)
                        endComment();
                    else
                        endCdata();
                }

                return false;
            }

            return true;
        }

        private void Clear()
        {
            _memoryStream = null;
            _uriPhpStream?.Dispose();
            _uriPhpStream = null;
            _writer?.Dispose();
            _writer = null;

            _state = new Stack<State>(2);
            _dtdStart = false;
            _unclosedNodesCount = 0;
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

    #region MethodsProceduralStyle

    [PhpExtension("xmlwriter")]
    public static class XMLWriterFunctions
    {
        #region Constants

        private const string DefaultXmlVersion = "1.0";

        #endregion

        internal sealed class XMLWriterResource : PhpResource
        {
            public XMLWriter Writer { get; }

            public XMLWriterResource(XMLWriter writer)
                : base("xmlwriter")
            {
                Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            }

            protected override void FreeManaged()
            {
                ((IDisposable)Writer).Dispose();
                base.FreeManaged();
            }
        }

        private static XMLWriterResource ValidateXmlWriterResource(PhpResource xmlwriter)
        {
            if (xmlwriter is XMLWriterResource h && h.IsValid)
            {
                return h;
            }
            else if (xmlwriter == null)
            {
                PhpException.ArgumentNull(nameof(xmlwriter));
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Pchp.Library.Resources.Resources.invalid_resource, xmlwriter.TypeName);
            }

            //
            return null;
        }

        #region Methods

        [return: CastToFalse]
        public static PhpResource xmlwriter_open_memory(Context ctx)
        {
            var writer = new XMLWriter();

            if (writer.openMemory(ctx))
                return new XMLWriterResource(writer);
            else
                return null;
        }

        [return: CastToFalse]
        public static PhpResource xmlwriter_open_uri(Context ctx, string uri)
        {
            var writer = new XMLWriter();

            if (writer.openUri(ctx, uri))
                return new XMLWriterResource(writer);
            else
                return null;
        }

        [return: CastToFalse]
        public static PhpString xmlwriter_output_memory(PhpResource xmlwriter, bool flush = true)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return null;

            return resource.Writer.outputMemory(flush);
        }

        public static bool xmlwriter_end_attribute(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endAttribute();
        }

        public static bool xmlwriter_end_cdata(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endCdata();
        }

        public static bool xmlwriter_end_comment(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endComment();
        }

        public static bool xmlwriter_end_document(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endDocument();
        }

        public static bool xmlwriter_end_dtd_attlist(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endDtdAttlist();
        }

        public static bool xmlwriter_end_dtd_element(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endDtdElement();
        }

        public static bool xmlwriter_end_dtd_entity(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endDtdEntity();
        }

        public static bool xmlwriter_end_dtd(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endDtd();
        }

        public static bool xmlwriter_end_element(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endElement();
        }

        public static bool xmlwriter_end_pi(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.endPi();
        }

        public static PhpValue xmlwriter_flush(PhpResource xmlwriter, bool empty = true)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.flush(empty);
        }

        public static bool xmlwriter_full_end_element(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.fullEndElement();
        }

        public static bool xmlwriter_set_indent_string(PhpResource xmlwriter, string indentString)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.setIndentString(indentString);
        }

        public static bool xmlwriter_set_indent(PhpResource xmlwriter, bool indent)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.setIndent(indent);
        }

        public static bool xmlwriter_start_attribute_ns(PhpResource xmlwriter, string prefix, string name, string uri)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startAttributeNs(prefix, name, uri);
        }

        public static bool xmlwriter_start_attribute(PhpResource xmlwriter, string name)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startAttribute(name);
        }

        public static bool xmlwriter_start_cdata(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startCdata();
        }

        public static bool xmlwriter_start_comment(PhpResource xmlwriter)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startComment();
        }

        public static bool xmlwriter_start_document(PhpResource xmlwriter, string version = DefaultXmlVersion, string encoding = null, string standalone = null)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startDocument();
        }

        public static bool xmlwriter_start_dtd_attlist(PhpResource xmlwriter, string name)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startDtdAttlist(name);
        }

        public static bool xmlwriter_start_dtd_element(PhpResource xmlwriter, string name)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startDtdElement(name);
        }

        public static bool xmlwriter_start_dtd_entity(PhpResource xmlwriter, string name, bool isparam)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startDtdEntity(name, isparam);
        }

        public static bool xmlwriter_start_dtd(PhpResource xmlwriter, string qualifiedName, string publicId = null, string systemId = null)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startDtd(qualifiedName, publicId, systemId);
        }

        public static bool xmlwriter_start_element_ns(PhpResource xmlwriter, string prefix, string name, string uri)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startElementNs(prefix, name, uri);
        }

        public static bool xmlwriter_start_element(PhpResource xmlwriter, string name)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startElement(name);
        }

        public static bool xmlwriter_start_pi(PhpResource xmlwriter, string target)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.startPi(target);
        }

        public static bool xmlwriter_text(PhpResource xmlwriter, string content)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.text(content);
        }

        public static bool xmlwriter_write_attribute_ns(PhpResource xmlwriter, string prefix, string name, string uri, string content)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeAttributeNs(prefix, name, uri, content);
        }

        public static bool xmlwriter_write_attribute(PhpResource xmlwriter, string name, string value)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeAttribute(name, value);
        }

        public static bool xmlwriter_write_cdata(PhpResource xmlwriter, string content)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeCdata(content);
        }

        public static bool xmlwriter_write_comment(PhpResource xmlwriter, string content)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeComment(content);
        }

        public static bool xmlwriter_write_dtd_attlist(PhpResource xmlwriter, string name, string content)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeDtdAttlist(name, content);
        }

        public static bool xmlwriter_write_dtd_element(PhpResource xmlwriter, string name, string content)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeDtdElement(name, content);
        }

        public static bool xmlwriter_write_dtd_entity(PhpResource xmlwriter, string name, string content, bool pe = false)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeDtdEntity(name, content, pe);
        }

        public static bool xmlwriter_write_dtd_entity(PhpResource xmlwriter, string name, string content, bool pe, string pubid, string sysid)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeDtdEntity(name, content, pe, pubid, sysid);
        }

        public static bool xmlwriter_write_dtd_entity(PhpResource xmlwriter, string name, string content, bool pe, string pubid, string sysid, string ndataid)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeDtdEntity(name, content, pe, pubid, sysid, ndataid);
        }

        public static bool xmlwriter_write_dtd(PhpResource xmlwriter, string name, string publicId = null, string systemId = null, string subset = null)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeDtd(name, publicId, systemId, subset);
        }

        public static bool xmlwriter_write_element_ns(PhpResource xmlwriter, string prefix, string name, string uri, string content = null)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeElementNs(prefix, name, uri, content);
        }

        public static bool xmlwriter_write_element(PhpResource xmlwriter, string name, string content = null)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeElement(name, content);
        }

        public static bool xmlwriter_write_pi(PhpResource xmlwriter, string target, string content)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writePi(target, content);
        }

        public static bool xmlwriter_write_raw(PhpResource xmlwriter, string content)
        {
            var resource = ValidateXmlWriterResource(xmlwriter);
            if (resource == null)
                return false;

            return resource.Writer.writeRaw(content);
        }

        #endregion
    }

    #endregion

    public static class StringExtensions
    {
        private static string _quoteReplacement = "&quot;";
        private static string _lessReplacement = "&lt;";
        private static string _greaterReplacement = "&gt;";
        private static string _ampresandReplacement = "&amp;";

        /// <summary>
        /// Escapes characters &quot;, &lt;, &gt;, &amp;.
        /// </summary>
        /// <returns>Replaced source with the replacement.</returns>
        public static string Escape(this string source)
        {
            if (String.IsNullOrEmpty(source))
                return "";

            StringBuilder builder = null;

            // Gets a new builder from the pool and appends substring of source, if necessary and appends charReplacement
            void Replace(int index, string charReplacement)
            {
                if (builder == null)
                {
                    builder = StringBuilderUtilities.Pool.Get();
                    builder.Append(source.Substring(0, index));
                }

                builder.Append(charReplacement);
            }

            for (int i = 0; i < source.Length; i++)
            {
                switch (source[i])
                {
                    case '"':
                        Replace(i, _quoteReplacement);
                        break;

                    case '>':
                        Replace(i, _greaterReplacement);
                        break;

                    case '<':
                        Replace(i, _lessReplacement);
                        break;

                    case '&':
                        Replace(i, _ampresandReplacement);
                        break;

                    default:
                        if (builder == null)
                            continue;
                        else
                            builder.Append(source[i]);
                        break;
                }
            }

            return (builder != null) ? StringBuilderUtilities.GetStringAndReturn(builder) : source;
        }
    }
}
