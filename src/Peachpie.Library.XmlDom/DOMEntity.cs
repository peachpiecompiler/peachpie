using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// This class represents a known entity, either parsed or unparsed, in an XML document.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public partial class DOMEntity : DOMNode
    {
        #region Fields and Properties

        internal XmlEntity XmlEntity
        {
            get { return (XmlEntity)XmlNode; }
            set { XmlNode = value; }
        }

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.Entity"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.Entity;

        /// <summary>
        /// Returns the public identifier of this entity.
        /// </summary>
        public string publicId => XmlEntity.PublicId;

        /// <summary>
        /// Returns the system identifier of this entity.
        /// </summary>
        public string systemId => XmlEntity.SystemId;

        /// <summary>
        /// Returns the name of the optional NDATA attribute.
        /// </summary>
        public string notationName => XmlEntity.NotationName;

        /// <summary>
        /// Always returns <B>null</B> as in PHP 5.1.6.
        /// </summary>
        public string actualEncoding
        {
            get { return null; }
            set { }
        }

        /// <summary>
        /// Always returns <B>null</B> as in PHP 5.1.6.
        /// </summary>
        public string encoding => null;

        /// <summary>
        /// Always returns <B>null</B> as in PHP 5.1.6.
        /// </summary>
        public string version
        {
            get { return null; }
            set { }
        }

        #endregion

        #region Construction

        public DOMEntity()
        { }

        internal DOMEntity(XmlEntity/*!*/ xmlEntity)
        {
            this.XmlEntity = xmlEntity;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMEntity(XmlEntity);
            else return new DOMEntity();
        }

        #endregion
    }
}
