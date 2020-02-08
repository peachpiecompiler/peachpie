#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Peachpie.Library.XmlDom
{
    /// <summary>
    /// Represents an XML/HTML document.
    /// Extends functionality of <see cref="XmlDocument"/>.
    /// </summary>
    sealed class PhpXmlDocument : XmlDocument
    {
        /// <summary>
        /// Creates an instance of <see cref="XmlDocument"/>.
        /// </summary>
        /// <returns></returns>
        public static XmlDocument Create()
        {
            return new PhpXmlDocument()
            {
                PreserveWhitespace = true,
            };
        }

        /// <summary>
        /// Map of elements with <c>id</c> attribute.
        /// </summary>
        Dictionary<string, List<WeakReference<XmlAttribute>>>? _elementIdMap;

        private PhpXmlDocument()
        {
        }

        /// <summary>
        /// Prepares the element map.
        /// </summary>
        internal void HandleNewAttribute(XmlAttribute attr)
        {
            if (string.Equals(attr.LocalName, "id", StringComparison.OrdinalIgnoreCase))
            {
                if (_elementIdMap == null)
                {
                    _elementIdMap = new Dictionary<string, List<WeakReference<XmlAttribute>>>(StringComparer.OrdinalIgnoreCase);
                }

                if (_elementIdMap.TryGetValue(attr.Value, out var list) == false)
                {
                    _elementIdMap[attr.Value] = list = new List<WeakReference<XmlAttribute>>(1);
                }

                //
                list.Add(new WeakReference<XmlAttribute>(attr));
            }
        }

        public override XmlElement GetElementById(string elementId)
        {
            if (_elementIdMap != null && _elementIdMap.TryGetValue(elementId, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].TryGetTarget(out var attr))
                    {
                        if (attr.OwnerElement != null)
                        {
                            // TODO: check element is connected
                            return attr.OwnerElement;
                        }
                    }
                }
            }

            //
            return base.GetElementById(elementId);
        }
    }
}
