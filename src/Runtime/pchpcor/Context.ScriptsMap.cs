using Pchp.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Signature of the scripts main method.
        /// </summary>
        /// <param name="ctx">Reference to current context. Cannot be <c>null</c>.</param>
        /// <param name="locals">Reference to variables scope. Cannot be <c>null</c>. Can refer to either globals or new array locals.</param>
        /// <returns>Result of the main method call.</returns>
        public delegate PhpValue MainDelegate(Context ctx, PhpArray locals);

        /// <summary>
        /// Script descriptor.
        /// </summary>
        public struct ScriptInfo
        {
            readonly public int Index;
            readonly public string Path;
            readonly public MainDelegate MainMethod;
            
            static MainDelegate CreateMain(TypeInfo script)
            {
                var mainmethod =
                    script.GetDeclaredMethod("<Main>@PhpValue") ?? 
                    script.GetDeclaredMethod("<Main>");

                Debug.Assert(mainmethod != null);                
                Debug.Assert(mainmethod.ReturnType == typeof(PhpValue));

                return (MainDelegate)mainmethod.CreateDelegate(typeof(MainDelegate));
            }

            internal ScriptInfo(int index, string path, TypeInfo script)
            {
                Index = index;
                Path = path;
                MainMethod = CreateMain(script);
            }
        }

        /// <summary>
        /// Manages map of known scripts and bit array of already included.
        /// </summary>
        class ScriptsMap
        {
            readonly ElasticBitArray array = new ElasticBitArray(_scriptsMap.Count);
            
            /// <summary>
            /// Maps script paths to their id.
            /// </summary>
            static Dictionary<string, int> _scriptsMap = new Dictionary<string, int>(64, StringComparer.OrdinalIgnoreCase);  // TODO: Ordinal comparer on Unix

            /// <summary>
            /// Scripts descriptors corresponding to id.
            /// </summary>
            static ScriptInfo[] _scripts = new ScriptInfo[64];

            static void DeclareScript(int index, string path, TypeInfo script)
            {
                // TODO: RW lock

                if (index >= _scripts.Length)
                {
                    Array.Resize(ref _scripts, index * 2 + 1);
                }

                _scripts[index] = new ScriptInfo(index, path, script);
            }

            internal static void DeclareScript(string path, RuntimeMethodHandle mainmethodHandle)
            {
                var mainmethod = MethodBase.GetMethodFromHandle(mainmethodHandle);

                GetScriptIndex(path, mainmethod.DeclaringType.GetTypeInfo());
            }

            public void SetIncluded<TScript>() => array.SetTrue(EnsureIndex<TScript>(ref IndexHolder<TScript>.Index) - 1);

            public bool IsIncluded<TScript>() => IsIncluded(EnsureIndex<TScript>(ref IndexHolder<TScript>.Index) - 1);

            internal bool IsIncluded(int index) => array[index];

            public ScriptInfo GetScript(string path)
            {
                int index;

                lock (_scriptsMap)  // TODO: RW lock
                {
                    if (!_scriptsMap.TryGetValue(path, out index))
                        return default(ScriptInfo);
                }

                return _scripts[index];
            }

            static int EnsureIndex<TScript>(ref int script_id)
            {
                if (script_id == 0)
                {
                    script_id = GetScriptIndex(typeof(TScript).GetTypeInfo()) + 1;
                }

                return script_id;
            }

            static int GetScriptIndex(TypeInfo script)
            {
                var attr = script.GetCustomAttribute<ScriptAttribute>();
                Debug.Assert(attr != null);

                var path = (attr != null) ? attr.Path : $"?{_scriptsMap.Count}";

                return GetScriptIndex(path, script);
            }

            static int GetScriptIndex(string path, TypeInfo script)
            {
                int index;

                lock (_scriptsMap)  // TODO: RW lock
                {
                    if (!_scriptsMap.TryGetValue(path, out index))
                    {
                        index = _scriptsMap.Count;
                        DeclareScript(index, path, script);

                        _scriptsMap[path] = index;
                    }
                }

                return index;
            }
        }
    }
}
