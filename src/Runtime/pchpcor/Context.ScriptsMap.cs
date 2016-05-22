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
        public delegate PhpValue MainDelegate(Context ctx, PhpArray locals, object @this);

        /// <summary>
        /// Script descriptor.
        /// </summary>
        public struct ScriptInfo
        {
            /// <summary>
            /// Undefined script.
            /// </summary>
            public static ScriptInfo Empty => default(ScriptInfo);

            /// <summary>
            /// Whether the script is defined.
            /// </summary>
            public bool IsValid => this.MainMethod != null;

            readonly public int Index;
            readonly public string Path;
            readonly public MainDelegate MainMethod;

            static MainDelegate CreateMain(TypeInfo script)
            {
                var mainmethod =
                    script.GetDeclaredMethod("<Main>`0") ??     // generated wrapper that always returns PhpValue
                    script.GetDeclaredMethod("<Main>");         // if there is no generated wrapper, Main itself returns PhpValue

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

            public static ScriptInfo GetScript<TScript>()
            {
                var idx = EnsureIndex<TScript>(ref IndexHolder<TScript>.Index);
                return _scripts[idx - 1];
            }

            public ScriptInfo GetScript(string path)
            {
                int index;

                lock (_scriptsMap)  // TODO: R lock
                {
                    if (!_scriptsMap.TryGetValue(path, out index))
                        return default(ScriptInfo);
                }

                return _scripts[index];
            }

            /// <summary>
            /// Resolves the script path according to PHP semantics for first defined script.
            /// </summary>
            /// <param name="path">Requested script path, either absolute or relative.</param>
            /// <param name="includePath">Array of defined include paths.</param>
            /// <param name="cd">Current script directory.</param>
            /// <param name="cwd">Current working directory.</param>
            /// <returns>First matching script descriptor.</returns>
            public ScriptInfo GetScript(string path, string[] includePath, string cd, string cwd)
            {
                ScriptInfo result;

                if (!string.IsNullOrEmpty(path))
                {
                    if (path[0] == '.' && path.Length > 2)
                    {
                        if (path[1] == '/' || path[1] == '\\')
                        {
                            // ./
                            result = GetScript(cd + path.Substring(1));
                            if (result.IsValid) return result;
                        }
                        else if (path[1] == '.' && (path[2] == '/' || path[2] == '\\'))
                        {
                            // ../
                            result = GetScript(System.IO.Path.GetDirectoryName(cd) + path.Substring(2));
                            if (result.IsValid) return result;
                        }
                    }

                    // TODO: file://

                    if (includePath != null)
                    {
                        for (int i = 0; i < includePath.Length; i++)
                        {
                            result = GetScript(new Uri(System.IO.Path.Combine(includePath[i], path)).LocalPath);
                            if (result.IsValid) return result;
                        }
                    }

                    if (cd != null)
                    {
                        result = GetScript(System.IO.Path.Combine(cd, path));
                        if (result.IsValid) return result;
                    }

                    if (cwd != null)
                    {
                        result = GetScript(System.IO.Path.Combine(cwd, path));
                        if (result.IsValid) return result;
                    }

                    //
                    result = GetScript(path);
                }
                else
                {
                    result = default(ScriptInfo);
                }

                //
                return result;
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
