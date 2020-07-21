using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Pchp.Core;
using Pchp.Library;
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

        private const string DefaultXmlVersion = "1.0";

        #endregion

        #region Fields and properties

        System.Xml.XmlWriter _writer;
        MemoryStream _memoryStream;
        PhpStream _uriPhpStream;

        // Flags for CData, Comment and PISection, DTD
        private int countOfNastedNodes = 0;
        private Section section = Section.Default;
        private bool dtdStart = false;

        [Flags]
        private enum Section { Default = 0, Comment = 1, PI = 2, CDATA = 4, DTD = 8, DtdElement = 16, DtdEntity = 32, DtdAttribute = 64 }

        static XmlWriterSettings DefaultSettings = new XmlWriterSettings()
        {
            Encoding = new UTF8Encoding(false),     // Disable BOM
            Indent = false,
            NewLineChars = "\n",
            WriteEndDocumentOnClose = true,
            ConformanceLevel = ConformanceLevel.Auto
        };

        #endregion

        public void Dispose()
        {
            _writer?.Dispose();
            _writer = null;
        }

        #region Methods

        public bool endAttribute()
        {
            if ((section & Section.PI) == Section.PI)
                return false;

            // Cannot end attributes in comments, where are not elements
            if ((section == Section.Comment || section == Section.CDATA) && countOfNastedNodes == 0)
                return false;

            return CheckedCall(_writer.WriteEndAttribute);
        }

        public bool endCdata()
        {
            if ((section & Section.CDATA) != Section.CDATA)
                return false;

            if ((section & Section.PI) == Section.PI)
                section = Section.PI;
            else
                section = Section.Default;

            return CheckedCall(() => _writer.WriteRaw("]]>"));
        }

        public bool endComment()
        {
            if (section != Section.Comment)
                return false;

            section = Section.Default;

            if (countOfNastedNodes > 0)
            {
                while (countOfNastedNodes-- > 0)
                    _writer.WriteEndElement();

                CheckedCall(() => _writer.WriteRaw("-->"));
                return false;
            }

            return CheckedCall(() => _writer.WriteRaw("-->"));
        }

        public bool endDocument()
        {
            EndDtdElements();

            if (section == Section.Comment)
                endComment();
            else if ((section & Section.PI) == Section.PI)
                endPi();
            else if (section == Section.CDATA)
                endCdata();
            else if ((section & Section.DTD) == Section.DTD)
                endDtd();

            CheckedCall(() => _writer.WriteEndDocument());

            return CheckedCall(() => _writer.WriteEndDocument());
        }

        public bool endDtdAttlist()
        {
            if ((section & Section.DtdAttribute) != Section.DtdAttribute)
                return false;

            section = section & Section.DTD;

            return CheckedCall(() => _writer.WriteRaw($">"));
        }

        public bool endDtdElement()
        {
            if ((section & Section.DtdElement) != Section.DtdElement)
                return false;

            section = section & Section.DTD;

            return CheckedCall(() => _writer.WriteRaw($">"));
        }

        public bool endDtdEntity()
        {
            if ((section & Section.DtdEntity) != Section.DtdEntity)
                return false;

            section = section & Section.DTD;

            return CheckedCall(() => _writer.WriteRaw($"\">"));
        }

        public bool endDtd()
        {
            if ((section & Section.DTD) != Section.DTD)
                return false;

            EndDtdElements();
            section = Section.Default;

            // Ends dtd section.
            string end = _writer.Settings.Indent ? _writer.Settings.NewLineChars : "";
            end += dtdStart ? ">" : "]>";
            dtdStart = false;

            return CheckedCall(() => _writer.WriteRaw(end));
        }

        public bool endElement()
        {
            if ((section & Section.PI) == Section.PI)
                return false;

            if ((section == Section.Comment || section == Section.CDATA))
            {
                if (countOfNastedNodes == 0)
                {
                    if (section == Section.Comment)
                        endComment();
                    else
                        endCdata();

                    return false;
                }
                else
                    countOfNastedNodes--;
            }

            return CheckedCall(() => _writer.WriteEndElement());
        }

        public bool endPi()
        {
            if ((section & Section.PI) != Section.PI)
                return false;

            if ((section & Section.CDATA) == Section.CDATA)
                endCdata();

            section = Section.Default;

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
            if ((section & Section.PI) == Section.PI)
                return false;

            // Cannot start end element in comments or cdata, where are not elements.
            if ((section == Section.Comment || section == Section.CDATA) && countOfNastedNodes == 0)
            {
                if (section == Section.Comment)
                    endComment();
                else
                    endCdata();

                return false;
            }

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
            if ((section & Section.PI) == Section.PI)
                return false;

            // Cannot start attributes in comments or cdata, where are not elements.
            if ((section == Section.Comment || (section & Section.CDATA) == Section.CDATA) && countOfNastedNodes == 0)
            {
                if (section == Section.Comment)
                    endComment();
                else
                    endCdata();
                return false;
            }

            return CheckedCall(() => _writer.WriteStartAttribute(prefix, name, uri));
        }

        public bool startAttribute(string name)
        {
            if ((section & Section.PI) == Section.PI)
                return false;

            // Cannot start attributes in comments or cdata, where are not elements.
            if ((section == Section.Comment || section == Section.CDATA) && countOfNastedNodes == 0)
            {
                if (section == Section.Comment)
                    endComment();
                else
                    endCdata();
                return false;
            }

            return CheckedCall(() => _writer.WriteStartAttribute(name));
        }

        public bool startCdata()
        {
            if (section != Section.Default && section != Section.PI)
            {
                PhpException.Throw(PhpError.Warning, Resources.XmlWritterCDataWrongContext);
                return false;
            }
      
            if (section == Section.PI)
                section = section | Section.CDATA;
            else
                section = Section.CDATA;

            return CheckedCall(() => _writer.WriteRaw("<![CDATA["));
        }

        public bool startComment()
        {
            if (section != Section.Default)
                return false;

            section = Section.Comment;
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
                if (!_writer.Settings.Indent)
                    res &= CheckedCall(() => _writer.WriteRaw(_writer.Settings.NewLineChars));

                return res;
            }
            else
            {
                bool res = CheckedCall(() => _writer.WriteStartDocument(standalone == "yes"));
                if (!_writer.Settings.Indent)
                    res &= CheckedCall(() => _writer.WriteRaw(_writer.Settings.NewLineChars));

                return res;
            }
        }

        public bool startDtdAttlist(string name)
        {
            if (section != Section.DTD && section != Section.Default)
                return false;

            section = section | Section.DtdAttribute;

            CheckDtdStart();
            return CheckedCall(() => _writer.WriteRaw($"<!ATTLIST {name} "));
        }

        public bool startDtdElement(string qualifiedName)
        {
            // DTD elements can be only in DTD or Default section.
            if (section != Section.DTD && section != Section.Default)
                return false;

            section = section | Section.DtdElement;

            CheckDtdStart();
            return CheckedCall(() => _writer.WriteRaw($"<!ELEMENT {qualifiedName} "));
        }

        public bool startDtdEntity(string name, bool isparam)
        {
            // DTD entity can be only in DTD or Default section.
            if (section != Section.DTD && section != Section.Default)
                return false;

            section = section | Section.DtdEntity;

            CheckDtdStart();
            return CheckedCall(() => _writer.WriteRaw(isparam ? $"<!ENTITY % {name} \"" : $"<!ENTITY {name} \""));
        }

        public bool startDtd(string qualifiedName, string publicId = null, string systemId = null)
        {
            if (section != Section.Default ||
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
            string doctype = $"<!DOCTYPE {qualifiedName}";
            if (!String.IsNullOrEmpty(publicId))
                doctype += _writer.Settings.Indent ? $"{_writer.Settings.NewLineChars}PUBLIC \"{publicId}\"" : $" PUBLIC \"{publicId}\"";
            if (!String.IsNullOrEmpty(systemId))
                doctype += _writer.Settings.Indent ? $"{_writer.Settings.NewLineChars}SYSTEM \"{systemId}\"" : $" SYSTEM \"{systemId}\"";

            CheckDtdStart();
            section = Section.DTD;
            dtdStart = true;

            return CheckedCall(() => _writer.WriteRaw(doctype));
        }

        public bool startElementNs(string prefix, string name, string uri)
        {
            if ((section & Section.PI) == Section.PI)
                return false;

            // Cannot start namespace in comments or cdata, where are not elements.
            if ((section == Section.Comment || section == Section.CDATA) && countOfNastedNodes == 0)
            {
                if (section == Section.Comment)
                    endComment();
                else
                    endCdata();
                return false;
            }

            return CheckedCall(() => _writer.WriteStartElement(prefix, name, uri));
        }

        public bool startElement(string name)
        {
            if (section == Section.PI)
                return false;

            if (section == Section.Comment || section == Section.CDATA)
                countOfNastedNodes++;

            return CheckedCall(() => _writer.WriteStartElement(name));
        }

        public bool startPi(string target)
        {
            if (section != Section.Default)
                return false;

            section = Section.PI;
            return CheckedCall(() => _writer.WriteRaw($"<?{target} "));
        }

        public bool text(string content)
        {
            if (section == Section.Default ||
                ((section == Section.Comment || section == Section.CDATA) && countOfNastedNodes != 0))
            {
                // Escapes characters
                return CheckedCall(() => _writer.WriteRaw(content.Escape(escapedChars)));
            }

            if (section == Section.DTD)
                CheckDtdStart();

            return CheckedCall(() => _writer.WriteRaw(content));
        }

        public bool writeAttributeNs(string prefix, string name, string uri, string content)
        {
            if ((section & Section.PI) == Section.PI)
                return false;

            if ((section == Section.Comment || section == Section.CDATA) && countOfNastedNodes == 0)
            {
                if (section == Section.Comment)
                    endComment();
                else
                    endCdata();
                return false;
            }
            // WriteAttributeString does not escape "
            bool res = true;
            res &= CheckedCall(() => _writer.WriteStartAttribute(prefix, name, uri));
            res &= CheckedCall(() => _writer.WriteRaw(content.Escape()));
            res &= CheckedCall(() => _writer.WriteEndAttribute());
            return res;
        }

        public bool writeAttribute(string name, string content)
        {
            if ((section & Section.PI) == Section.PI)
                return false;

            if ((section == Section.Comment || section == Section.CDATA) && countOfNastedNodes == 0)
            {
                if (section == Section.Comment)
                    endComment();
                else
                    endCdata();
                return false;
            }
            // WriteAttributeString does not escape "
            bool res = true;
            res &= CheckedCall(() => _writer.WriteStartAttribute(name));
            res &= CheckedCall(() => _writer.WriteRaw(content.Escape()));
            res &= CheckedCall(() => _writer.WriteEndAttribute());
            return res;
        }

        public bool writeCdata(string content)
        {
            if (section != Section.Default && section != Section.PI)
            {
                PhpException.Throw(PhpError.Warning, Resources.XmlWritterCDataWrongContext);
                return false;
            }

            return CheckedCall(() => _writer.WriteCData(content));
        }

        public bool writeComment(string content)
        {
            if (section != Section.Default)
                return false;

            return CheckedCall(() => _writer.WriteComment(content));
        }

        public bool writeDtdAttlist(string name, string content)
        {
            // DTD attlist can be only in DTD or Default section.
            if (section != Section.DTD && section != Section.Default)
                return false;

            CheckDtdStart();
            return CheckedCall(() => _writer.WriteRaw($"<!ATTLIST {name} {content}>"));
        }

        public bool writeDtdElement(string name, string content)
        {
            // DTD elements can be only in DTD or Default section.
            if (section != Section.DTD && section != Section.Default)
                return false;

            CheckDtdStart();
            return CheckedCall(() => _writer.WriteRaw($"<!ELEMENT {name} {content}>"));
        }

        public bool writeDtdEntity(string name, string content, bool pe, string pubid, string sysid, string ndataid)
        {
            // DTD entity can be only in DTD or Default section.
            if (section != Section.DTD && section != Section.Default)
                return false;

            if (pe)
                return false;

            CheckDtdStart();
            return CheckedCall(() => _writer.WriteRaw($"<!ENTITY {name} PUBLIC \"{pubid}\" \"{sysid}\" NDATA {ndataid}>"));
        }

        public bool writeDtdEntity(string name, string content, bool pe, string pubid, string sysid)
        {
            // DTD entity can be only in DTD or Default section.
            if (section != Section.DTD && section != Section.Default)
                return false;

            if (pe)
                return false;

            CheckDtdStart();
            return CheckedCall(() => _writer.WriteRaw($"<!ENTITY {name} PUBLIC \"{pubid}\" \"{sysid}\">"));
        }

        public bool writeDtdEntity(string name, string content, bool pe = false)
        {
            // DTD entity can be only in DTD or Default section.
            if (section != Section.DTD && section != Section.Default)
                return false;

            CheckDtdStart();
            return CheckedCall(() => _writer.WriteRaw(pe ? $"<!ENTITY % {name} \"{content}\">" : $"<!ENTITY {name} \"{content}\">"));
        }

        public bool writeDtd(string name, string publicId = null, string systemId = null, string subset = null)
        {
            if (section != Section.Default ||
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

            CheckDtdStart();

            return CheckedCall(() => _writer.WriteRaw(doctype));
        }

        public bool writeElementNs(string prefix, string name, string uri, string content = null)
        {
            if ((section & Section.PI) == Section.PI)
                return false;

            if ((section == Section.Comment || section == Section.CDATA) && countOfNastedNodes == 0)
            {
                if (section == Section.Comment)
                    endComment();
                else
                    endCdata();

                return false;
            }
            // WriteElementString does not escape "
            bool res = true;
            res &= CheckedCall(() => _writer.WriteStartElement(prefix, name, uri));
            res &= CheckedCall(() => _writer.WriteRaw(content.Escape()));
            res &= CheckedCall(() => _writer.WriteEndElement());
            return res;
        }

        public bool writeElement(string name, string content = null) 
        {
            if ((section & Section.PI) == Section.PI)
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
            if (section != Section.Default)
                return false;

            return CheckedCall(() => _writer.WriteProcessingInstruction(target, content));
        }

        public bool writeRaw(string content) => CheckedCall(() => _writer.WriteRaw(content));

        #endregion

        #region Implementation

        /// <summary>
        /// If it is needed, it adds a new line or "[" while using DTD. 
        /// </summary>
        private void CheckDtdStart()
        {
            if (dtdStart) // We are first in DTD section
            {
                _writer.WriteRaw(" [");
                dtdStart = false;
            }

            if (_writer.Settings.Indent) 
            {
                _writer.WriteRaw(_writer.Settings.NewLineChars);

                if ((section & Section.DTD) == Section.DTD)
                    _writer.WriteRaw(" ");
            }
        }

        /// <summary>
        /// Ends attribute or entity or element.
        /// </summary>
        private void EndDtdElements()
        {
            endDtdAttlist();
            endDtdElement();
            endDtdEntity();
        }

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

    #region MethodsProceduralStyle

    [PhpExtension("xmlwriter")]
    public static class XMLWriterFunctions
    {
        #region Constants

        private const string DefaultXmlVersion = "1.0";

        #endregion

        internal class XMLWriterResource : PhpResource
        {
            public XMLWriter Writer { get; }

            public XMLWriterResource(XMLWriter writer) : base("xmlwriter")
            {
                Writer = writer;
            }

            protected override void FreeManaged()
            {
                Writer.Dispose();
                base.FreeManaged();
            }
        }

        private static XMLWriterResource ValidateXmlWriterResource(PhpResource context)
        {
            if (context is XMLWriterResource h && h.IsValid)
            {
                return h;
            }

            //
            PhpException.Throw(PhpError.Warning, Pchp.Library.Resources.Resources.invalid_resource, context.TypeName);
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
