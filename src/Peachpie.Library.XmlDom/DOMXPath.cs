using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Pchp.Core;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Supports XPath 1.0.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("dom")]
    public class DOMXPath
    {
        #region Fields and Properties

        /// <summary>
        /// Object used to evaluate XPath queries.
        /// </summary>
        internal XPathNavigator XPathNavigator;

        /// <summary>
        /// Namespace manager containing all the namespaces defined in the document.
        /// </summary>
        internal XmlNamespaceManager NamespaceManagerFull;

        /// <summary>
        /// Namespace manager containing only explicitly added namespaces.
        /// </summary>
        internal XmlNamespaceManager NamespaceManagerExplicit;

        internal protected XmlDocument OriginalXmlDocument
        {
            get
            {
                if (XPathNavigator.UnderlyingObject is XmlDocument xmldoc)
                {
                    return xmldoc;
                }
                else if (XPathNavigator.UnderlyingObject is XmlNode xmlnode)
                {
                    return xmlnode.OwnerDocument;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Returns the <see cref="DOMDocument"/> associated with this object.
        /// </summary>
        public DOMDocument document => new DOMDocument(OriginalXmlDocument);

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DOMXPath()
        { }

        public DOMXPath(DOMDocument document)
        {
            __construct(document);
        }

        internal DOMXPath(XPathNavigator/*!*/ navigator)
        {
            this.XPathNavigator = navigator;
            InitNamespaceManagers(false);
        }

        private void InitNamespaceManagers(bool isHtmlDocument)
        {
            this.NamespaceManagerFull = new XmlNamespaceManager(XPathNavigator.NameTable);
            this.NamespaceManagerExplicit = new XmlNamespaceManager(XPathNavigator.NameTable);

            if (!isHtmlDocument)
            {
                XPathNodeIterator iterator = XPathNavigator.Select("//namespace::*[not(. = ../../namespace::*)]");

                while (iterator.MoveNext())
                {
                    NamespaceManagerFull.AddNamespace(iterator.Current.Name, iterator.Current.Value);
                }
            }
        }

        public void __construct(DOMDocument document)
        {
            var xmldoc = document.XmlDocument;
            if (xmldoc == null)
            {
                // invalid document provided
                throw new ArgumentException(nameof(document));
            }

            var element = xmldoc.DocumentElement;

            this.XPathNavigator = (element != null)
                ? element.CreateNavigator() // regular XPathNavigator from the root
                : xmldoc.CreateNavigator(); // empty xml document!

            InitNamespaceManagers(document._isHtmlDocument);
        }

        #endregion

        #region XPath

        /// <summary>
        /// Registeres the given namespace with the collection of known namespaces.
        /// </summary>
        /// <param name="prefix">The prefix to associate with the namespace being registered.</param>
        /// <param name="uri">The namespace to register.</param>
        /// <returns><B>True</B>.</returns>
        public bool registerNamespace(string prefix, string uri)
        {
            NamespaceManagerFull.AddNamespace(prefix, uri);
            NamespaceManagerExplicit.AddNamespace(prefix, uri);
            return true;
        }

        /// <summary>
        /// Evaluates the given XPath expression.
        /// </summary>
        /// <param name="expr">The expression to evaluate.</param>
        /// <param name="contextnode">The context node for doing relative XPath queries. By default, the queries are
        /// relative to the root element.</param>
        /// <param name="registerNodeNS">Can be specified to disable automatic registration of the context node namespace.</param>
        /// <returns>The <see cref="DOMNodeList"/> containg the result or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public DOMNodeList query(string expr, DOMNode contextnode = null, bool registerNodeNS = true)
        {
            XPathNavigator navigator = GetNavigator(contextnode);
            if (navigator == null) return null;

            var nsManager = registerNodeNS ? NamespaceManagerFull : NamespaceManagerExplicit;

            XPathNodeIterator iterator;
            try
            {
                iterator = navigator.Select(expr, nsManager);
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.E_WARNING, ex.Message);
                return null;
            }

            // create the resulting node list
            return IteratorToList(iterator);
        }

        /// <summary>
        /// Evaluates the given XPath expression and returns a typed result if possible.
        /// </summary>
        /// <param name="expr">The expression to evaluate.</param>
        /// <param name="contextnode">The context node for doing relative XPath queries. By default, the queries are
        /// relative to the root element.</param>
        /// <param name="registerNodeNS">Can be specified to disable automatic registration of the context node namespace.</param>
        /// <returns>A typed result if possible or a <see cref="DOMNodeList"/> containing all nodes matching the
        /// given <paramref name="expr"/>.</returns>
        public PhpValue evaluate(string expr, DOMNode contextnode = null, bool registerNodeNS = true)
        {
            XPathNavigator navigator = GetNavigator(contextnode);
            if (navigator == null) return PhpValue.Create(false);

            var nsManager = registerNodeNS ? NamespaceManagerFull : NamespaceManagerExplicit;

            object result;
            try
            {
                result = navigator.Evaluate(expr, nsManager);
            }
            catch (Exception ex)
            {
                DOMException.Throw(ExceptionCode.SyntaxError, ex.Message);
                return PhpValue.Create(false);
            }

            // the result can be bool, double, string, or iterator
            XPathNodeIterator iterator = result as XPathNodeIterator;
            if (iterator != null)
            {
                return PhpValue.FromClass(IteratorToList(iterator));
            }
            else
            {
                return PhpValue.FromClr(result);
            }
        }

        /// <summary>
        /// Register PHP functions as XPath functions.
        /// </summary>
        /// <param name="restrict">Use this parameter to only allow certain functions to be called from XPath. 
        /// This parameter can be either a string (a function name) or an array of function names.</param>
        public void registerPhpFunctions(PhpArray restrict)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register PHP functions as XPath functions.
        /// </summary>
        /// <param name="restrict">Use this parameter to only allow certain functions to be called from XPath. 
        /// This parameter can be either a string (a function name) or an array of function names.</param>
        public void registerPhpFunctions(string restrict)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register PHP functions as XPath functions.
        /// </summary>
        public void registerPhpFunctions()
        {
            throw new NotImplementedException();
        }

        private XPathNavigator GetNavigator(DOMNode context)
        {
            if (context == null) return XPathNavigator;
            else
            {
                XmlNode node = context.XmlNode;

                if (node.OwnerDocument != OriginalXmlDocument)
                {
                    DOMException.Throw(ExceptionCode.WrongDocument);
                    return null;
                }

                return node.CreateNavigator();
            }
        }

        private DOMNodeList IteratorToList(XPathNodeIterator iterator)
        {
            DOMNodeList list = new DOMNodeList();

            while (iterator.MoveNext())
            {
                IHasXmlNode has_node = iterator.Current as IHasXmlNode;
                if (has_node != null)
                {
                    var node = DOMNode.Create(has_node.GetNode());
                    if (node != null) list.AppendNode(node);
                }
            }

            return list;
        }

        #endregion
    }
}
