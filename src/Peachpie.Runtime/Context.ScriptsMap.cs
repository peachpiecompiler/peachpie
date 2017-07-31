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
        /// <param name="self">Reference to current class context.</param>
        /// <returns>Result of the main method call.</returns>
        public delegate PhpValue MainDelegate(Context ctx, PhpArray locals, object @this, RuntimeTypeHandle self);

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
            public PhpValue Evaluate(Context ctx, PhpArray locals, object @this, RuntimeTypeHandle self = default(RuntimeTypeHandle))
            {
                if (!IsValid) throw new InvalidOperationException();
                return this.MainMethod(ctx, locals, @this, self);
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
                Path = CurrentPlatform.NormalizeSlashes(path);
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
            static Dictionary<string, int> _scriptsMap = new Dictionary<string, int>(64, CurrentPlatform.PathComparer);

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

            static string NormalizeSlashes(string path) => CurrentPlatform.NormalizeSlashes(path);

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
                // trim leading slash
                if (path[0].IsDirectorySeparator())
                {
                    path = path.Substring(1);
                }

                //
                int index;
                lock (_scriptsMap)  // TODO: R lock
                {
                    if (!_scriptsMap.TryGetValue(NormalizeSlashes(path), out index))
                    {
                        return default(ScriptInfo);
                    }
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
            /// Resolves script corresponding to given parameters.
            /// </summary>
            /// <param name="path">Requested script path, absolute or relative in PHP manner.</param>
            /// <param name="root_path">The application root path. All scripts are relative to this directory.</param>
            /// <param name="include_path">List of include paths (absolute or relative) to search in. Can be <c>null</c>.</param>
            /// <param name="working_dir">Current working directory to search in after <paramref name="include_path"/>. Also <c>.</c> and <c>..</c> are relative to this directory.</param>
            /// <param name="script_dir">The directory of the currently executing script. Normalized. Can be <c>null</c>. Is relative to <paramref name="root_path"/>.</param>
            /// <returns>Script descriptor or empty an invalid script in case path inclusion cannot be resolved.</returns>
            public static ScriptInfo ResolveInclude(string path, string root_path, string[] include_path, string working_dir, string script_dir)
            {
                // check arguments
                Debug.Assert(root_path != null);

                ScriptInfo script = default(ScriptInfo);

                if (string.IsNullOrEmpty(path))
                {
                    return script;
                }

                // 0. rooted path -> resolved
                if (Path.IsPathRooted(path))
                {
                    // normalize, check it is within root_path
                    path = NormalizeSlashes(Path.GetFullPath(path));
                    if (path.StartsWith(root_path))
                    {
                        script = GetDeclaredScript(path.Substring(root_path.Length + 1));
                        // TODO: script may be not loaded yet but exists physically, check it exists and compile
                    }

                    return script;
                }

                // 1. ".." or "." are always relative to working_dir -> resolved
                if (path[0] == '.' && path.Length >= 2)
                {
                    // ./
                    if (PathUtils.IsDirectorySeparator(path[1]))
                    {
                        return ResolveRelativeScript(working_dir, path.Substring(2), root_path, working_dir);
                    }
                    // ../
                    if (path[1] == '.' && path.Length >= 3 && PathUtils.IsDirectorySeparator(path[2]))
                    {
                        return ResolveRelativeScript(Path.GetDirectoryName(working_dir), path.Substring(3), root_path, working_dir);
                    }
                }

                // 2. repeat for combinations with include_path
                if (include_path != null)
                {
                    for (int i = 0; i < include_path.Length; i++)
                    {
                        script = ResolveRelativeScript(include_path[i], path, root_path, working_dir);
                        if (script.IsValid) return script;
                    }
                }

                // 3. working_dir
                script = ResolveRelativeScript(working_dir, path, root_path, working_dir);
                if (script.IsValid) return script;

                // 4. script_dir
                if (script_dir != null)
                {
                    script = ResolveInclude(Path.Combine(root_path, script_dir, path), root_path, null, null, null);
                    if (script.IsValid) return script;
                }

                //
                return script;
            }

            static ScriptInfo ResolveRelativeScript(string path1, string path2, string root_path, string working_dir)
            {
                return string.IsNullOrEmpty(path1)
                    ? default(ScriptInfo)
                    : ResolveInclude(Path.Combine(path1, path2), root_path, null, working_dir, null);
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
