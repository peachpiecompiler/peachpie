using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

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

        internal static Encoding/*!*/ GetNodeEncoding(XmlNode xmlNode)
        {
            XmlDocument xml_document = xmlNode.OwnerDocument;
            if (xml_document == null) xml_document = (XmlDocument)xmlNode;

            Encoding encoding;

            XmlDeclaration decl = xml_document.FirstChild as XmlDeclaration;
            if (decl != null && !String.IsNullOrEmpty(decl.Encoding))
            {
                encoding = Encoding.GetEncoding(decl.Encoding);
            }
            else
            {
                encoding = Encoding.UTF8;

                // TODO: Replace by this when configuration is enabled in .NET Core
                //       (netstandard2.0, package System.Configuration.ConfigurationManager)
                //encoding = Configuration.Application.Globalization.PageEncoding;
            }

            // no BOM for UTF-8 please!
            if (encoding is UTF8Encoding) return new UTF8Encoding(false);
            else return encoding;
        }
    }
}
