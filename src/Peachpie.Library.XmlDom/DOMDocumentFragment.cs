using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMDocumentFragment : DOMNode
    {
        #region Fields and Properties

        internal XmlDocumentFragment XmlDocumentFragment
        {
            get { return (XmlDocumentFragment)XmlNode; }
            set { XmlNode = value; }
        }

        /// <summary>
        /// Returns &quot;#document-fragment&quot;.
        /// </summary>
        public override string nodeName => "#document-fragment";

        /// <summary>
        /// Returns <B>null</B>.
        /// </summary>
        public override string nodeValue
        {
            get { return null; }
            set { }
        }

        /// <summary>
        /// Returns the namespace URI of the node.
        /// </summary>
        public override string namespaceURI => (IsAssociated ? base.namespaceURI : null);

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.DocumentFragment"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.DocumentFragment;

        #endregion

        #region Construction

        public DOMDocumentFragment()
        { }

        internal DOMDocumentFragment(XmlDocumentFragment/*!*/ xmlDocumentFragment)
        {
            this.XmlDocumentFragment = xmlDocumentFragment;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            return new DOMDocumentFragment(XmlDocumentFragment);
        }

        #endregion

        #region Hierarchy

        internal protected override void Associate(XmlDocument document)
        {
            if (!IsAssociated)
            {
                XmlDocumentFragment = document.CreateDocumentFragment();
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Appends (well-formed) XML data to this document fragment.
        /// </summary>
        /// <param name="data">The data to append.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public bool appendXML(string data)
        {
            try
            {
                XmlDocumentFragment.InnerXml += data;
            }
            catch (XmlException)
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}
