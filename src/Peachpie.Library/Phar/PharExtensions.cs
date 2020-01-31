using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Core.Utilities;

namespace Pchp.Library.Phar
{
    /// <summary>
    /// Helper PHAR methods.
    /// </summary>
    internal static class PharExtensions
    {
        [DebuggerDisplay("{PharFile,nq} ({Scripts.Length} scripts)")]
        sealed class CachedPhar
        {
            /// <summary>
            /// Containing assembly.
            /// </summary>
            public Assembly Assembly { get; }

            /// <summary>
            /// Relative Phar file name.
            /// </summary>
            public string PharFile { get; }

            /// <summary>
            /// Contained script files.
            /// </summary>
            public string[] Scripts { get; }

            /// <summary>
            /// Associated resource manager.
            /// </summary>
            public ResourceManager Resources { get; }

            public CachedPhar(Type stubScriptType)
            {
                Assembly = stubScriptType.Assembly;
                PharFile = GetPharFile(stubScriptType);
                Resources = new ResourceManager($"phar://{PharFile}", Assembly);

                Scripts = EnumeratePharScripts(stubScriptType.Assembly, PharFile)
                    .Select(t =>
                    {
                        var relpath = PharEntryRelativePath(t);
                        Context.DeclareScript(relpath, Context.ScriptInfo.CreateMain(t));
                        return relpath;
                    })
                    .ToArray();
            }
        }

        sealed class PharContext
        {
            /// <summary>
            /// Mapped Phar archives.
            /// </summary>
            public Dictionary<string, CachedPhar> PharMap { get; } = new Dictionary<string, CachedPhar>(CurrentPlatform.PathComparer);
        }

        public static string PharExtension => ".phar";

        /// <summary>
        /// Read-only dictionary of cached phars.
        /// Indexed by PharFile.
        /// </summary>
        static Dictionary<string, CachedPhar> s_cachedPhars; // TODO: ImmutableDictionary

        /// <summary>
        /// Resolves the phar's file name of given script representing phar stub.
        /// </summary>
        public static string GetPharFile(Type stubScriptType)
        {
            var attr = ReflectionUtils.GetScriptAttribute(stubScriptType);
            if (attr != null && attr.Path.EndsWith(PharExtension, StringComparison.OrdinalIgnoreCase))
            {
                return attr.Path;
            }

            return null;
        }

        /// <summary>
        /// Gets relative path to the phar entry.
        /// The returned string is in form <c>{relative phar path}{DS}{phar entry path}</c> (e.g. <c>file.phar\dir\file.php</c>).
        /// </summary>
        public static string PharEntryRelativePath(Type pharEntryScriptType)
        {
            var scriptattr = ReflectionUtils.GetScriptAttribute(pharEntryScriptType) ?? throw new ArgumentException();
            return scriptattr.Path;
        }

        /// <summary>
        /// Gets relative path to the phar entry.
        /// The returned string is in form <c>{relative phar path}{DS}{phar entry path}</c> (e.g. <c>file.phar/dir/file.php</c>).
        /// </summary>
        public static string PharEntryRelativePath(string pharFile, string pharEntryPath)
        {
            return CurrentPlatform.NormalizeSlashes(pharFile + CurrentPlatform.DirectorySeparator.ToString() + pharEntryPath);
        }

        /// <summary>
        /// Gets <see cref="PharAttribute"/> of given script type (the type that represents a compiled script file).
        /// </summary>
        /// <returns>The attribute or <c>null</c>.</returns>
        public static PharAttribute GetPharAttribute(Type pharEntryScriptType)
        {
            var attrs = pharEntryScriptType.GetCustomAttributes(typeof(PharAttribute), inherit: false) as Attribute[]; // faster
            return attrs != null && attrs.Length != 0
                ? (PharAttribute)attrs[0]
                : null;
        }

        static CachedPhar EnsureCachedPhar(Type stubScriptType)
        {
            CachedPhar cached = null;

            var pharFile = GetPharFile(stubScriptType);
            if (pharFile != null)
            {
                var cachedPhars = s_cachedPhars;
                if (cachedPhars == null || !cachedPhars.TryGetValue(pharFile, out cached))
                {
                    cached = new CachedPhar(stubScriptType);

                    var newdict = cachedPhars != null
                        ? new Dictionary<string, CachedPhar>(cachedPhars, CurrentPlatform.PathComparer)
                        : new Dictionary<string, CachedPhar>(CurrentPlatform.PathComparer);

                    newdict[pharFile] = cached;
                    s_cachedPhars = newdict;
                }
            }

            return cached;
        }

        /// <summary>
        /// Ensures phar scripts are declared within <see cref="Context"/> and registers phar alias
        /// </summary>
        public static bool MapPhar(Context ctx, Type stubScriptType, string alias)
        {
            var phar = EnsureCachedPhar(stubScriptType);
            if (phar != null)
            {
                if (alias != null)
                {
                    ctx.GetStatic<PharContext>().PharMap.Add(alias, phar);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Resolves Phar Alias to a PharFile (relative .phar file path).
        /// </summary>
        /// <returns>Relative .phar file path or <c>null</c>.</returns>
        public static string AliasToPharFile(Context ctx, string alias)
        {
            return ctx.GetStatic<PharContext>().PharMap.TryGetValue(alias, out var phar)
                ? phar.PharFile
                : null;
        }

        /// <summary>
        /// Gets phar content file stream.
        /// </summary>
        public static Stream GetResourceStream(string pharFile, string entryName)
        {
            if (s_cachedPhars != null && s_cachedPhars.TryGetValue(pharFile, out var phar) && phar.Resources != null)
            {
                // TODO: the resource should be embedded as Stream
                var content = phar.Resources.GetString(entryName.Replace('\\', '/'));
                return new MemoryStream(Encoding.UTF8.GetBytes(content));
            }

            return null;
        }

        /// <summary>
        /// Enumerates all compiled scripts that represent a PHAR entry.
        /// </summary>
        static IEnumerable<Type> EnumeratePharScripts(Assembly assembly, string pharFileName)
        {
            return assembly
                .GetTypes()
                .Where(t => t.IsAbstract && t.IsSealed && !t.IsValueType && t.IsPublic)
                .Where(t => string.Equals(GetPharAttribute(t)?.PharFile, pharFileName, CurrentPlatform.PathStringComparison));
        }
    }
}
