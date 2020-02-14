using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// DOM notation.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public partial class DOMNotation : DOMNode
    {
        #region Fields and Properties

        internal XmlNotation XmlNotation
        {
            get { return (XmlNotation)XmlNode; }
            set { XmlNode = value; }
        }

        /// <summary>
        /// Returns the value of the public identifier on the notation declaration.
        /// </summary>
        public string publicId => XmlNotation.PublicId;

        /// <summary>
        /// Returns the value of the system identifier on the notation declaration.
        /// </summary>
        public string systemId => XmlNotation.SystemId;

        /// <summary>
        /// Returns the name of the notation node.
        /// </summary>
        public override string nodeName => XmlNotation.Name;

        /// <summary>
        /// Returns or sets the value of the notation node.
        /// </summary>
        public override string nodeValue
        {
            get { return XmlNotation.Value; }
            set { XmlNotation.Value = value; }
        }

        /// <summary>
        /// Returns the attributes of this notation node.
        /// </summary>
        public override DOMNamedNodeMap attributes
        {
            get
            {
                DOMNamedNodeMap map = new DOMNamedNodeMap();

                foreach (XmlAttribute attr in XmlNotation.Attributes)
                {
                    var node = DOMNode.Create(attr);
                    if (node != null) map.AddNode(node);
                }

                return map;
            }
        }

        #endregion

        #region Construction

        public DOMNotation()
        { }

        internal DOMNotation(XmlNotation/*!*/ xmlNotation)
        {
            this.XmlNotation = xmlNotation;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMNotation(XmlNotation);
            else return new DOMNotation();
        }

        #endregion
    }
}
