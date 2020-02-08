using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Enumerates the clone behavior. (Where is this supposed to be used?)
    /// </summary>
    public enum CloneType
    {
        Auto = 0,
        Never = 1,
        Always = -1
    }

    [PhpExtension("dom")]
    public static class XsltConstants
    {
        public const int XSL_CLONE_AUTO = (int)CloneType.Auto;
        public const int XSL_CLONE_NEVER = (int)CloneType.Never;
        public const int XSL_CLONE_ALWAYS = (int)CloneType.Always;
    }

    /// <summary>
    /// Implements the XSLT processor.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("xsl")]
    public class XSLTProcessor
    {
        #region Delegates

        private delegate void LoadDelegate(IXPathNavigable stylesheet);
        private delegate XmlWriterSettings GetOutputSettingsDelegate();
        private delegate void TransformToWriterDelegate(IXPathNavigable input, XsltArgumentList arguments, System.Xml.XmlWriter results);

        #endregion

        #region Fields and Properties

        private Context _ctx;

        private LoadDelegate Load;
        private GetOutputSettingsDelegate GetOutputSettings;
        private TransformToWriterDelegate TransformToWriter;

        private XsltArgumentList xsltArgumentList;
        private XsltUserFunctionHandler xsltUserFunctionHandler;

        private const string PhpNameSpaceUri = "http://php.net/xsl";

        private static bool mvpXmlAvailable;
        private static Type mvpXmlType;

        private static MethodInfo getOutputSettingsMethodFW;
        private static MethodInfo loadMethodMvp;
        private static MethodInfo getOutputSettingsMethodMvp;
        private static MethodInfo transformToWriterMethodMvp;
        private static MethodInfo transformToStreamMethodMvp;

        #endregion

        #region Construction

        /// <summary>
        /// Determines whether Mvp.Xml is available and reflects the MvpXslTransform type.
        /// </summary>
        static XSLTProcessor()
        {
            getOutputSettingsMethodFW = typeof(XslCompiledTransform).GetProperty("OutputSettings").GetGetMethod();

            // try to load the Mvp.Xml assembly
            try
            {
                Assembly mvp_xml_assembly = Assembly.Load("Mvp.Xml, Version=2.0.2158.1055, Culture=neutral, PublicKeyToken=dd92544dc05f5671");
                mvpXmlType = mvp_xml_assembly.GetType("Mvp.Xml.Exslt.ExsltTransform");

                if (mvpXmlType != null)
                {
                    loadMethodMvp = mvpXmlType.GetMethod("Load", new Type[] { typeof(IXPathNavigable) });
                    getOutputSettingsMethodMvp = mvpXmlType.GetProperty("OutputSettings").GetGetMethod();
                    transformToWriterMethodMvp = mvpXmlType.GetMethod("Transform", new Type[] { typeof(IXPathNavigable), typeof(XsltArgumentList), typeof(XmlWriter) });
                    transformToStreamMethodMvp = mvpXmlType.GetMethod("Transform", new Type[] { typeof(IXPathNavigable), typeof(XsltArgumentList), typeof(Stream) });

                    mvpXmlAvailable =
                        (loadMethodMvp != null &&
                        getOutputSettingsMethodMvp != null &&
                        transformToWriterMethodMvp != null &&
                        transformToStreamMethodMvp != null);
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        public XSLTProcessor(Context ctx)
        {
            _ctx = ctx;

            if (mvpXmlAvailable)
            {
                object transform = Activator.CreateInstance(mvpXmlType);

                Load = (LoadDelegate)Delegate.CreateDelegate(typeof(LoadDelegate), transform, loadMethodMvp);
                GetOutputSettings = (GetOutputSettingsDelegate)Delegate.CreateDelegate(typeof(GetOutputSettingsDelegate),
                    transform, getOutputSettingsMethodMvp);

                TransformToWriter = (TransformToWriterDelegate)Delegate.CreateDelegate(typeof(TransformToWriterDelegate),
                    transform, transformToWriterMethodMvp);
            }
            else
            {
                // Mvp.Xml not available -> falling back to XslCompiledTransform
                XslCompiledTransform transform = new XslCompiledTransform();

                Load = new LoadDelegate(transform.Load);
                GetOutputSettings = (GetOutputSettingsDelegate)
                    Delegate.CreateDelegate(typeof(GetOutputSettingsDelegate), transform, getOutputSettingsMethodFW);

                TransformToWriter = new TransformToWriterDelegate(transform.Transform);
            }

            this.xsltArgumentList = new XsltArgumentList();
        }

        //public override bool ToBoolean()
        //{
        //    return true;
        //}

        #endregion

        #region Transformation

        /// <summary>
        /// Import a stylesheet.
        /// </summary>
        /// <param name="doc">The imported style sheet passed as a <see cref="DOMDocument"/> object.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public bool importStylesheet(DOMDocument doc)
        {
            try
            {
                Load(doc.XmlDocument);
            }
            catch (XsltException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Transforms the source node to a <see cref="DOMDocument"/> applying the stylesheet given by the
        /// <see cref="importStylesheet(DOMDocument)"/> method.
        /// </summary>
        /// <param name="node">The node to be transformed.</param>
        /// <returns>The resulting <see cref="DOMDocument"/> or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public DOMDocument transformToDoc(DOMNode node)
        {
            var doc = PhpXmlDocument.Create();

            using (MemoryStream stream = new MemoryStream())
            {
                if (!TransformInternal(node.XmlNode, stream))
                {
                    return null;
                }

                stream.Seek(0, SeekOrigin.Begin);

                // build the resulting XML document
                try
                {
                    doc.Load(stream);
                }
                catch (XmlException e)
                {
                    PhpException.Throw(PhpError.Warning, e.Message);
                    return null;
                }
            }

            return new DOMDocument(doc);
        }

        /// <summary>
        /// Transforms the source node to an URI applying the stylesheet given by the
        /// <see cref="importStylesheet(DOMDocument)"/> method.
        /// </summary>
        /// <param name="ctx">The current runtime context.</param>
        /// <param name="doc">The document to transform.</param>
        /// <param name="uri">The destination URI.</param>
        /// <returns>Returns the number of bytes written or <B>false</B> if an error occurred.</returns>
        public PhpValue transformToUri(Context ctx, DOMDocument doc, string uri)
        {
            using (PhpStream stream = PhpStream.Open(ctx, uri, "wt"))
            {
                if (stream == null) return PhpValue.Create(false);

                if (!TransformInternal(doc.XmlNode, stream.RawStream))
                {
                    return PhpValue.Create(false);
                }

                // TODO:
                return PhpValue.Create(stream.RawStream.CanSeek ? stream.RawStream.Position : 1);
            }
        }

        /// <summary>
        /// Transforms the source node to a string applying the stylesheet given by the
        /// <see cref="importStylesheet(DOMDocument)"/> method.
        /// </summary>
        /// <param name="doc">The document to transform.</param>
        /// <returns>The result of the transformation as a string or FALSE on error.</returns>
        [return: CastToFalse]
        public PhpString transformToXml(DOMDocument doc)
        {
            // writing to a StringWriter would result in forcing UTF-16 encoding
            using (MemoryStream stream = new MemoryStream())
            {
                if (!TransformInternal(doc.XmlNode, stream))
                {
                    return default(PhpString);
                }

                return new PhpString(stream.ToArray());
            }
        }

        private bool TransformInternal(IXPathNavigable input, Stream stream)
        {
            XmlWriterSettings settings = GetOutputSettings();
            if (settings.Encoding is UTF8Encoding)
            {
                // no BOM in UTF-8 please!
                settings = settings.Clone();
                settings.Encoding = new UTF8Encoding(false);
            }

            using (var writer = System.Xml.XmlWriter.Create(stream, settings))
            {
                // transform the document
                try
                {
                    TransformToWriter(input, xsltArgumentList, writer);
                }
                catch (XsltException e)
                {
                    if (e.InnerException != null)
                    {
                        // ScriptDiedException etc.
                        throw e.InnerException;
                    }

                    PhpException.Throw(PhpError.Warning, e.Message);
                    return false;
                }
                catch (InvalidOperationException e)
                {
                    PhpException.Throw(PhpError.Warning, e.Message);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///  Sets value for a parameter.
        /// </summary>
        /// <param name="ns">The namespace URI of the XSLT parameter.</param>
        /// <param name="name">The local name of the XSLT parameter or an array of name =&gt; option pairs.</param>
        /// <param name="value">The new value of the XSLT parameter.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public bool setParameter(string ns, PhpArray name, string value = null)
        {
            // set all name => value pairs contained in the array
            foreach (var pair in name)
            {
                if (!pair.Key.IsString)
                {
                    PhpException.Throw(PhpError.Warning, Resources.InvalidParameterKey);
                    return false;
                }

                if (xsltArgumentList.GetParam(pair.Key.String, ns) != null)
                {
                    xsltArgumentList.RemoveParam(pair.Key.String, ns);
                }
                xsltArgumentList.AddParam(pair.Key.String, ns, XsltConvertor.PhpToDotNet(_ctx, pair.Value));
            }

            return true;
        }

        /// <summary>
        ///  Sets value for a parameter.
        /// </summary>
        /// <param name="ns">The namespace URI of the XSLT parameter.</param>
        /// <param name="name">The local name of the XSLT parameter or an array of name =&gt; option pairs.</param>
        /// <param name="value">The new value of the XSLT parameter.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public bool setParameter(string ns, string name, string value = null)
        {
            if (xsltArgumentList.GetParam(name, ns) != null) xsltArgumentList.RemoveParam(name, ns);
            xsltArgumentList.AddParam(name, ns, value ?? "" /*XsltConvertor.PhpToDotNet(_ctx, PhpValue.Create(value))*/);

            return true;
        }

        /// <summary>
        /// Gets value of a parameter.
        /// </summary>
        /// <param name="ns">The namespace URI of the XSLT parameter.</param>
        /// <param name="name">The local name of the XSLT parameter.</param>
        /// <returns>The value of the parameter (as a string), or FALSE if it's not set.</returns>
        [return: CastToFalse]
        public string getParameter(string ns, string name)
        {
            return xsltArgumentList.GetParam(name, ns) as string; //XsltConvertor.DotNetToPhp(xsltArgumentList.GetParam(name, ns));
        }

        /// <summary>
        /// Removes a parameter.
        /// </summary>
        /// <param name="ns">The namespace URI of the XSLT parameter.</param>
        /// <param name="name">The local name of the XSLT parameter.</param>
        /// <returns><B>True</B> or <B>false</B>.</returns>
        public bool removeParameter(string ns, string name)
        {
            return (xsltArgumentList.RemoveParam(name, ns) != null);
        }

        /// <summary>
        /// Determine if this extension has EXSLT support.
        /// </summary>
        /// <returns><B>False</B>.</returns>
        /// <remarks>
        /// A EXSLT implementation for the .NET XSL can be found here
        /// <A href="http://mvp-xml.sourceforge.net/exslt/">http://mvp-xml.sourceforge.net/exslt/</A>.</remarks>
        public bool hasExsltSupport()
        {
            if (!mvpXmlAvailable)
            {
                PhpException.Throw(PhpError.Notice, Resources.ExsltSupportMissing);
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Enables the ability to use PHP functions as XSLT functions.
        /// </summary>
        /// <param name="restrict">A string or array denoting function(s) to be made callable.</param>
        public void registerPHPFunctions(string restrict)
        {
            EnsureXsltUserFunctionHandler();

            xsltUserFunctionHandler.RegisterFunction(restrict);
        }

        /// <summary>
        /// Enables the ability to use PHP functions as XSLT functions.
        /// </summary>
        /// <param name="restrict">A string or array denoting function(s) to be made callable.</param>
        public void registerPHPFunctions(PhpArray restrict)
        {
            EnsureXsltUserFunctionHandler();

            foreach (var pair in restrict)
            {
                xsltUserFunctionHandler.RegisterFunction(pair.Value.ToString(_ctx));
            }
        }

        /// <summary>
        /// Enables the ability to use PHP functions as XSLT functions.
        /// </summary>
        public void registerPHPFunctions()
        {
            EnsureXsltUserFunctionHandler();

            xsltUserFunctionHandler.RegisterAllFunctions();
        }

        private void EnsureXsltUserFunctionHandler()
        {
            if (xsltUserFunctionHandler == null)
            {
                xsltUserFunctionHandler = new XsltUserFunctionHandler(_ctx);
                xsltArgumentList.AddExtensionObject(PhpNameSpaceUri, xsltUserFunctionHandler);
            }
        }

        #endregion
    }

    /// <summary>
    /// Provides conversion routines between .NET and PHP representation of W3C data types.
    /// </summary>
    internal static class XsltConvertor
    {
        #region Conversions

        /// <summary>
        /// Converts a W3C .NET object to the corresponding W3C PHP object.
        /// </summary>
        public static PhpValue DotNetToPhp(object arg)
        {
            // Result Tree Fragment (XSLT) / Node (XPath)
            XPathNavigator nav = arg as XPathNavigator;
            if (nav != null) return PhpValue.FromClass(DOMNode.Create(nav.UnderlyingObject as XmlNode));

            // Node Set (XPath) - XPathNavigator[]
            XPathNavigator[] navs = arg as XPathNavigator[];
            if (navs != null)
            {
                PhpArray array = new PhpArray(navs.Length);

                for (int i = 0; i < navs.Length; i++)
                {
                    var node = DOMNode.Create(navs[i].UnderlyingObject as XmlNode);
                    if (node != null) array.Add(node);
                }

                return PhpValue.Create(array);
            }

            // Node Set (XPath) - XPathNodeIterator
            XPathNodeIterator iter = arg as XPathNodeIterator;
            if (iter != null)
            {
                PhpArray array = new PhpArray();

                foreach (XPathNavigator navigator in iter)
                {
                    var node = DOMNode.Create(navigator.UnderlyingObject as XmlNode);
                    if (node != null) array.Add(node);
                }

                return PhpValue.Create(array);
            }

            // Number (XPath), Boolean (XPath), String (XPath)
            return PhpValue.FromClr(arg);
        }

        /// <summary>
        /// Converts a W3C PHP object to the corresponding W3C .NET object.
        /// </summary>
        public static object/*!*/ PhpToDotNet(Context ctx, PhpValue arg)
        {
            if (arg.IsNull)
            {
                return String.Empty;
            }

            // Node* (XPath)
            if (arg.IsObject)
            {
                var node = arg.Object as DOMNode;
                if (node != null) return node.XmlNode.CreateNavigator();

                // Node Set (XPath), Result Tree Fragment (XSLT)
                DOMNodeList list = arg.Object as DOMNodeList;
                if (list != null)
                {
                    XPathNavigator[] navs = new XPathNavigator[list.length];

                    for (int i = 0; i < list.length; i++)
                    {
                        navs[i] = list.item(i).XmlNode.CreateNavigator();
                    }

                    return navs;
                }

                // any other object
                return arg.ToString(ctx);
            }

            // TODO: Handle PhpArray separately?
            // String (XPath), Boolean (XPath), Number (XPath)
            return arg.ToClr();
        }

        /// <summary>
        /// Converts a W3C PHP object to a corresponding string.
        /// </summary>
        public static string/*!*/ PhpToString(Context ctx, PhpValue arg)
        {
            if (arg.IsObject)
            {
                // Node* (XPath)
                var node = arg.Object as DOMNode;
                if (node != null) return node.XmlNode.Value;

                // Node Set (XPath), Result Tree Fragment (XSLT)
                DOMNodeList list = arg.Object as DOMNodeList;
                if (list != null)
                {
                    if (list.length == 0) return String.Empty;
                    return list.item(0).XmlNode.Value;
                }
            }

            // any other object
            return arg.ToString(ctx);
        }

        #endregion
    }

    /// <summary>
    /// Handles PHP function invocations via <code>php:function</code> and <code>php:functionString</code>.
    /// </summary>
    internal sealed class XsltUserFunctionHandler
    {
        #region Fields

        private Context _ctx;

        private bool allFunctionsRegistered;
        private Dictionary<string, PhpCallback> registeredFunctions = new Dictionary<string, PhpCallback>();

        #endregion

        #region Construction

        public XsltUserFunctionHandler(Context ctx)
        {
            _ctx = ctx;
        }

        #endregion

        #region Function registration

        internal void RegisterAllFunctions()
        {
            allFunctionsRegistered = true;
        }

        internal void RegisterFunction(string functionName)
        {
            if (!registeredFunctions.ContainsKey(functionName))
            {
                registeredFunctions.Add(functionName, null);
            }
        }

        #endregion

        #region Function invocation

        private object InvokeFunction(string name, params object[] args)
        {
            return XsltConvertor.PhpToDotNet(_ctx, InvokeFunctionCore(name, args));
        }

        private string InvokeFunctionString(string name, params object[] args)
        {
            return XsltConvertor.PhpToString(_ctx, InvokeFunctionCore(name, args));
        }

        private PhpValue InvokeFunctionCore(string name, params object[] args)
        {
            // check whether this function is allowed to be called
            PhpCallback callback;
            if (allFunctionsRegistered)
            {
                registeredFunctions.TryGetValue(name, out callback);
            }
            else
            {
                if (registeredFunctions.TryGetValue(name, out callback))
                {
                    PhpException.Throw(PhpError.Warning, String.Format(Resources.HandlerNotAllowed, name));
                    return PhpValue.Null;
                }
            }

            // if the callback does not already exist, create it
            if (callback == null)
            {
                callback = PhpCallback.Create(name, default(RuntimeTypeHandle));

                registeredFunctions[name] = callback;
            }

            // convert arguments
            var phpArgs = new PhpValue[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                phpArgs[i] = XsltConvertor.DotNetToPhp(args[i]);
            }

            // invoke!
            return callback.Invoke(_ctx, phpArgs);
        }

        #endregion

        #region function (exposed to XSL)

        public object function(string name)
        {
            return InvokeFunction(name);
        }

        public object function(string name, object arg1)
        {
            return InvokeFunction(name, arg1);
        }

        public object function(string name, object arg1, object arg2)
        {
            return InvokeFunction(name, arg1, arg2);
        }

        public object function(string name, object arg1, object arg2, object arg3)
        {
            return InvokeFunction(name, arg1, arg2, arg3);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16, arg17);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16, arg17, arg18);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18,
            object arg19)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19);
        }

        public object function(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18,
            object arg19, object arg20)
        {
            return InvokeFunction(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20);
        }

        #endregion

        #region functionString (exposed to XSL)

        public object functionString(string name)
        {
            return InvokeFunctionString(name);
        }

        public object functionString(string name, object arg1)
        {
            return InvokeFunctionString(name, arg1);
        }

        public object functionString(string name, object arg1, object arg2)
        {
            return InvokeFunctionString(name, arg1, arg2);
        }

        public object functionString(string name, object arg1, object arg2, object arg3)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16, arg17);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16, arg17, arg18);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18,
            object arg19)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19);
        }

        public object functionString(string name, object arg1, object arg2, object arg3, object arg4,
            object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11,
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18,
            object arg19, object arg20)
        {
            return InvokeFunctionString(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11,
                arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20);
        }

        #endregion
    }
}
