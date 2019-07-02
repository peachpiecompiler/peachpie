using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Pchp.Core;
using System.IO;
using static Peachpie.Library.XmlDom.XMLReader;
using System.Linq;

namespace Peachpie.Library.XmlDom
{
    internal static class XIncludeHelper
    {
        /// <summary>
        /// Namespace used by xinclude and xml
        /// </summary>
        public const string nsOfXInclude = "http://www.w3.org/2001/XInclude";
        public const string nsOfXml = "xml";

        /// <summary>
        /// element include and fallback
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

        /// <summary>
        /// Recursive method of XInclude
        /// </summary>
        /// <param name="doc"></param>
        /// <returns>Returns -1 when process failded, 0 when there was no substitusion else number of substitusion</returns>
        public static PhpValue ReplaceXIncludes(Context ctx, XmlDocument doc, int options)
        {

            string nsPrefix = doc.DocumentElement.GetPrefixOfNamespace(nsOfXInclude); //prefix of xinclude 

            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace(nsPrefix, nsOfXInclude);

            XmlElement[] nodes = doc.SelectNodes($"//{nsPrefix}:{etInclude}[ not( ancestor::{nsPrefix}:{etFallback} ) ]", nsManager).OfType<XmlElement>().ToArray(); //Include nodes

            //XmlElement[] nodes = doc.GetElementsByTagName(etInclude, nsOfXInclude).OfType<XmlElement>().ToArray(); //include nodes
            //Dictionary<string, string> documentations = new Dictionary<string, string>(); // Helps to control loop on recursive loop
            
            //resolve all include nodes
            foreach (XmlNode node in nodes)
            {
                treatInclude(ctx, doc, node, options);
            }

            return 1;
        }

        /// <summary>
        /// Method which replace include tag for text
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="doc"></param>
        /// <param name="options"></param>
        /// <param name="uRI"></param>
        /// <param name="includeNode"></param>
        public static void includeText(Context ctx, XmlDocument doc, int options, string uRI,XmlNode includeNode)
        {
                StreamReader sr = new StreamReader(uRI);
                string includingText = sr.ReadToEnd();
                sr.Close();
                XmlNode parent = includeNode.ParentNode;
                parent.ReplaceChild(doc.CreateTextNode(includingText), includeNode);
        }

        /// <summary>
        /// Method which replace include tag for xml
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="doc"></param>
        /// <param name="options"></param>
        /// <param name="uRI"></param>
        /// <param name="includeNode"></param>
        public static void includeXml(Context ctx, XmlDocument doc, int options, string uRI, XmlNode includeNode, string xPointer)
        {
            XmlDocument document = new XmlDocument();

            string nsPrefix = doc.DocumentElement.GetPrefixOfNamespace(nsOfXInclude); //prefix of xinclude 

            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace(nsPrefix, nsOfXInclude);

            try
            {
                document.Load(uRI);
            }
            catch (Exception)
            {

                XmlElement[] nodes = includeNode.SelectNodes($"{nsPrefix}:{etFallback}", nsManager).OfType<XmlElement>().ToArray();
                
                if (nodes.Length == 1) //solving fallback tag...
                {
                    XmlElement[] includes = nodes[0].SelectNodes($".//{nsPrefix}:{etInclude}[ not( descendant::{nsPrefix}:{etFallback} ) ]", nsManager).OfType<XmlElement>().ToArray();
                    XmlNode parent = includeNode.ParentNode;
                        while (nodes[0].ChildNodes.Count != 0)
                        {
                            parent.InsertAfter(nodes[0].LastChild, includeNode);
                        }
                    //foreach (XmlNode node in nodes[0].ChildNodes)
                    //{

                    //    parent.InsertAfter(node, includeNode);

                    //}
                    

     


                    //parent.RemoveChild(includeNode);//replacing include tag for fallback childern.   
                    //recursive call on fallback       

                    if (nsPrefix != "")
                    {
                        

                        foreach (XmlNode node in includes)
                        {

                            treatInclude(ctx, doc, node, options);

                        }
                    }
                    parent.RemoveChild(includeNode);
                    return;
                }
                else
                {
                    throw new Exception();
                }
            }

            
            nsPrefix = document.DocumentElement.GetPrefixOfNamespace(nsOfXInclude);
            
            //XmlElement[] nodes = document.GetElementsByTagName(etInclude, nsOfXInclude).OfType<XmlElement>().ToArray(); //include nodes
            XmlNode rootOfAppendDocument = doc.ImportNode(document.DocumentElement, true);
            includeNode.ParentNode.ReplaceChild(rootOfAppendDocument, includeNode);

            nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace(nsPrefix, nsOfXInclude);

            if (nsPrefix != "")
            {
                XmlElement[] nodes = rootOfAppendDocument.SelectNodes($".//{nsPrefix}:{etInclude}[ not( ancestor::{nsPrefix}:{etFallback} ) ]",nsManager).OfType<XmlElement>().ToArray();

                foreach (XmlNode node in nodes)
                {
                    treatInclude(ctx, doc, node, options);
                }
            }
        }


        /// <summary>
        /// Help method for recursion
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="doc"></param>
        public static void treatInclude(Context ctx, XmlDocument doc, XmlNode include, int options)
        {
            
            string location = ((XmlElement)include).GetAttribute(atHref); // get location of next file
            string pointer = ((XmlElement)include).GetAttribute(atXPointer);
            string parser = ((XmlElement)include).GetAttribute(atParse);

            if (location.Length == 0 && pointer.Length == 0)
            {

                return; //failed because of infinity loop 

            }
            else
            {

                if (parser == "text") // parsing text and replacing him.
                {

                    includeText(ctx, doc, options, location, include);

                }
                else
                {                  
                    if (pointer == "") // including hole document
                    {

                            includeXml(ctx, doc, options, location, include, null);

                    }
                    else  //solving xpointer
                    {

                        includeXml(ctx, doc,options, location,include, pointer);
                        
                    }

                }
            }
        }
    }
}
