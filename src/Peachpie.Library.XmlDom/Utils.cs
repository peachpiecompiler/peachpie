using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
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

        static void LoadHtml(this XmlNode containing, HtmlNode node)
        {
            var xmldoc = containing as XmlDocument ?? containing.OwnerDocument ?? throw new InvalidOperationException();

            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    var commentnode = (HtmlCommentNode)node;
                    if (commentnode.Comment.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase))
                    {
                        var doctype = xmldoc.CreateDocumentType("html", null, null, null);
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
                        throw new NotImplementedException();
                    }

                    name = HtmlDocument.GetXmlName(name, isAttribute: false, preserveXmlNamespaces: true);

                    // <element
                    var element = xmldoc.CreateElement(name);
                    containing.AppendChild(element);
                    
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
                        throw new NotSupportedException();
                    }
                    containing.AppendChild(xmldoc.CreateTextNode(text));
                    break;
            }
        }
    }
}
