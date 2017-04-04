using Pchp.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        /// Helper generic class holding an app static index.
        /// </summary>
        /// <typeparam name="T">Type of object kept as context static.</typeparam>
        static class ScriptIndexHolder<T>
        {
            /// <summary>
            /// Index of the object of type <typeparamref name="T"/>.
            /// </summary>
            public static int Index;
        }

        /// <summary>
        /// Signature of the scripts main method.
        /// </summary>
        /// <param name="ctx">Reference to current context. Cannot be <c>null</c>.</param>
        /// <param name="locals">Reference to variables scope. Cannot be <c>null</c>. Can refer to either globals or new array locals.</param>
        /// <param name="this">Reference to self in case the script is called within an instance method.</param>
        /// <returns>Result of the main method call.</returns>
        public delegate PhpValue MainDelegate(Context ctx, PhpArray locals, object @this);

        /// <summary>
        /// Script descriptor.
        /// </summary>
        [DebuggerDisplay("{Index}: {Path,nq}")]
        public struct ScriptInfo : IScript
        {
            /// <summary>
            /// Undefined script.
            /// </summary>
            public static ScriptInfo Empty => default(ScriptInfo);

            /// <summary>
            /// Compiler generated type containing reflection and entry information.
            /// </summary>
            public const string ScriptTypeName = "<Script>";

            /// <summary>
            /// Whether the script is defined.
            /// </summary>
            public bool IsValid => this.MainMethod != null;

            readonly public int Index;
            readonly public string Path;
            readonly MainDelegate MainMethod;

            static MainDelegate CreateMain(TypeInfo script)
            {
                var mainmethod =
                    script.GetDeclaredMethod("<Main>`0") ??     // generated wrapper that always returns PhpValue
                    script.GetDeclaredMethod("<Main>");         // if there is no generated wrapper, Main itself returns PhpValue

                Debug.Assert(mainmethod != null);
                Debug.Assert(mainmethod.ReturnType == typeof(PhpValue));

                return (MainDelegate)mainmethod.CreateDelegate(typeof(MainDelegate));
            }

            /// <summary>
            /// Runs the script.
            /// </summary>
            public PhpValue Evaluate(Context ctx, PhpArray locals, object @this)
            {
                if (!IsValid) throw new InvalidOperationException();
                return this.MainMethod(ctx, locals, @this);
            }

            /// <summary>
            /// Resolves global function handle(s).
            /// </summary>
            public IEnumerable<MethodInfo> GetGlobalRoutineHandle(string name)
            {
                throw new NotSupportedException();
            }

            public ScriptInfo(int index, string path, TypeInfo script)
            {
                Index = index;
                Path = path;
                MainMethod = CreateMain(script);
            }
        }

        /// <summary>
        /// Manages map of known scripts and bit array of already included.
        /// </summary>
        protected class ScriptsMap
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

            public static string NormalizeSlashes(string path) => path.Replace('\\', '/').Replace("//", "/");

            public void SetIncluded<TScript>() => array.SetTrue(EnsureIndex<TScript>(ref ScriptIndexHolder<TScript>.Index) - 1);

            public bool IsIncluded<TScript>() => IsIncluded(EnsureIndex<TScript>(ref ScriptIndexHolder<TScript>.Index) - 1);

            internal bool IsIncluded(ScriptInfo script) => script.IsValid && IsIncluded(script.Index);

            internal bool IsIncluded(int index) => array[index];

            public static ScriptInfo GetScript<TScript>()
            {
                var idx = EnsureIndex<TScript>(ref ScriptIndexHolder<TScript>.Index);
                return _scripts[idx - 1];
            }

            public static ScriptInfo GetDeclaredScript(string path)
            {
                int index;

                lock (_scriptsMap)  // TODO: R lock
                {
                    if (!_scriptsMap.TryGetValue(path, out index))
                        return default(ScriptInfo);
                }

                return _scripts[index];
            }

            public ScriptInfo GetScript(string path) => GetDeclaredScript(path);

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

                path = NormalizeSlashes(path);

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

            /// <summary>
            /// Searches for a file in the script library, current directory, included paths, and web application root respectively according to PHP semantic.
            /// </summary>
            public static ScriptInfo SearchForIncludedFile(string path, string[] includePath, string cd, Func<string, ScriptInfo> exists)
            {
                Debug.Assert(exists != null);
                Debug.Assert(cd != null);

                if (string.IsNullOrEmpty(path))
                    return default(ScriptInfo);

                if (Path.IsPathRooted(path))
                {
                    if (path[0].IsDirectorySeparator())
                    {
                        // incomplete absolute path //

                        // the path is at least one character long - the first character is slash that should be trimmed out: 
                        path = Path.Combine(Path.GetPathRoot(cd), path.Substring(1));
                    }
                }
                else
                {
                    // relative path //

                    if (path[0] == '.' && path.Length > 2)
                    {
                        if (path[1].IsDirectorySeparator()) // ./
                            return exists(Path.Combine(cd, path.Substring(1)));

                        if (path[1] == '.' && path[2].IsDirectorySeparator())   // ../
                            return exists(Path.GetDirectoryName(cd) + path.Substring(2));
                    }

                    // search in search paths at first (accepts empty path list as well):
                    var result = SearchInSearchPaths(path, includePath, cd, exists);

                    // if the file is found then it exists so we can return immediately:
                    if (result.IsValid)
                    {
                        return result;
                    }
                    else
                    {
                        // if an error message occurred, immediately return
                        //if (errorMessage != null)
                        //    return FullPath.Empty;
                    }

                    // not found => the path is combined with the directory where the script being compiled is stored:
                    path = Path.Combine(cd, path);
                }

                // canonizes the complete absolute path:
                //path = new Uri(path).LocalPath; // Path.GetFullPath(path);

                return exists(path);
            }

            /// <summary>
            /// Searches for an existing file among files which names are combinations of a relative path and one of the paths specified in a list.
            /// </summary>
            static ScriptInfo SearchInSearchPaths(string realtivePath, string[] searchPaths, string cd, Func<string, ScriptInfo>/*!*/exists)
            {
                Debug.Assert(exists != null);

                if (searchPaths != null)
                {
                    for (int i = 0; i < searchPaths.Length; i++)
                    {
                        var path = searchPaths[i];
                        var path_root = Path.GetPathRoot(path);

                        // makes the path complete and absolute:
                        if (path_root.Length == 1 && path_root[0] == PathUtils.DirectorySeparator)
                        {
                            path = Path.Combine(Path.GetPathRoot(cd), path.Substring(1));
                        }
                        else if (path_root.Length == 0)
                        {
                            path = Path.Combine(cd, path);
                        }

                        // combines the search path with the relative path:
                        //path = new Uri(path).LocalPath; // Path.GetFullPath(Path.Combine(path, relativePath));

                        var result = exists(path);
                        if (result.IsValid)
                            return result;
                    }
                }

                //
                return default(ScriptInfo);
            }

            /// <summary>
            /// Gets enumeration of scripts that were included in current context.
            /// </summary>
            public IEnumerable<ScriptInfo> GetIncludedScripts()
            {
                return _scripts.Take(_scriptsMap.Count).Where(IsIncluded);
            }
        }
    }
}
