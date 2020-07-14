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

        // Flags for CData, Comment and PISection, DTD
        private int countOfNastedNodes = 0;
        private Section section = Section.Default;
        private bool dtdStart = false;

        [Flags]
        private enum Section { Default = 0, Comment = 1, PI = 2, CDATA = 4, DTD = 8, DtdElement = 16, DtdEntity = 32, DtdAttribute = 64 }

        static XmlWriterSettings DefaultSettings = new XmlWriterSettings()
        {
            Encoding = new UTF8Encoding(false),     // Disable BOM
            Indent = true,
            NewLineChars = "\n",
            WriteEndDocumentOnClose = true,
            ConformanceLevel = ConformanceLevel.Auto
        };

        // XML writer does not escape "
        private static Dictionary<char, string> escapedChars = new Dictionary<char, string>()
        {
            { '"' , "&quot;" },
            { '<' , "&lt;" },
            { '>' , "&gt;" },
            { '&',"&amp;"}
        };

        #endregion

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
            var result = CheckedCall(() => _writer.WriteRaw(dtdStart ? ">" : "]>"));
            dtdStart = false;
            return result;
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

        public bool setIndentString(string indentString)
        {
            if (_writer.WriteState != WriteState.Start)
                return false;

            var settings = new XmlWriterSettings()
            {
                Encoding = new UTF8Encoding(false),     // Disable BOM
                Indent = _writer.Settings.Indent,
                IndentChars = indentString,
                NewLineChars = "\n",
                WriteEndDocumentOnClose = true,
                ConformanceLevel = ConformanceLevel.Auto
            };

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

            var settings = new XmlWriterSettings()
            {
                Encoding = new UTF8Encoding(false),     // Disable BOM
                Indent = indent,
                IndentChars = _writer.Settings.IndentChars,
                NewLineChars = "\n",
                WriteEndDocumentOnClose = true,
                ConformanceLevel = ConformanceLevel.Auto
            };

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
                return false;

            if (section == Section.PI)
                section = section | Section.CDATA;

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

            dtdStart = true;

            if (string.IsNullOrEmpty(standalone))
            {
                return CheckedCall(() => _writer.WriteStartDocument());
            }
            else
            {
                return CheckedCall(() => _writer.WriteStartDocument(standalone == "yes"));
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
            // Nested DTDs are prohibited
            if (section != Section.Default)
                return false;

            if (_writer.Settings.ConformanceLevel == ConformanceLevel.Document && _writer.WriteState != WriteState.Prolog && _writer.WriteState != WriteState.Start)
            {
                // TODO: Warning
                return false;
            }

            if (String.IsNullOrEmpty(systemId) && !String.IsNullOrEmpty(publicId))
            {
                // TODO: Warning
                return false;
            }

            // Makes doctype
            string doctype = $"<!DOCTYPE {qualifiedName}";
            if (!String.IsNullOrEmpty(publicId))
                doctype += $" PUBLIC \"{publicId}\"";
            if (!String.IsNullOrEmpty(systemId))
                doctype += $" SYSTEM \"{systemId}\"";

            // Makes new line, if it is first decleration after prolog.
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
                string.Format("content");

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

            return CheckedCall(() => _writer.WriteAttributeString(prefix, name, uri, content));
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

            return CheckedCall(() => _writer.WriteAttributeString(name, content));
        }

        public bool writeCdata(string content)
        {
            if (section != Section.Default && section != Section.PI)
                return false;

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
            // Nested DTDs are prohibited
            if (section != Section.Default)
                return false;

            if (_writer.Settings.ConformanceLevel == ConformanceLevel.Document && _writer.WriteState != WriteState.Prolog && _writer.WriteState != WriteState.Start)
            {
                // TODO: Warning
                return false;
            }

            if (String.IsNullOrEmpty(systemId) && !String.IsNullOrEmpty(publicId))
            {
                // TODO: Warning
                return false;
            }

            // Makes doctype
            string doctype = $"<!DOCTYPE {name}";
            if (!String.IsNullOrEmpty(publicId))
                doctype += $" PUBLIC \"{publicId}\"";
            if (!String.IsNullOrEmpty(systemId))
                doctype += $" \"{systemId}\"";
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

            return CheckedCall(() => _writer.WriteElementString(prefix, name, uri, content));
        }

        public bool writeElement(string name, string content = null)
        {
            if ((section & Section.PI) == Section.PI)
                return false;

            return CheckedCall(() => _writer.WriteElementString(name, content));
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
            if (dtdStart)
            {
                if ((section & Section.DTD) == Section.DTD)
                    _writer.WriteRaw(" [");
                else
                    _writer.WriteRaw(_writer.Settings.NewLineChars);
                dtdStart = false;
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
    public static class StringExtensions
    {
        public static string Escape(this string source, Dictionary<char, string> characters)
        {
            if (String.IsNullOrEmpty(source))
                return "";

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < source.Length; i++)
            {
                if (characters.TryGetValue(source[i], out string replacement))
                    sb.Append(replacement);
                else
                    sb.Append(source[i]);
            }

            return sb.ToString();
        }
    }
}
