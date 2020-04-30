#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Map of all public PHP types defined in loaded assemblies.
    /// </summary>
    sealed class AutoloadClassMap
    {
        readonly Dictionary<string, TypeNode> _typemap = new Dictionary<string, TypeNode>(StringComparer.InvariantCultureIgnoreCase);

        struct TypeNode
        {
            public readonly byte AutoloadFlag;
            object _TypeInfoOrArray; // PhpTypeInfo[] or PhpTypeInfo

            public PhpTypeInfo? SingleTypeInfo() => _TypeInfoOrArray as PhpTypeInfo;

            public TypeNode(byte autoload, PhpTypeInfo tinfo)
            {
                AutoloadFlag = autoload;
                _TypeInfoOrArray = tinfo;
            }

            private TypeNode(byte autoload, PhpTypeInfo[] types)
            {
                AutoloadFlag = autoload;
                _TypeInfoOrArray = types;
            }

            /// <summary>
            /// Gets node representing two or more types.
            /// </summary>
            public TypeNode Combine(byte autoload, PhpTypeInfo tinfo)
            {
                if (tinfo == _TypeInfoOrArray)
                    return this;

                if (_TypeInfoOrArray == null)
                    return new TypeNode(autoload, tinfo);

                if (_TypeInfoOrArray is PhpTypeInfo[] arr)
                {
                    if (Array.IndexOf(arr, tinfo) >= 0) return this;
                    Array.Resize(ref arr, arr.Length + 1);
                    arr[arr.Length - 1] = tinfo;

                    return new TypeNode((byte)(AutoloadFlag | autoload), arr);
                }
                else if (_TypeInfoOrArray is PhpTypeInfo tinfo1)
                {
                    return new TypeNode((byte)(AutoloadFlag | autoload), new[] { tinfo1, tinfo, });
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Stores the type in the map.
        /// </summary>
        public byte AddTypeNoLock(Type t, out PhpTypeInfo? tinfo)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            if (!t.IsPublic)
            {
                tinfo = null;
                return 0;
            }

            // 
            var phptype = t.GetCustomAttribute<PhpTypeAttribute>();
            if (phptype == null ||
                phptype.FileName == null ||
                phptype.FileName.Contains(".phar/")) // CONSIDER: better check for type in phar archive
            {
                tinfo = null;
                return 0;
            }

            tinfo = t.GetPhpTypeInfo();

            if (_typemap.TryGetValue(tinfo.Name, out var node))
            {
                node = node.Combine(phptype.AutoloadFlag, tinfo);
            }
            else
            {
                node = new TypeNode(phptype.AutoloadFlag, tinfo);
            }

            _typemap[tinfo.Name] = node;

            //
            return node.AutoloadFlag;
        }

        public bool TryGetType(string fullName, out PhpTypeInfo? tinfo, out byte autoload)
        {
            if (_typemap.TryGetValue(fullName, out var node))
            {
                autoload = node.AutoloadFlag;
                tinfo = node.SingleTypeInfo();
            }
            else
            {
                autoload = 0;
                tinfo = null;
            }

            return tinfo != null;
        }
    }
}
