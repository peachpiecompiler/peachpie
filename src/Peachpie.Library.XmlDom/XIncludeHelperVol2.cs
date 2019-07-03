using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Peachpie.Library.XmlDom
{
    static class XIncludeHelperVol2
    {

        #region Tags,atributes,namespaces

        /// <summary>
        /// namespaces
        /// </summary>
        public const string nsOfXIncludeNew = "http://www.w3.org/2001/XInclude";
        public const string nsOfXIncludeOld = "http://www.w3.org/2003/XInclude";
        public const string nsOfXml = "http://www.w3.org/XML/1998/namespace";

        /// <summary>
        /// elements
        /// </summary>
        public const string etInclude = "include"; // prefix + :include
        public const string etFallback = "fallback";

        /// <summary>
        /// attributes
        /// </summary>
        public const string atHref = "href";
        public const string atParse = "parse";
        public const string atXPointer = "xpointer";
        public static string atBase = "base";
        public static string atLang = "lang";
        public static string atEncoding = "encoding";
        #endregion

        static Stack<XmlDocument> documents=new Stack<XmlDocument>();
        static int replaceCount = 0;

        public static int XIncludeXml(string absoluteUri,XmlElement includeNode,XmlDocument MasterDocument)
        {
            XmlDocument document;
            if (MasterDocument == null)
            {
                document = new XmlDocument();
                document.Load(absoluteUri);
            }
            else
                document = MasterDocument;

            string nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXIncludeNew);
            if (nsPrefix == "")
            {
                nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXIncludeOld);
            }
            if (nsPrefix != "")
            {
                XmlNamespaceManager nsm = new XmlNamespaceManager(document.NameTable);
                nsm.AddNamespace(nsPrefix, nsOfXIncludeNew);

                //Find all include elements, which does not have ancestor element fallback.
                XmlElement[] includeNodes = document.SelectNodes($"//{nsPrefix}:{etInclude}[ not( ancestor::{nsPrefix}:{etFallback} ) ]", nsm).OfType<XmlElement>().ToArray<XmlElement>();

                //Treat all include nodes
                foreach (XmlElement includeElement in includeNodes)
                {
                    string valueOfXPoiner = includeElement.GetAttribute(atXPointer);
                    string valueOfHref = includeElement.GetAttribute(atHref);

                    if (valueOfHref == String.Empty) // There must be xpointer Attribute, if not, fatal error.. 
                    {
                        if (valueOfXPoiner == String.Empty)
                            return 0; // fatal error
                                      //TODO: TreatXpointer
                        return 0;
                    }

                    //Resolving absolute and relative uri...
                    string uri;
                    Uri result;
                    if (!Uri.TryCreate(valueOfHref, UriKind.Absolute, out result)) // try, if href is not absolute path
                    {
                        string workingDirectory;
                        if (document.BaseURI == String.Empty)
                            workingDirectory = Path.GetDirectoryName(absoluteUri);
                        else
                            workingDirectory = Path.GetDirectoryName(document.BaseURI);
                        uri = Path.Combine(workingDirectory, valueOfHref);
                    }
                    else
                        uri = result.ToString();

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
                            if (valueOfXPoiner == String.Empty)
                            {
                                documents.Push(document);
                                XIncludeXml(uri, includeElement, null);
                            }
                            else
                            {
                                //TODO: Treat Xpointer...
                            }
                        }
                    }
                    catch (System.IO.FileNotFoundException ex)
                    {
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

                            TreatIncludes(includes, document, absoluteUri, nsPrefix);

                            includeElement.ParentNode.RemoveChild(includeElement);
                        }
                        else
                            throw ex;
                    }
                }
            }
            if (MasterDocument == null)
            {
                //There are not any includes, insert root of this document to parent document 
                XmlDocument parent = documents.Pop();

                //base uri fix.
                if (document.DocumentElement.BaseURI != includeNode.ParentNode.BaseURI)
                {
                    parent.DocumentElement.SetAttribute("xmlns:xml", nsOfXml);

                    XmlAttribute baseUri = document.CreateAttribute("xml", atBase, nsOfXml);
                    baseUri.Value = document.DocumentElement.BaseURI;

                    document.DocumentElement.Attributes.Append(baseUri);
                }

                //lang  fix.
                if (parent.DocumentElement.Attributes.GetNamedItem(atLang) != null && document.DocumentElement.Attributes.GetNamedItem(atLang) != null)
                {
                    if (document.DocumentElement.Attributes.GetNamedItem(atLang).Value != parent.Attributes.GetNamedItem(atLang).Value)
                    {
                        parent.DocumentElement.SetAttribute("lang:xml", nsOfXml);

                        XmlAttribute lang = document.CreateAttribute("xml", atLang, nsOfXml);
                        lang.Value = document.DocumentElement.Attributes.GetNamedItem(atLang).Value;

                        document.DocumentElement.Attributes.Append(lang);
                    }
                }

                XmlNode importedNode = parent.ImportNode(document.DocumentElement, true);

                includeNode.ParentNode.ReplaceChild(importedNode, includeNode);
                replaceCount++;

                return 0;
            }
            return replaceCount;
        }

        public static int XIncludeText(string absoluteUri,XmlElement includeNode)
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

        public static int TreatIncludes(XmlElement[] includeNodes, XmlDocument document, string absoluteUri, string nsPrefix)
        {
            foreach (XmlElement includeElement in includeNodes)
            {
                string valueOfXPoiner = includeElement.GetAttribute(atXPointer);
                string valueOfHref = includeElement.GetAttribute(atHref);

                if (valueOfHref == String.Empty) // There must be xpointer Attribute, if not, fatal error.. 
                {
                    if (valueOfXPoiner == String.Empty)
                        return 0; // fatal error
                                  //TODO: TreatXpointer
                    return 0;
                }

                //Resolving absolute and relative uri...
                string uri;
                Uri result;
                if (!Uri.TryCreate(valueOfHref, UriKind.Absolute, out result)) // try, if href is not absolute path
                {
                    string workingDirectory;
                    if (document.BaseURI == String.Empty)
                        workingDirectory = Path.GetDirectoryName(absoluteUri);
                    else
                        workingDirectory = Path.GetDirectoryName(document.BaseURI);
                    uri = Path.Combine(workingDirectory, valueOfHref);
                }
                else
                    uri = result.ToString();

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
                        if (valueOfXPoiner == String.Empty)
                        {
                            documents.Push(document);
                            XIncludeXml(uri, includeElement, null);
                        }
                        else
                        {
                            //TODO: Treat Xpointer...
                            
                        }
                    }
                }
                catch (System.IO.FileNotFoundException ex)
                {
                    //Get fallbacknodes
                    XmlElement[] fallbacks = includeElement.GetElementsByTagName(nsPrefix + ":" + etFallback).OfType<XmlElement>().ToArray<XmlElement>();
                    if (fallbacks.Length > 1)
                        return 0; //Fatal error
                    if (fallbacks.Length == 1)
                    {
                        XmlElement[] includes = fallbacks[0].GetElementsByTagName(nsPrefix + ":" + etInclude).OfType<XmlElement>().ToArray<XmlElement>();
                        TreatIncludes(includes, document, absoluteUri, nsPrefix);
                    }
                    else
                        throw ex;
                }
            }
            return 0;
        }

    }
}
