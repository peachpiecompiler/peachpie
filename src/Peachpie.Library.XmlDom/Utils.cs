using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
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
            XmlDocument xml_document = xmlNode.OwnerDocument;
            if (xml_document == null) xml_document = (XmlDocument)xmlNode;

            Encoding encoding;

            if (xml_document.FirstChild is XmlDeclaration decl && !string.IsNullOrEmpty(decl.Encoding))
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
    }
}
