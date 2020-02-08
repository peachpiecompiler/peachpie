using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Pchp.Core;
using Mvp.Xml;
using Mvp.Xml.XPointer;
using Mvp.Xml.Common;
using Pchp.CodeAnalysis;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Class implements Xinclude in namespace http://www.w3.org/2001/XInclude or http://www.w3.org/2003/XInclude, where recursively solves include tags. There is support for fallback, cycle recursion, base uri and lang fix. Most of things for xpointer are supported too.
    /// </summary>
    class XIncludeHelper
    {
        #region Tags, atributes, namespaces

        // Namespaces
        const string nsOfXIncludeNew = "http://www.w3.org/2001/XInclude";
        const string nsOfXIncludeOld = "http://www.w3.org/2003/XInclude";
        const string nsOfXml = "http://www.w3.org/XML/1998/namespace";

        // Elements in namespace http://www.w3.org/2001/XInclude
        const string etInclude = "include"; // prefix + :include
        const string etFallback = "fallback";

        // Atributes
        const string atHref = "href";
        const string atParse = "parse";
        const string atXPointer = "xpointer";
        const string atBase = "base";
        const string atLang = "lang";
        const string atEncoding = "encoding";

        #endregion

        /// <summary>
        /// When IncludeXml detects tag include, pushes curent document to documents stack and resolves next documents,
        /// after that, pulls previous document in other to includes probing document.
        /// </summary>
        Stack<XmlDocument> _documents;

        /// <summary>
        /// Keeps route of calling documents and controls loop.
        /// </summary>
        Dictionary<string, string> _references;

        /// <summary>
        /// Counts include replaces.
        /// <c>-1</c> for failure.
        /// </summary>
        int _replaceCount = 0; // 

        readonly Context _ctx;

        public XIncludeHelper(Context ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// If resolvingUri is relative, combines it with absoluteUriOfDirectory.
        /// </summary>
        /// <returns>Returns absolute uri</returns>
        static public string UriResolver(string resolvingUri, string absoluteUriOfDirectory)
        {
            Uri result;
            if (!Uri.TryCreate(resolvingUri, UriKind.Absolute, out result)) // try, if resolvingUri is not absolute path
            {
                result = new Uri(new Uri("file://"), Path.Combine(absoluteUriOfDirectory, resolvingUri));
            }

            return result.ToString();
        }

        /// <summary>
        /// Finds all tags Xinclude and treats everyone recursively.
        /// </summary>
        public PhpValue Include(XmlDocument document)
        {
            _replaceCount = 0;
            _documents = new Stack<XmlDocument>();
            _references = new Dictionary<string, string>();
            IncludeXml(document.BaseURI, null, document, "");
            if (_replaceCount == 0)
                return PhpValue.False;
            else
                return PhpValue.Create(_replaceCount);
        }

        /// <summary>
        /// Finds all tags Xinclude and treats everyone recursively.
        /// </summary>
        /// <param name="absoluteUri">Uri of treated xml document.</param>
        /// <param name="includeNode">Node, which references to document, which Uri is absoluteUri</param>
        /// <param name="MasterDocument">Document, where Xinclude start</param>
        /// <param name="xpointer">xpointer value, or null</param>
        void IncludeXml(string absoluteUri, XmlElement includeNode, XmlDocument MasterDocument, string xpointer)
        {
            XmlDocument document;
            bool checkingError = false;

            if (MasterDocument == null)
            {
                document = PhpXmlDocument.Create();

                document.Load(xpointer == null
                    ? (XmlReader)new XmlBaseAwareXmlReader(absoluteUri)
                    : new XPointerReader(absoluteUri, xpointer));
            }
            else
            {
                document = MasterDocument;
            }

            // Recursion on nsPrefix resolver, decleration must be in root
            string nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXIncludeNew);
            if (string.IsNullOrEmpty(nsPrefix))
            {
                nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXIncludeOld);
            }

            if (!string.IsNullOrEmpty(nsPrefix))
            {
                var nsm = new XmlNamespaceManager(document.NameTable);
                nsm.AddNamespace(nsPrefix, nsOfXIncludeNew);

                // Finds all include elements, which does not have ancestor element fallback.
                XmlElement[] includeNodes = document.SelectNodes($"//{nsPrefix}:{etInclude}[ not( ancestor::{nsPrefix}:{etFallback} ) ]", nsm).OfType<XmlElement>().ToArray<XmlElement>();
                checkingError = TreatIncludes(includeNodes, document, absoluteUri, nsPrefix, nsm);
            }
            if (MasterDocument == null && !checkingError)
            {
                // There are not any includes, insert root of this document to parent document 
                var parent = _documents.Pop();
                // base uri fix.
                //FixBaseUri(parent, document, includeNode);
                // lang fix
                Fixlang(parent, document);

                var importedNode = parent.ImportNode(document.DocumentElement, true); //Import method changes baseuri instead of does not change
                includeNode.ParentNode.ReplaceChild(importedNode, includeNode);
                _replaceCount++;
            }
            else
            {
                _replaceCount = -1;
            }
        }

        //void FixBaseUri(XmlDocument parent, XmlDocument child, XmlElement includeNode)
        //{
        //    if (child.DocumentElement.BaseURI != includeNode.ParentNode.BaseURI && child.DocumentElement.BaseURI != String.Empty)
        //    {
        //        if (parent.DocumentElement.GetNamespaceOfPrefix("xml") != nsOfXml)
        //            parent.DocumentElement.SetAttribute("xmlns:xml", nsOfXml);

        //        var baseUri = child.CreateAttribute("xml", atBase, nsOfXml);
        //        baseUri.Value = child.DocumentElement.BaseURI;

        //        child.DocumentElement.Attributes.Append(baseUri);
        //    }
        //}

        void Fixlang(XmlDocument parent, XmlDocument child)
        {
            if (parent.DocumentElement.Attributes.GetNamedItem(atLang) != null && child.DocumentElement.Attributes.GetNamedItem(atLang) != null)
                if (child.DocumentElement.Attributes.GetNamedItem(atLang).Value != parent.Attributes.GetNamedItem(atLang).Value)
                {
                    if (parent.DocumentElement.GetNamespaceOfPrefix("xml") != nsOfXml)
                        parent.DocumentElement.SetAttribute("xmlns:xml", nsOfXml);

                    XmlAttribute lang = child.CreateAttribute("xml", atLang, nsOfXml);
                    lang.Value = child.DocumentElement.Attributes.GetNamedItem(atLang).Value;

                    child.DocumentElement.Attributes.Append(lang);
                }
        }

        void IncludeText(string absoluteUri, XmlElement includeNode)
        {
            string includingText;

            // Reading xml as a text
            using (var sr = new StreamReader(absoluteUri))
            {
                includingText = sr.ReadToEnd();
            }

            // Replacing text...
            var parent = includeNode.ParentNode;
            var parentDocument = _documents.Pop();
            parent.ReplaceChild(parentDocument.CreateTextNode(includingText), includeNode);

            _replaceCount++;
        }
        /// <summary>
        /// Treats all include tags, if there are bad reference on document, cathes exception and tries to find fallback, which is treated as document.
        /// </summary>
        /// <param name="includeNodes">Nodes, which will be replace</param>
        /// <param name="document">Document, which contains includeNodes</param>
        /// <param name="absoluteUri">Uri of document</param>
        /// <param name="nsPrefix">Prefix of include namespace</param>
        /// <param name="nsm">Namespace Manager of document</param>
        /// <returns></returns>
        bool TreatIncludes(XmlElement[] includeNodes, XmlDocument document, string absoluteUri, string nsPrefix, XmlNamespaceManager nsm)
        {
            foreach (var includeElement in includeNodes)
            {
                var valueOfXPoiner = includeElement.GetAttribute(atXPointer);
                var valueOfHref = includeElement.GetAttribute(atHref);

                if (string.IsNullOrEmpty(valueOfHref)) // There must be xpointer Attribute, if not, fatal error.. 
                {
                    if (string.IsNullOrEmpty(valueOfXPoiner))
                    {
                        PhpLibXml.IssueXmlError(_ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): detected a local recursion with no xpointer in {absoluteUri} in {_ctx.MainScriptFile.Path}", absoluteUri);
                        return true;
                    }
                    _documents.Push(document);
                    var includeUri = includeElement.BaseURI + valueOfXPoiner;
                    if (_references.ContainsKey(includeUri)) // fatal error, cycle recursion
                    {
                        PhpLibXml.IssueXmlError(_ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): detected a recursion in {absoluteUri} in {_ctx.MainScriptFile.Path}", absoluteUri);
                        return true;
                    }
                    _references[absoluteUri] = includeUri;

                    IncludeXml(includeElement.BaseURI, includeElement, null, valueOfXPoiner);
                    return false;
                }

                // Resolving absolute and relative uri...
                string uri = UriResolver(valueOfHref, Path.GetDirectoryName(absoluteUri));

                // Resolving type of parsing.
                string typeOfParse = includeElement.GetAttribute(atParse);
                try
                {
                    if (string.Equals(typeOfParse, "text", StringComparison.OrdinalIgnoreCase))
                    {
                        _documents.Push(document);
                        IncludeText(uri, includeElement);
                    }
                    else
                    {
                        if (_references.ContainsKey(uri + valueOfXPoiner)) // fatal error, cycle recursion
                        {
                            PhpLibXml.IssueXmlError(_ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): detected a recursion in {absoluteUri} in {_ctx.MainScriptFile.Path}", absoluteUri);
                            //return true;
                        }
                        else
                        {
                            _documents.Push(document);
                            _references[absoluteUri] = uri + valueOfXPoiner;
                            IncludeXml(uri, includeElement, null, string.IsNullOrEmpty(valueOfXPoiner) ? null : valueOfXPoiner);
                        }
                    }
                }
                catch (System.IO.FileNotFoundException)
                {
                    PhpLibXml.IssueXmlError(_ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): I/O warning : failed to load external entity &quot;{absoluteUri}&quot; in {_ctx.MainScriptFile.Path}", absoluteUri);
                    _documents.Pop();
                    XmlElement[] fallbacks = includeElement.GetElementsByTagName(nsPrefix + ":" + etFallback).OfType<XmlElement>().ToArray<XmlElement>();

                    if (fallbacks.Length > 1) // fatal error
                    {
                        PhpLibXml.IssueXmlError(_ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): include has multiple fallback children in {_ctx.MainScriptFile.Path}", _ctx.MainScriptFile.Path);
                        return true;
                    }

                    if (fallbacks.Length == 1)
                    {
                        XmlElement[] includes = fallbacks[0].SelectNodes($".//{nsPrefix}:{etInclude}[ not( descendant::{nsPrefix}:{etFallback} ) ]", nsm).OfType<XmlElement>().ToArray();

                        while (fallbacks[0].ChildNodes.Count != 0)
                        {
                            includeElement.ParentNode.InsertAfter(fallbacks[0].LastChild, includeElement);
                        }

                        TreatIncludes(includes, document, absoluteUri, nsPrefix, nsm);

                        includeElement.ParentNode.RemoveChild(includeElement);
                    }
                    else // error missing fallback
                    {
                        PhpLibXml.IssueXmlError(_ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): could not load {absoluteUri}, and no fallback was found in {_ctx.MainScriptFile.Path}", absoluteUri);
                        return true;
                    }
                }
                _references = new Dictionary<string, string>();
            }
            return false;
        }
    }

}
