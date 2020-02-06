using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// DOM entity reference.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public partial class DOMEntityReference : DOMNode
    {
        #region Fields and Properties

        internal XmlEntityReference XmlEntityReference
        {
            get { return (XmlEntityReference)XmlNode; }
            set { XmlNode = value; }
        }

        private string _name;

        /// <summary>
        /// Returns the name of the entity reference.
        /// </summary>
        public override string nodeName => IsAssociated ? base.nodeName : _name;

        /// <summary>
        /// Returns <B>null</B>.
        /// </summary>
        public override string nodeValue
        {
            get { return null; }
            set { }
        }

        /// <summary>
        /// Returns <B>null</B>.
        /// </summary>
        public override string namespaceURI => null;

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.EntityReference"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.EntityReference;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DOMEntityReference()
        { }

        public DOMEntityReference(string name)
        {
            __construct(name);
        }

        internal DOMEntityReference(XmlEntityReference/*!*/ xmlEntityReference)
        {
            this.XmlEntityReference = xmlEntityReference;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMEntityReference(XmlEntityReference);
            else
            {
                DOMEntityReference copy = new DOMEntityReference();
                copy.__construct(this._name);
                return copy;
            }
        }

        public virtual void __construct(string name)
        {
            this._name = name;
        }

        #endregion

        #region Hierarchy

        internal protected override void Associate(XmlDocument/*!*/ document)
        {
            if (!IsAssociated)
            {
                XmlEntityReference = document.CreateEntityReference(_name);
            }
        }

        #endregion
    }
}
