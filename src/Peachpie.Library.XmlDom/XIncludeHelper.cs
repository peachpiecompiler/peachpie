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
        public const string nsOfXIncludeNew = "http://www.w3.org/2001/XInclude";
        public const string nsOfXIncludeOld = "http://www.w3.org/2003/XInclude";
        public const string nsOfXml = "http://www.w3.org/XML/1998/namespace";
        
        // Elements in namespace http://www.w3.org/2001/XInclude
        public const string etInclude = "include"; // prefix + :include
        public const string etFallback = "fallback";

        // Atributes
        public const string atHref = "href";
        public const string atParse = "parse";
        public const string atXPointer = "xpointer";
        public const string atBase = "base";
        public const string atLang = "lang";
        public const string atEncoding = "encoding";
        #endregion
        
        Stack<XmlDocument> documents = new Stack<XmlDocument>(); // When IncludeXml detects tag include, pushes curent document to documents stack and resolves next documents, after that, pulls previous document in other to includes probing document.
        Dictionary<string, string> references = new Dictionary<string, string>(); // Keeps route of calling documents and controls loop
        int replaceCount = 0; // Counts include replaces.
        Context ctx;

        public XIncludeHelper(Context ctx)
        {
            this.ctx = ctx;
        }

        /// <summary>
        /// If resolvingUri is relative, combines it with absoluteUriOfDirectory.
        /// </summary>
        /// <returns>Returns absolute uri</returns>
        static public string UriResolver(string resolvingUri, string absoluteUriOfDirectory)
        {
            Uri result;
            try
            {
                result = new Uri(resolvingUri, UriKind.Absolute);
            }
            catch (UriFormatException)
            {
                result = new Uri(Path.Combine(absoluteUriOfDirectory, resolvingUri));
            }

            //if (!Uri.TryCreate(resolvingUri, UriKind.Absolute, out result)) // try, if resolvingUri is not absolute path
            //    result = new Uri(Path.Combine(absoluteUriOfDirectory, resolvingUri));
            return result.ToString();
        }

        /// <summary>
        /// Finds all tags Xinclude and treats everyone recursively.
        /// </summary>
        public PhpValue Include(XmlDocument document)
        {
            replaceCount = 0;
            documents = new Stack<XmlDocument>();
            references = new Dictionary<string, string>();
            IncludeXml(document.BaseURI, null, document, "");
            if (replaceCount == 0)
                return PhpValue.False;
            else
                return PhpValue.Create(replaceCount);
        }

        /// <summary>
        /// Finds all tags Xinclude and treats everyone recursively.
        /// </summary>
        /// <param name="absoluteUri">Uri of treated xml document.</param>
        /// <param name="includeNode">Node, which references to document, which Uri is absoluteUri</param>
        /// <param name="MasterDocument">Document, where Xinclude start</param>
        void IncludeXml(string absoluteUri, XmlElement includeNode, XmlDocument MasterDocument, string xpointer)
        {
            XmlDocument document;
            bool checkingError = false;

            if (MasterDocument == null)
            {
                document = new XmlDocument();
                if (xpointer == null)
                    document.Load(new XmlBaseAwareXmlReader(absoluteUri));
                else
                    document.Load((new XPointerReader(absoluteUri,xpointer)));
            }
            else
                document = MasterDocument;

            // Recursion on nsPrefix resolver, decleration must be in root
            string nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXIncludeNew);
            string baseuri = document.DocumentElement.BaseURI;
            if (nsPrefix == "")
                nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXIncludeOld);
            if (nsPrefix != "")
            {
                XmlNamespaceManager nsm = new XmlNamespaceManager(document.NameTable);
                nsm.AddNamespace(nsPrefix, nsOfXIncludeNew);
                
                // Finds all include elements, which does not have ancestor element fallback.
                XmlElement[] includeNodes = document.SelectNodes($"//{nsPrefix}:{etInclude}[ not( ancestor::{nsPrefix}:{etFallback} ) ]", nsm).OfType<XmlElement>().ToArray<XmlElement>();
                checkingError=TreatIncludes(includeNodes, document, absoluteUri, nsPrefix, nsm);    
            }
            if (MasterDocument == null && !checkingError)
            {
                // There are not any includes, insert root of this document to parent document 
                XmlDocument parent = documents.Pop();
                // base uri fix.
                FixBaseUri(parent, document, includeNode);
                // lang fix
                Fixlang(parent, document);

                XmlNode importedNode = parent.ImportNode(document.DocumentElement, true); //Import method changes baseuri instead of does not change
                includeNode.ParentNode.ReplaceChild(importedNode, includeNode);
                replaceCount++;

                return;
            }
            else
                replaceCount = -1;
            return;
        }

        void FixBaseUri(XmlDocument parent, XmlDocument child, XmlElement includeNode)
        {
            if (child.DocumentElement.BaseURI != includeNode.ParentNode.BaseURI && child.DocumentElement.BaseURI != String.Empty)
            {
                if (parent.DocumentElement.GetNamespaceOfPrefix("xml") != nsOfXml)
                    parent.DocumentElement.SetAttribute("xmlns:xml", nsOfXml);

                XmlAttribute baseUri = child.CreateAttribute("xml", atBase, nsOfXml);
                baseUri.Value = child.DocumentElement.BaseURI;

                child.DocumentElement.Attributes.Append(baseUri);
            }
        }

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
            // Reading xml as a text
            StreamReader sr = new StreamReader(absoluteUri);
            string includingText = sr.ReadToEnd();
            sr.Close();
            // Replacing text...
            XmlNode parent = includeNode.ParentNode;
            XmlDocument parentDocument = documents.Pop();
            parent.ReplaceChild(parentDocument.CreateTextNode(includingText), includeNode);
            replaceCount ++;

            return;
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
            foreach (XmlElement includeElement in includeNodes)
            {
                string valueOfXPoiner = includeElement.GetAttribute(atXPointer);
                string valueOfHref = includeElement.GetAttribute(atHref);

                if (valueOfHref == String.Empty) // There must be xpointer Attribute, if not, fatal error.. 
                {
                    if (valueOfXPoiner == String.Empty) 
                    {
                        PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): detected a local recursion with no xpointer in {absoluteUri} in {ctx.MainScriptFile.Path}", absoluteUri);
                        return true;
                    }
                    documents.Push(document);
                    if (references.ContainsKey(includeElement.BaseURI + valueOfXPoiner)) //fatal error, cycle recursion
                    {
                        PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING,0,0,0, $"DOMDocument::xinclude(): detected a recursion in {absoluteUri} in {ctx.MainScriptFile.Path}", absoluteUri);
                        return true; 
                    }
                    references[absoluteUri] = includeElement.BaseURI + valueOfXPoiner;
                    IncludeXml(includeElement.BaseURI, includeElement, null, valueOfXPoiner);
                    return false;
                }

                // Resolving absolute and relative uri...
                string uri = UriResolver(valueOfHref, Path.GetDirectoryName(absoluteUri));

                // Resolving type of parsing.
                string typeOfParse = includeElement.GetAttribute(atParse);
                try
                {
                    if (typeOfParse == "text")
                    {
                        documents.Push(document);
                        IncludeText(uri, includeElement);
                    }
                    else
                    {
                        documents.Push(document);
                        if (valueOfXPoiner == String.Empty)
                        {
                            if (references.ContainsKey(uri)) //fatal error, cycle recursion
                            {
                                PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): detected a recursion in {absoluteUri} in {ctx.MainScriptFile.Path}", absoluteUri);
                                return true;
                            }
                            references[absoluteUri] = uri;
                            IncludeXml(uri, includeElement, null, null);
                        }
                        else
                        {
                            if (references.ContainsKey(uri + valueOfXPoiner)) // fatal error, cycle recursion
                            {
                                PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): detected a recursion in {absoluteUri} in {ctx.MainScriptFile.Path}", absoluteUri);
                                return true;   
                            }
                            references[absoluteUri] = uri+valueOfXPoiner;
                            IncludeXml(uri, includeElement, null, valueOfXPoiner);
                        }
                    }
                }
                catch (System.IO.FileNotFoundException)
                {
                    PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): I/O warning : failed to load external entity &quot;{absoluteUri}&quot; in {ctx.MainScriptFile.Path}", absoluteUri);
                    documents.Pop();
                    XmlElement[] fallbacks = includeElement.GetElementsByTagName(nsPrefix + ":" + etFallback).OfType<XmlElement>().ToArray<XmlElement>();

                    if (fallbacks.Length > 1) // fatal error
                    {
                        PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): include has multiple fallback children in {ctx.MainScriptFile.Path}", ctx.MainScriptFile.Path);
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
                        PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): could not load {absoluteUri}, and no fallback was found in {ctx.MainScriptFile.Path}", absoluteUri);
                        return true;
                    }
                }
                references = new Dictionary<string, string>();
            }
            return false;
        }
    }

}
