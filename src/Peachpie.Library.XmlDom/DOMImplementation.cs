using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Provides a number of methods for performing operations that are independent of any particular instance of the document object model.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMImplementation
    {
        #region Fields and Properties

        internal XmlImplementation XmlImplementation;

        #endregion

        #region Construction

        public DOMImplementation()
        {
            XmlImplementation = new XmlImplementation();
        }

        #endregion

        #region Operations

        /// <summary>
        /// Not implemented in PHP 7.1.1.
        /// </summary>
        public DOMNode getFeature(string feature, string version)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests if this DOM implementation implements a specific feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="version">The feature version.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public bool hasFeature(string feature, string version) => XmlImplementation.HasFeature(feature, version);

        /// <summary>
        /// Creates a new <see cref="DOMDocumentType"/>.
        /// </summary>
        /// <param name="qualifiedName">Name of the document type.</param>
        /// <param name="publicId">The public identifier of the document type.</param>
        /// <param name="systemId">The system identifier of the document type.</param>
        /// <returns>The <see cref="DOMDocumentType"/>.</returns>
        public DOMDocumentType createDocumentType(string qualifiedName = null, string publicId = null, string systemId = null)
        {
            return new DOMDocumentType(qualifiedName, publicId, systemId);
        }

        /// <summary>
        /// Creates a new <see cref="DOMDocument"/>.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI of the root element to create.</param>
        /// <param name="qualifiedName">The qualified name of the document element.</param>
        /// <param name="docType">The type of document to be created.</param>
        /// <returns>The <see cref="DOMDocument"/>.</returns>
        public DOMDocument createDocument(string namespaceUri = null, string qualifiedName = null, DOMDocumentType docType = null)
        {
            XmlDocument doc = XmlImplementation.CreateDocument();

            if (docType != null)
            {
                if (!docType.IsAssociated) docType.Associate(doc);
                else
                {
                    DOMException.Throw(ExceptionCode.WrongDocument);
                    return null;
                }
            }

            doc.AppendChild(docType.XmlNode);
            doc.AppendChild(doc.CreateElement(qualifiedName, namespaceUri));

            return new DOMDocument(doc);
        }

        #endregion
    }
}
