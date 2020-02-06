using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMProcessingInstruction : DOMNode
    {
		#region Fields and Properties

		protected internal XmlProcessingInstruction XmlProcessingInstruction
		{
			get { return (XmlProcessingInstruction)XmlNode; }
			set { XmlNode = value; }
		}

		private string _name;
		private string _value;

		/// <summary>
		/// Returns the name of the processing instruction.
		/// </summary>
		public override string nodeName => (IsAssociated ? base.nodeName : _name);

		/// <summary>
		/// Returns or sets the value of the processing instruction.
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
		/// Returns the namespace URI of the processing instruction.
		/// </summary>
		public override string namespaceURI => (IsAssociated ? base.namespaceURI : null);

		/// <summary>
		/// Returns the type of the node (<see cref="NodeType.ProcessingInstruction"/>).
		/// </summary>
		public override int nodeType => (int)NodeType.ProcessingInstruction;

		/// <summary>
		/// Returns the target (name) of the processing instruction.
		/// </summary>
		public string target => this.nodeName;

		/// <summary>
		/// Returns or sets the data (value) of the processing instruction.
		/// </summary>
		public string data
		{
			get { return this.nodeValue; }
			set { this.nodeValue = value; }
		}

		#endregion

		#region Construction

        [PhpFieldsOnlyCtor]
		protected DOMProcessingInstruction()
		{ }

		public DOMProcessingInstruction(string name, string value = null)
		{
            __construct(name, value);
        }

		internal DOMProcessingInstruction(XmlProcessingInstruction/*!*/ xmlProcessingInstruction)
		{
			this.XmlProcessingInstruction = xmlProcessingInstruction;
		}

        private protected override DOMNode CloneObjectInternal(bool deepCopyFields)
		{
			if (IsAssociated) return new DOMProcessingInstruction(XmlProcessingInstruction);
			else
			{
				DOMProcessingInstruction copy = new DOMProcessingInstruction();
				copy.__construct(this._name, this._value);
				return copy;
			}
		}

		public void __construct(string name, string value = null)
		{
			this._name = name;
			this._value = value;
		}

		#endregion
    }
}
