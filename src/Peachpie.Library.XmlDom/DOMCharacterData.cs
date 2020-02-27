using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Represents nodes with character data. No nodes directly correspond to this class, but other nodes do inherit from it.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public partial class DOMCharacterData : DOMNode
    {
        #region Fields and Properties

        internal XmlCharacterData XmlCharacterData
        {
            get { return (XmlCharacterData)XmlNode; }
            set { XmlNode = value; }
        }

        internal virtual string dataImpl
        {
            get { return XmlCharacterData.Data; }
            set { XmlCharacterData.Data = value; }
        }

        internal virtual int dataLengthImpl => XmlCharacterData.Length;

        /// <summary>
        /// Returns or sets the data of the node.
        /// </summary>
        public string data
        {
            get { return this.dataImpl; }
            set { this.dataImpl = value; }
        }

        /// <summary>
        /// Returns the length of the data in characters.
        /// </summary>
        public int length => this.dataLengthImpl;

        #endregion

        #region Construction

        public DOMCharacterData()
        { }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            return new DOMCharacterData();
        }

        #endregion

        #region String operations

        /// <summary>
        /// Retrieves a substring of the full string from the specified range.
        /// </summary>
        /// <param name="offset">The position within the string to start retrieving.</param>
        /// <param name="count">The number of characters to retrieve.</param>
        /// <returns>The substring corresponding to the specified range or <B>false</B>.</returns>
        [return: CastToFalse]
        public virtual string substringData(int offset, int count)
        {
            if (offset < 0 || count < 0 || offset > XmlCharacterData.Length)
            {
                DOMException.Throw(ExceptionCode.IndexOutOfBounds);
                return null;
            }

            return XmlCharacterData.Substring(offset, count);
        }

        /// <summary>
        /// Appends the specified string to the end of the character data of the node.
        /// </summary>
        /// <param name="arg">The string to insert into the existing string.</param>
        public virtual void appendData(string arg) => XmlCharacterData.AppendData(arg);

        /// <summary>
        /// Inserts the specified string at the specified character offset. 
        /// </summary>
        /// <param name="offset">The position within the string to insert the supplied string data.</param>
        /// <param name="arg">The string data that is to be inserted into the existing string.</param>
        public virtual void insertData(int offset, string arg)
        {
            if (offset < 0 || offset > XmlCharacterData.Length)
            {
                DOMException.Throw(ExceptionCode.IndexOutOfBounds);
            }

            XmlCharacterData.InsertData(offset, arg);
        }

        /// <summary>
        /// Removes a range of characters from the node.
        /// </summary>
        /// <param name="offset">The position within the string to start deleting.</param>
        /// <param name="count">The number of characters to delete.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual void deleteData(int offset, int count)
        {
            if (offset < 0 || count < 0 || offset > XmlCharacterData.Length)
            {
                DOMException.Throw(ExceptionCode.IndexOutOfBounds);
            }

            XmlCharacterData.DeleteData(offset, count);
        }

        /// <summary>
        /// Replaces the specified number of characters starting at the specified offset with the specified string.
        /// </summary>
        /// <param name="offset">The position within the string to start replacing.</param>
        /// <param name="count">The number of characters to replace.</param>
        /// <param name="arg">The new data that replaces the old string data.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public virtual void replaceData(int offset, int count, string arg)
        {
            if (offset < 0 || count < 0 || offset > length)
            {
                DOMException.Throw(ExceptionCode.IndexOutOfBounds);
            }

            XmlCharacterData.ReplaceData(offset, count, arg);
        }

        #endregion
    }

    /// <summary>
    /// Inherits from <see cref="DOMCharacterData"/> and represents the textual content of a
    /// <see cref="DOMElement"/> or <see cref="DOMAttr"/>. 
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public partial class DOMText : DOMCharacterData
    {
        #region Fields and Properties

        private string _value;

        internal override string dataImpl
        {
            get
            {
                return IsAssociated ? base.dataImpl : _value;
            }
            set
            {
                if (IsAssociated)
                    base.dataImpl = value;

                this._value = value;
            }
        }

        internal override int dataLengthImpl => IsAssociated ? base.dataLengthImpl : (this._value != null ? this._value.Length : 0);

        /// <summary>
        /// Returns &quot;#text&quot;.
        /// </summary>
        public override string nodeName => "#text";

        /// <summary>
        /// Returns or sets the text.
        /// </summary>
        public override string nodeValue
        {
            get
            {
                return this.dataImpl;
            }
            set
            {
                this._value = value;
                if (IsAssociated) base.nodeValue = this._value;
            }
        }

        /// <summary>
        /// Returns <B>null</B>.
        /// </summary>
        public override string namespaceURI => null;

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.Text"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.Text;

        /// <summary>
        /// Gets the concatenated values of the node and all its child nodes.
        /// </summary>
        public string wholeText => this.dataImpl;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DOMText()
        { }

        public DOMText(string value = null)
        {
            __construct(value);
        }

        /// <summary>
        /// This constructor can be used either with proper <see cref="XmlText"/> or with <see cref="XmlWhitespace"/>
        /// or <see cref="XmlSignificantWhitespace"/>. PHP uses <see cref="DOMText"/> for all these cases.
        /// </summary>
        internal DOMText(XmlCharacterData/*!*/ xmlCharacterData)
        {
            this.XmlCharacterData = xmlCharacterData;
        }

        internal static DOMText CreateDOMText(string value)
        {
            DOMText copy = new DOMText();
            copy.__construct(value);
            return copy;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMText(XmlCharacterData);
            else
            {
                return CreateDOMText(_value);
            }
        }

        public virtual void __construct(string value = null)
        {
            this._value = value ?? string.Empty;
        }

        #endregion

        #region Hierarchy

        internal protected override void Associate(XmlDocument/*!*/ document)
        {
            if (!IsAssociated)
            {
                XmlCharacterData = document.CreateTextNode(_value);
            }
        }

        #endregion

        #region String operations

        /// <summary>
        /// Splits the node into two nodes at the specified offset, keeping both in the tree as siblings.
        /// </summary>
        /// <param name="offset">The offset at which to split the node.</param>
        /// <returns>The new node or <b>false</b> if <paramref name="offset"/> is invalid.</returns>
        [return: CastToFalse]
        public DOMText splitText(int offset)
        {
            if (offset < 0 || offset > this.dataLengthImpl) return null;

            if (!IsAssociated)
            {
                return CreateDOMText(dataImpl.Substring(offset));
            }
            else if (XmlCharacterData is XmlText xmlText)
            {
                return (DOMText)Create(xmlText.SplitText(offset));
            }
            else
            {
                // In case of XmlWhitespace and XmlSignificantWhitespace
                int count = this.dataLengthImpl - offset;
                string splitData = XmlCharacterData.Substring(offset, count);
                XmlCharacterData.DeleteData(offset, count);
                XmlText newTextNode = XmlCharacterData.OwnerDocument.CreateTextNode(splitData);
                XmlCharacterData.ParentNode.InsertAfter(newTextNode, XmlCharacterData);
                return (DOMText)Create(newTextNode);
            }
        }

        /// <summary>
        /// Determines whether this text node is empty / whitespace only.
        /// </summary>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public bool isWhitespaceInElementContent()
        {
            string text = nodeValue as string;
            if (text == null) return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (!Char.IsWhiteSpace(text, i)) return false;
            }
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Inherits from <see cref="DOMText"/> for textural representation of CData constructs. 
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public partial class DOMCdataSection : DOMText
    {
        #region Fields and Properties

        internal XmlCDataSection XmlCDataSection
        {
            get { return (XmlCDataSection)XmlNode; }
            set { XmlNode = value; }
        }

        /// <summary>
        /// Returns &quot;#cdata-section&quot;.
        /// </summary>
        public override string nodeName => "#cdata-section";

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.CharacterDataSection"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.CharacterDataSection;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DOMCdataSection()
        { }

        public DOMCdataSection(string value)
        {
            __construct(value);
        }

        internal DOMCdataSection(XmlCDataSection/*!*/ xmlCDataSection)
        {
            this.XmlCDataSection = xmlCDataSection;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMCdataSection(XmlCDataSection);
            else
            {
                DOMCdataSection copy = new DOMCdataSection();
                copy.__construct(this.dataImpl);
                return copy;
            }
        }

        public override void __construct(string value)
        {
            base.__construct(value);
        }

        #endregion

        #region Hierarchy

        internal protected override void Associate(XmlDocument/*!*/ document)
        {
            if (!IsAssociated)
            {
                XmlCDataSection = document.CreateCDataSection(dataImpl);
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents comment nodes, characters delimited by <!-- and -->.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public partial class DOMComment : DOMCharacterData
    {
        #region Fields and Properties

        protected internal XmlComment XmlComment
        {
            get { return (XmlComment)XmlNode; }
            set { XmlNode = value; }
        }

        private string _value;

        /// <summary>
        /// Returns &quot;#comment&quot;.
        /// </summary>
        public override string nodeName => "#comment";

        /// <summary>
        /// Returns or sets the text.
        /// </summary>
        public override string nodeValue
        {
            get
            {
                return (IsAssociated ? base.nodeValue : _value);
            }
            set
            {
                this._value = value;
                if (IsAssociated) base.nodeValue = this._value;
            }
        }

        /// <summary>
        /// Returns <B>null</B>.
        /// </summary>
        public override string namespaceURI => null;

        /// <summary>
        /// Returns the type of the node (<see cref="NodeType.Comment"/>).
        /// </summary>
        public override int nodeType => (int)NodeType.Comment;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DOMComment()
        { }

        public DOMComment(string value = null)
        {
            __construct(value);
        }

        internal DOMComment(XmlComment/*!*/ xmlComment)
        {
            this.XmlComment = xmlComment;
        }

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
        {
            if (IsAssociated) return new DOMComment(XmlComment);
            else
            {
                DOMComment copy = new DOMComment();
                copy.__construct(this._value);
                return copy;
            }
        }

        public void __construct(string value = null)
        {
            this._value = value;
        }

        #endregion

        #region Hierarchy

        internal protected override void Associate(XmlDocument/*!*/ document)
        {
            if (!IsAssociated)
            {
                XmlComment = document.CreateComment(_value);
            }
        }

        #endregion
    }
}
