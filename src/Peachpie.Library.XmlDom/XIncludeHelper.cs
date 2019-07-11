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
    class XIncludeHelper
    {
        #region Tags, atributes, namespaces
        //Namespaces
        public const string nsOfXIncludeNew = "http://www.w3.org/2001/XInclude";
        public const string nsOfXIncludeOld = "http://www.w3.org/2003/XInclude";
        public const string nsOfXml = "http://www.w3.org/XML/1998/namespace";
        
        //Elements in namespace http://www.w3.org/2001/XInclude
        public const string etInclude = "include"; // prefix + :include
        public const string etFallback = "fallback";

        //Atributes
        public const string atHref = "href";
        public const string atParse = "parse";
        public const string atXPointer = "xpointer";
        public const string atBase = "base";
        public const string atLang = "lang";
        public const string atEncoding = "encoding";
        #endregion

        Stack<XmlDocument> documents=new Stack<XmlDocument>();
        Dictionary<string, string> references = new Dictionary<string, string>();
        int replaceCount = 0;
        Context ctx;

        /// <summary>
        /// Constructor of class
        /// </summary>
        /// <param name="ctx"></param>
        public XIncludeHelper(Context ctx)
        {
            this.ctx = ctx;
        }

        /// <summary>
        /// If resolvingUri is relative, combines it with absoluteUriOfDirectory.
        /// </summary>
        /// <param name="resolvingUri"></param>
        /// <param name="absoluteUriOfDirectory"></param>
        /// <returns></returns>
        static public string UriResolver(string resolvingUri, string absoluteUriOfDirectory)
        {
            Uri result;
            if (!Uri.TryCreate(resolvingUri, UriKind.Absolute, out result)) // try, if resolvingUri is not absolute path
                result = new Uri(Path.Combine(absoluteUriOfDirectory, resolvingUri));
            return result.ToString();
        }

        /// <summary>
        /// Find all tags include in namespace http://www.w3.org/2001/XInclude and treat everyone recursively.
        /// </summary>
        /// <param name="absoluteUri">Uri of treated xml document.</param>
        /// <param name="includeNode">Node, which references to document, which Uri is absoluteUri</param>
        /// <param name="MasterDocument">Document, where Xinclude start</param>
        /// <returns></returns>
        public int XIncludeXml(string absoluteUri, XmlElement includeNode, XmlDocument MasterDocument, string xpointer)
        {
            XmlDocument document;
            int checkingError = 0;

            if (MasterDocument == null)
            {
                document = new XmlDocument();
                if (xpointer == null)
                    document.Load(absoluteUri);
                else
                    document.Load((new XPointerReader(absoluteUri,xpointer)));
            }
            else
                document = MasterDocument;

            // recursion on nsPrefix resolver, decleration must be in root
            string nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXIncludeNew);
            string baseuri = document.DocumentElement.BaseURI;
            if (nsPrefix == "")
                nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXIncludeOld);
            if (nsPrefix != "")
            {
                XmlNamespaceManager nsm = new XmlNamespaceManager(document.NameTable);
                nsm.AddNamespace(nsPrefix, nsOfXIncludeNew);
                
                //Find all include elements, which does not have ancestor element fallback.
                XmlElement[] includeNodes = document.SelectNodes($"//{nsPrefix}:{etInclude}[ not( ancestor::{nsPrefix}:{etFallback} ) ]", nsm).OfType<XmlElement>().ToArray<XmlElement>();
                checkingError=TreatIncludes(includeNodes, document, absoluteUri, nsPrefix, nsm);    
            }
            if (MasterDocument == null && checkingError == 0)
            {
                //There are not any includes, insert root of this document to parent document 
                XmlDocument parent = documents.Pop();
                //base uri fix.
                baseUriFix(parent, document, includeNode);//problem with xpointer, base uri is Empty
                //lang fix
                langFix(parent, document);

                XmlNode importedNode = parent.ImportNode(document.DocumentElement, true); //Import method changes baseuri instead of does not change
                includeNode.ParentNode.ReplaceChild(importedNode, includeNode);
                replaceCount++;

                return 0;
            }
            return replaceCount;
        }

        void baseUriFix(XmlDocument parent, XmlDocument child, XmlElement includeNode)
        {
            if (child.DocumentElement.BaseURI != includeNode.ParentNode.BaseURI)
            {
                parent.DocumentElement.SetAttribute("xmlns:xml", nsOfXml);

                XmlAttribute baseUri = child.CreateAttribute("xml", atBase, nsOfXml);
                baseUri.Value = child.DocumentElement.BaseURI;

                child.DocumentElement.Attributes.Append(baseUri);
            }
        }

        void langFix(XmlDocument parent, XmlDocument child)
        {
            if (parent.DocumentElement.Attributes.GetNamedItem(atLang) != null && child.DocumentElement.Attributes.GetNamedItem(atLang) != null)
                if (child.DocumentElement.Attributes.GetNamedItem(atLang).Value != parent.Attributes.GetNamedItem(atLang).Value)
                {
                    parent.DocumentElement.SetAttribute("lang:xml", nsOfXml);

                    XmlAttribute lang = child.CreateAttribute("xml", atLang, nsOfXml);
                    lang.Value = child.DocumentElement.Attributes.GetNamedItem(atLang).Value;

                    child.DocumentElement.Attributes.Append(lang);
                }
        }

        int XIncludeText(string absoluteUri, XmlElement includeNode)
        {
            //Reading xml as a text
            StreamReader sr = new StreamReader(absoluteUri);
            string includingText = sr.ReadToEnd();
            sr.Close();
            //Replacing text...
            XmlNode parent = includeNode.ParentNode;
            XmlDocument parentDocument = documents.Pop();
            parent.ReplaceChild(parentDocument.CreateTextNode(includingText), includeNode);
            replaceCount ++;

            return 0;
        }

        int TreatIncludes(XmlElement[] includeNodes, XmlDocument document, string absoluteUri, string nsPrefix, XmlNamespaceManager nsm)
        {
            foreach (XmlElement includeElement in includeNodes)
            {
                string valueOfXPoiner = includeElement.GetAttribute(atXPointer);
                string valueOfHref = includeElement.GetAttribute(atHref);

                if (valueOfHref == String.Empty) // There must be xpointer Attribute, if not, fatal error.. 
                {
                    if (valueOfXPoiner == String.Empty)
                        return 0; // fatal error
                    documents.Push(document);
                    if (references.ContainsKey(includeElement.BaseURI + valueOfXPoiner))
                    {
                        PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING,0,0,0, $"DOMDocument::xinclude(): detected a recursion in {absoluteUri} in {ctx.MainScriptFile.Path}", absoluteUri);
                        return -1;
                        //throw new Exception();
                        //fatal error, cycle recursion
                    }
                    references[absoluteUri] = includeElement.BaseURI + valueOfXPoiner;
                    XIncludeXml(includeElement.BaseURI, includeElement, null, valueOfXPoiner);
                    return 0;
                }

                //Resolving absolute and relative uri...
                string uri = UriResolver(valueOfHref, Path.GetDirectoryName(absoluteUri));

                //Resolving type of parsing.
                string typeOfParse = includeElement.GetAttribute(atParse);
                try
                {
                    if (typeOfParse == "text")
                    {
                        documents.Push(document);
                        XIncludeText(uri, includeElement);
                    }
                    else
                    {
                        documents.Push(document);
                        if (valueOfXPoiner == String.Empty)
                        {
                            if (references.ContainsKey(uri))
                            {
                                //throw new Exception();
                                PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): detected a recursion in {absoluteUri} in {ctx.MainScriptFile.Path}", absoluteUri);
                                return -1;
                                //fatal error, cycle recursion
                            }
                            references[absoluteUri] = uri;
                            XIncludeXml(uri, includeElement, null, null);
                        }
                        else
                        {
                            if (references.ContainsKey(uri + valueOfXPoiner))
                            {
                                //throw new Exception();
                                PhpLibXml.IssueXmlError(ctx, PhpLibXml.LIBXML_ERR_WARNING, 0, 0, 0, $"DOMDocument::xinclude(): detected a recursion in {absoluteUri} in {ctx.MainScriptFile.Path}", absoluteUri);
                                return -1;
                                //fatal error, cycle recursion
                            }
                            references[absoluteUri] = uri+valueOfXPoiner;
                            XIncludeXml(uri, includeElement, null, valueOfXPoiner);
                        }
                    }
                }
                catch (System.IO.FileNotFoundException ex)
                {
                    documents.Pop();
                    //Get fallbacknodes
                    XmlElement[] fallbacks = includeElement.GetElementsByTagName(nsPrefix + ":" + etFallback).OfType<XmlElement>().ToArray<XmlElement>();
                    if (fallbacks.Length > 1)
                        return 0; //Fatal error
                    if (fallbacks.Length == 1)
                    {
                        XmlElement[] includes = fallbacks[0].SelectNodes($".//{nsPrefix}:{etInclude}[ not( descendant::{nsPrefix}:{etFallback} ) ]", nsm).OfType<XmlElement>().ToArray();

                        while (fallbacks[0].ChildNodes.Count != 0)
                        {
                            includeElement.ParentNode.InsertAfter(fallbacks[0].LastChild, includeElement);
                        }

                        TreatIncludes(includes, document, absoluteUri, nsPrefix,nsm);

                        includeElement.ParentNode.RemoveChild(includeElement);
                    }
                    else
                        throw ex; //error missing fallback
                }
                references = new Dictionary<string, string>();
            }
            return 0;
        }
    }

}
