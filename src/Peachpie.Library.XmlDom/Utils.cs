using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Linq;
using HtmlAgilityPack;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    internal static class Utils
    {
        internal static void ParseQualifiedName(string qualifiedName, out string prefix, out string localName)
        {
            if (qualifiedName == null)
            {
                prefix = null;
                localName = null;
            }
            else
            {
                int index = qualifiedName.IndexOf(':');
                if (index >= 0)
                {
                    prefix = qualifiedName.Substring(0, index);
                    localName = qualifiedName.Substring(index + 1);
                }
                else
                {
                    prefix = String.Empty;
                    localName = qualifiedName;
                }
            }
        }


        internal static Encoding/*!*/ GetNodeEncoding(Context ctx, XmlNode xmlNode)
        {
            var xml_document = xmlNode as XmlDocument ?? xmlNode.OwnerDocument;

            Encoding encoding;

            if (xml_document != null && xml_document.FirstChild is XmlDeclaration decl && !string.IsNullOrEmpty(decl.Encoding))
            {
                encoding = Encoding.GetEncoding(decl.Encoding);
            }
            else
            {
                encoding = ctx.StringEncoding;
            }

            return (encoding is UTF8Encoding)
                ? new UTF8Encoding(false)   // no BOM for UTF-8 please!
                : encoding;
        }

        /// <summary>
        /// Loads HTML document into XML document.
        /// </summary>
        /// <param name="xmldoc"></param>
        /// <param name="htmldoc"></param>
        internal static void LoadHtml(this XmlDocument xmldoc, HtmlDocument htmldoc)
        {
            xmldoc.RemoveAll();

            LoadHtml(xmldoc, htmldoc.DocumentNode);
        }

        /// <summary>
        /// Finds first node of given type.
        /// </summary>
        static bool TryGetNodeOfType(this XmlNodeList nodes, XmlNodeType nt, out XmlNode node)
        {
            if (nodes != null && nodes.Count != 0)
            {
                foreach (XmlNode child in nodes)
                {
                    if (child.NodeType == nt)
                    {
                        node = child;
                        return true;
                    }
                }
            }

            node = null;
            return false;
        }

        static void LoadHtml(this XmlNode containing, HtmlNode node)
        {
            var xmldoc = containing as XmlDocument ?? containing.OwnerDocument ?? throw new InvalidOperationException();

            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    var commentnode = (HtmlCommentNode)node;
                    if (DocumentTypeUtils.TryParseDocumentType(commentnode.Comment, out var doctypeName, out var publicId, out var systemId, out var subset))
                    {
                        var doctype = xmldoc.CreateDocumentType(doctypeName, publicId, systemId, subset); // NOTE: causes DTD validation
                        containing.AppendChild(doctype);
                    }
                    else
                    {
                        containing.AppendChild(xmldoc.CreateComment(commentnode.Comment));
                    }
                    break;

                case HtmlNodeType.Document:
                    foreach (var child in node.ChildNodes)
                    {
                        LoadHtml(containing, child);
                    }
                    break;

                case HtmlNodeType.Element:

                    var name = node.Name;   // NOTE: Name is lowercased, needed for XPath queries

                    if (name.Length == 0 || name[0] == '?' || name[0] == '!')
                    {
                        // <?xml >

                        // ??

                        break;
                    }

                    name = HtmlDocument.GetXmlName(name, isAttribute: false, preserveXmlNamespaces: true);

                    // <element
                    var element = xmldoc.CreateElement(name);

                    if (containing.ParentNode == null && TryGetNodeOfType(containing.ChildNodes, XmlNodeType.Element, out var existingroot)) // root can only have one child element
                    {
                        // !!!
                        // HACK: nest the element into the existing root
                        existingroot.AppendChild(element);
                    }
                    else
                    {
                        containing.AppendChild(element);
                    }

                    // attributes
                    foreach (var attr in node.Attributes)
                    {
                        var element_attr = xmldoc.CreateAttribute(attr.OriginalName);
                        element_attr.Value = attr.Value;

                        element.Attributes.Append(element_attr);
                    }

                    // nested children
                    foreach (var child in node.ChildNodes)
                    {
                        LoadHtml(element, child);
                    }

                    // </element>

                    break;

                case HtmlNodeType.Text:
                    var text = ((HtmlTextNode)node).Text;
                    if (containing.ParentNode == null) // root
                    {
                        if (string.IsNullOrWhiteSpace(text)) break;
                        throw new NotSupportedException();  // TODO: we can wrap the Text element into something and append
                    }
                    containing.AppendChild(xmldoc.CreateTextNode(text));
                    break;
            }
        }
    }

    static class DocumentTypeUtils
    {
        /// <summary>
        /// Simple parser getting words subsequently.
        /// </summary>
        static bool ConsumeToken(string text, ref int pos, out ReadOnlySpan<char> token)
        {
            Debug.Assert(text != null);
            Debug.Assert(pos >= 0);

            // skip whitespaces
            while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }

            if (pos >= text.Length)
            {
                token = ReadOnlySpan<char>.Empty;
                return false;
            }

            // word:
            int start = pos;
            if (text[pos] == '\"')
            {
                pos++;

                // word enclosed in quotes:
                while (pos < text.Length)
                {
                    var ch = text[pos++];
                    if (ch == '\"')
                    {
                        break;
                    }
                }

                token = text.AsSpan(start + 1, pos - start - 2);
            }
            else
            {
                for (; pos < text.Length; pos++)
                {
                    var ch = text[pos];
                    if (char.IsWhiteSpace(ch) || ch == '>' || ch == '\"')
                    {
                        break;
                    }
                }

                token = text.AsSpan(start, pos - start);
            }

            //
            return token.Length != 0; // token found and valid (not '>')
        }

        /// <summary>
        /// Parses the DOCTYPE comment if possible.
        /// </summary>
        public static bool TryParseDocumentType(string text, out string name, out string publicId, out string systemId, out string subset)
        {
            name = publicId = systemId = subset = null;

            const string doctypeprefix = "<!doctype";
            if (text != null && text.StartsWith(doctypeprefix, StringComparison.OrdinalIgnoreCase))
            {
                name = "html"; // default

                int pos = doctypeprefix.Length;
                int index = 0; // token index

                // <!DOCTYPE {NAME} PUBLIC|SYSTEM {ID1} {ID2}>
                string pub = null;

                while (ConsumeToken(text, ref pos, out var token))
                {
                    Debug.Assert(token.Length != 0);

                    switch (index++)
                    {
                        case 0:
                            name = token.ToString();
                            break;

                        case 1:
                            pub = token.ToString();
                            break;

                        case 2:
                            if (string.Equals(pub, "PUBLIC", StringComparison.OrdinalIgnoreCase))
                            {
                                publicId = token.ToString();
                            }
                            else if (string.Equals(pub, "SYSTEM", StringComparison.OrdinalIgnoreCase))
                            {
                                systemId = token.ToString();
                            }
                            else
                            {
                                goto default;
                            }
                            break;

                        case 3:
                            if (publicId == null)
                            {
                                goto default;
                            }

                            systemId = token.ToString();
                            break;

                        default:
                            Debug.WriteLine("Unexpected DOCTYPE value: " + text);
                            return true; // done
                    }

                }

                return true;
            }

            //
            return false;
        }
    }
}
