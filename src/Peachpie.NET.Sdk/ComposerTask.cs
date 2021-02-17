using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Devsense.PHP.Syntax;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SimpleJSON;

namespace Peachpie.NET.Sdk.Tools
{
    /// <summary>
    /// Task that processes <c>composer.json</c> file if exists and outputs its properties and dependencies.
    /// </summary>
    public class ComposerTask : Task
    {
        /// <summary>
        /// Path to <c>composer.json</c> file.
        /// </summary>
        [Required]
        public string ComposerJsonPath { get; set; }

        /// <summary>
        /// Specified version suffix.
        /// </summary>
        public string VersionSuffix { get; set; }

        /// <summary>
        /// If set, <c>"require-dev"</c> composer packages are included into <see cref="Dependencies"/>.
        /// </summary>
        public bool ComposerIncludeDevPackages { get; set; }

        /// <summary>
        /// If set, <c>"suggest"</c> composer packages are included into <see cref="Dependencies"/>.
        /// </summary>
        /// <remarks>
        /// Suggest packages are only informative, does not make much sense to reference those Ids directly.
        /// </remarks>
        public bool ComposerIncludeSuggestPackages { get; set; }

        /// <summary>
        /// Outputs name of the project if specified.
        /// </summary>
        [Output]
        public string Name { get; private set; }

        /// <summary>
        /// Outputs version of the project if specified.
        /// </summary>
        [Output]
        public string Version { get; private set; }

        /// <summary>
        /// Outputs release date of the project if specified.
        /// </summary>
        [Output]
        public DateTime ReleaseDate { get; private set; }

        /// <summary>
        /// Outputs description of the project if specified.
        /// </summary>
        [Output]
        public string Description { get; private set; }

        /// <summary>
        /// Outputs semi-colon separated list of keywords if specified.
        /// </summary>
        [Output]
        public string Tags { get; private set; }

        /// <summary>
        /// Outputs homepage URL of the project if specified.
        /// </summary>
        [Output]
        public string Homepage { get; private set; }

        /// <summary>
        /// Outputs the package license.
        /// Can be a combination of licenses separated with semi-colon.
        /// </summary>
        /// <remarks>
        /// See SPDX https://spdx.org/licenses/ for details. Common values are:
        /// - Apache-2.0
        /// - BSD-2-Clause
        /// - BSD-3-Clause
        /// - BSD-4-Clause
        /// - GPL-2.0-only / GPL-2.0-or-later
        /// - GPL-3.0-only / GPL-3.0-or-later
        /// - LGPL-2.1-only / LGPL-2.1-or-later
        /// - LGPL-3.0-only / LGPL-3.0-or-later
        /// - MIT
        /// </remarks>
        [Output]
        public string License { get; private set; }

        /// <summary>
        /// Outputs list of authors if specified.
        /// Author item has following properties:
        /// - name: The author's name. Usually their real name.
        /// - email: The author's email address.
        /// - homepage: An URL to the author's website.
        /// - role: The author's role in the project (e.g. developer or translator)
        /// </summary>
        [Output]
        public ITaskItem[] Authors { get; private set; }

        /// <summary>
        /// Outputs list of dependencies.
        /// The item has following properties:
        /// - Name: package name
        /// - Version: version range (optional)
        /// </summary>
        [Output]
        public ITaskItem[] Dependencies { get; private set; }

        /// <summary>
        /// Autoload patterns according to PSR-4 (PSR-0 is converted to PSR-4).<br/>
        /// - Prefix: full class name prefix<br/>
        /// - Path: path where to look for the class implementation (without the prefixed namespace)<br/>
        /// </summary>
        [Output]
        public ITaskItem[] Autoload_PSR4 { get; private set; }

        /// <summary>
        /// Autoload class map directories.
        /// </summary>
        [Output]
        public ITaskItem[] Autoload_ClassMap { get; private set; }

        /// <summary>
        /// Autoload class map directories to be excluded.
        /// Supports wildcard patterns.
        /// </summary>
        [Output]
        public ITaskItem[] Autoload_ClassMap_Exclude { get; private set; }

        /// <summary>
        /// Files to be autoloaded.
        /// Supports wildcard patterns.
        /// </summary>
        [Output]
        public ITaskItem[] Autoload_Files { get; private set; }

        enum ErrorCodes
        {
            ERR_SyntaxError = 1000,
            WRN_EmptyDescription,
            WRN_InvalidHomepage,
            WRN_EmptyName,
            WRN_Unhandled,
            WRN_SPDX,
            WRN_InvalidVersion,
            WRN_InvalidReleaseDate,
            WRN_InvalidDependencyVersion,
        }

        static string ErrorCode(ErrorCodes code) => "CSDK" + ((int)code).ToString("D4");

        void LogError(ErrorCodes code, Location location, string message)
        {
            var path = Path.GetFullPath(ComposerJsonPath); // normalize slashes

            if (code.ToString().StartsWith("ERR_")) // error
            {
                Log.LogError(null, ErrorCode(code), null, path, location.Start.Line, location.Start.Col, location.End.Line, location.End.Col, message);
            }
            else
            {
                Log.LogWarning(null, ErrorCode(code), null, path, location.Start.Line, location.Start.Col, location.End.Line, location.End.Col, message);
            }
        }

        /// <summary>
        /// Processes the <c>composer.json</c> file.
        /// </summary>
        public override bool Execute()
        {
            // parse the input JSON file:
            JSONNode json;
            try
            {
                json = JSON.Parse(File.ReadAllText(ComposerJsonPath));
            }
            catch (JSONFormatException f)
            {
                LogError(ErrorCodes.ERR_SyntaxError, new Location { Start = f.Position }, f.Message);
                return false;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }

            // cleanup output properties
            Authors = Array.Empty<ITaskItem>();
            Dependencies = Array.Empty<ITaskItem>();
            Autoload_ClassMap = Array.Empty<ITaskItem>();
            Autoload_ClassMap_Exclude = Array.Empty<ITaskItem>();
            Autoload_PSR4 = Array.Empty<ITaskItem>();
            Autoload_Files = Array.Empty<ITaskItem>();

            // process the file:
            foreach (var node in json)
            {
                switch (node.Key.ToLowerInvariant())
                {
                    case "name":
                        Name = GetName(node.Value);
                        break;

                    case "description":
                        Description = GetDescription(node.Value);
                        break;

                    case "keywords":
                        Tags = GetTags(node.Value);
                        break;

                    case "homepage":
                        Homepage = GetHomepage(node.Value);
                        break;

                    case "license":
                        License = SpdxHelpers.SanitizeSpdx(node.Value, out var spdxwarning);
                        if (spdxwarning != null)
                        {
                            LogError(ErrorCodes.WRN_SPDX, node.Value.Location, spdxwarning);
                        }
                        break;

                    case "version":
                        /*
                         * Note, in general it is recommended to omit this tag and infer the value from a build process.
                         * Anyways, if it's there we should respect it.
                         */

                        /*
                         * This must follow the format of `X.Y.Z` or `vX.Y.Z`
                         * with an optional suffix of `-dev`, `-patch` (-p), `-alpha` (-a), `-beta` (-b) or `-RC`.
                         * The patch, alpha, beta and RC suffixes can also be followed by a number.
                         */
                        Version = SanitizeVersionValue(node.Value.Value);
                        break;

                    case "time":
                        ReleaseDate = GetReleaseDate(node.Value);
                        break;

                    case "authors":
                        Authors = Authors.Concat(GetAuthors(node.Value.AsArray)).AsArray();
                        break;

                    case "require":
                        // { "name": "version constraint", }
                        Dependencies = Dependencies.Concat(GetDependencies(node.Value)).AsArray();
                        break;

                    case "require-dev":
                        if (ComposerIncludeDevPackages)
                        {
                            // { "name": "version constraint", }
                            Dependencies = Dependencies.Concat(GetDependencies(node.Value)).AsArray();
                        }
                        break;

                    case "suggest":
                        if (ComposerIncludeSuggestPackages)
                        {
                            // CONSIDER: Suggest packages are only informative, does not make much sense to reference those "names" directly.

                            // { "name": "description", }
                            Dependencies = Dependencies.Concat(GetDependencies(node.Value, ignoreVersion: true)).AsArray();
                        }
                        break;

                    case "autoload":
                        // "autoload" : { "psr-0", "psr-4", "classmap", "exclude-from-classmap", "files" }
                        foreach (var autoload in node.Value)
                        {
                            switch (autoload.Key.ToLowerInvariant())
                            {
                                case "psr-4":
                                    Autoload_PSR4 = Autoload_PSR4.Concat(GetAutoloadPsr4FromPsr4(autoload.Value.AsObject)).AsArray();
                                    break;

                                case "psr-0":
                                    Autoload_PSR4 = Autoload_PSR4.Concat(GetAutoloadPsr4FromPsr0(autoload.Value.AsObject)).AsArray();
                                    break;

                                case "classmap":
                                    Autoload_ClassMap = Autoload_ClassMap.Concat(
                                        GetAutoloadClassMapString(autoload.Value.AsArray, false).Select(path => new TaskItem(path))).ToArray();
                                    break;

                                case "exclude-from-classmap":
                                    Autoload_ClassMap_Exclude = Autoload_ClassMap_Exclude.Concat(
                                        GetAutoloadClassMapString(autoload.Value.AsArray, true).Select(path => new TaskItem(path))).ToArray();
                                    break;

                                case "files":
                                    if (autoload.Value is JSONArray files_array)
                                    {
                                        Autoload_Files = Autoload_Files.Concat(
                                         files_array
                                            .Select(node => node.Value?.Trim())
                                            .Where(str => !string.IsNullOrEmpty(str))
                                            .Select(path => new TaskItem(path))).ToArray();
                                    }
                                    break;

                                default:
                                    // ???
                                    LogError(ErrorCodes.WRN_Unhandled, node.Value.Location, $"autoload \"{autoload.Key}\" key is not handled.");
                                    break;
                            }
                        }

                        break;
                }
            }

            return true;
        }

        string GetName(JSONNode name)
        {
            if (string.IsNullOrWhiteSpace(name.Value))
            {
                LogError(ErrorCodes.WRN_EmptyName, name.Location, "Name should not be empty.");
            }

            return IdToNuGetId(name.Value);
        }

        string GetDescription(JSONNode desc)
        {
            // some packages have a single whitespace as "description"
            // causing nuget task error (no error code)
            // trim the value.

            var description = desc.Value.Trim();

            if (description.Length == 0)
            {
                LogError(ErrorCodes.WRN_EmptyDescription, desc.Location, "Description should not be empty.");

                // CONSIDER: fill in something instead of `"Package Description"` filled in by nuget task
            }

            return description;
        }

        string GetHomepage(JSONNode url)
        {
            if (Uri.TryCreate(url.Value, UriKind.Absolute, out var uri))
            {
                return uri.AbsoluteUri;
            }
            else
            {
                LogError(ErrorCodes.WRN_InvalidHomepage, url.Location, "Invalid URI.");
                return string.Empty;
            }
        }

        string SanitizeVersionValue(JSONNode node)
        {
            var value = node.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value[0] == 'v')
            {
                // vX.Y.Z-PreRelease
                value = value.Substring(1);
            }

            var dash = value.IndexOf('-');
            var prefix = dash < 0 ? value : value.Remove(dash);
            var suffix = !string.IsNullOrEmpty(VersionSuffix) ? VersionSuffix : (dash < 0 ? "" : value.Substring(dash + 1));

            // prefix must be in form of X.Y.Z
            if (!System.Version.TryParse(prefix, out var v))
            {
                LogError(ErrorCodes.WRN_InvalidVersion, node.Location, $"Invalid \"version\" = \"{prefix}\".");
                return null;
            }

            var resultVersionString = v.Build >= 0
                ? v.ToString(3)
                : (v.ToString(2) + ".0");

            if (suffix.Length != 0)
            {
                resultVersionString = $"{resultVersionString}-{suffix}";
            }

            return resultVersionString;
        }

        DateTime GetReleaseDate(JSONNode date)
        {
            if (DateTime.TryParse(date.Value, out var dt))
            {
                return dt;
            }
            else
            {
                LogError(ErrorCodes.WRN_InvalidReleaseDate, date.Location, "Invalid date format.");
                return default;
            }
        }

        IEnumerable<ITaskItem> GetAuthors(JSONArray authors)
        {
            if (authors == null || authors.Count == 0)
            {
                yield break;
            }

            foreach (var author in authors.Values)
            {
                yield return new TaskItem("Author", new Dictionary<string, string>()
                {
                    { "Name", author["name"].Value },
                    { "EMail", author["email"].Value },
                });
            }
        }

        static ITaskItem AutoloadPSR4Item(string prefix, string path)
        {
            return new TaskItem("psr-4", new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Prefix", prefix },
                { "Path", path },
            });
        }

        IEnumerable<ITaskItem> GetAutoloadPsr4FromPsr4(JSONObject map)
        {
            if (map == null)
            {
                Log.LogWarning($"autoload \"psr-4\" is not a JSON object.");
                yield break;
            }

            foreach (var pair in map)
            {
                if (pair.Value is JSONArray patharray)
                {
                    // "Monolog\\" : ["src/", "lib/"]
                    foreach (var path in patharray)
                    {
                        if (path.Value is JSONString pathstring)
                        {
                            yield return AutoloadPSR4Item(pair.Key, pathstring.Value.Trim());
                        }
                    }
                }
                else if (pair.Value is JSONString pathstring)
                {
                    // "Monolog\\" : "src/"
                    yield return AutoloadPSR4Item(pair.Key, pathstring.Value.Trim());
                }
            }
        }

        IEnumerable<ITaskItem> GetAutoloadPsr4FromPsr0(JSONObject map)
        {
            if (map == null)
            {
                Log.LogWarning($"autoload \"psr-0\" is not a JSON object.");
                yield break;
            }

            foreach (var pair in map)
            {
                // class name prefix
                var prefix = pair.Key;

                // get the namespace part of the class name
                // psr-0 -> psr-4

                var slash = prefix.LastIndexOf('\\');
                var ns = slash > 0 ? prefix.Remove(slash) : string.Empty;

                //
                if (pair.Value is JSONArray patharray)
                {
                    // "Monolog\\" : ["src/", "lib/"]
                    foreach (var path in patharray)
                    {
                        if (path.Value is JSONString pathstring)
                        {
                            yield return AutoloadPSR4Item(prefix, pathstring.Value.Trim() + ns);
                        }
                    }
                }
                else if (pair.Value is JSONString pathstring)
                {
                    // "Monolog\\" : "src/"
                    yield return AutoloadPSR4Item(prefix, pathstring.Value.Trim() + ns);
                }
            }
        }

        IEnumerable<string> GetAutoloadClassMapString(JSONArray paths, bool isExclude)
        {
            // gets path suffixed with slash in case it is directory.
            string NormalizePath(string pattern, out bool isDirectory)
            {
                pattern = pattern.Trim().TrimStart('\\', '/', ' ');

                if (pattern.Length == 0)
                {
                    isDirectory = true;
                    return "";
                }

                if (pattern[pattern.Length - 1] == '/' ||
                    pattern[pattern.Length - 1] == '\\')
                {
                    isDirectory = true;
                    return pattern;
                }

                if (string.IsNullOrEmpty(Path.GetExtension(pattern)))
                {
                    isDirectory = true;
                    return pattern + "/";
                }

                //
                isDirectory = false;
                return pattern;
            }

            if (paths != null)
            {
                foreach (var pair in paths)
                {
                    var path = pair.Value.Value;
                    if (path.Length == 0) continue;

                    var pattern = NormalizePath(path, out var isDirectory);
                    if (isDirectory)
                    {
                        if (isExclude)
                        {
                            // '**' is implicitly added to the end of the paths.
                            yield return pattern + "**";
                        }
                        else
                        {
                            // all .php and .inc files in the given directories/files.
                            yield return pattern + "**/*.php";
                            yield return pattern + "**/*.inc";
                        }
                    }
                    else
                    {
                        yield return pattern;
                    }
                }
            }
        }

        static string ResolvePeachpieSdkVersion()
        {
            foreach (var inf in typeof(ComposerTask).Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>())
            {
                var version = inf.InformationalVersion;
                if (string.IsNullOrEmpty(version))
                {
                    continue;
                }

                // remove metadata
                int meta = version.IndexOf('+');
                if (meta >= 0)
                {
                    version = version.Remove(meta);
                }

                //
                return version;
            }

            //
            return typeof(ComposerTask).Assembly.GetName().Version.ToString(3);
        }

        /// <summary>Current version of Peachpie build. Used for referenced libraries</summary>
        static string PeachpieSdkVersion => _lazyPeachpieSdkVersion ??= ResolvePeachpieSdkVersion();
        static string _lazyPeachpieSdkVersion;

        const string Package_PeachpieLibrary = "Peachpie.Library";

        /// <summary>
        /// Map of known dependency id's to a corresponding package reference.
        /// </summary>
        static readonly Dictionary<string, string> s_knowndeps_to_packageid = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {"ext-curl", "Peachpie.Library.Network"},
            {"ext-sockets", "Peachpie.Library.Network"},
            {"ext-gettext", Package_PeachpieLibrary},
            {"ext-fileinfo", Package_PeachpieLibrary},
            {"ext-mbstring", Package_PeachpieLibrary},
            {"ext-json", Package_PeachpieLibrary},
            {"ext-mysql", "Peachpie.Library.MySql"},
            {"ext-mysqli", "Peachpie.Library.MySql"},
            {"ext-mssql", "Peachpie.Library.MsSql"},
            {"ext-mongodb", "Peachpie.Library.MongoDB"},// DOES NOT EXIST!
            {"ext-sqlite3", "Peachpie.Library.Sqlite"}, // DOES NOT EXIST!
            {"ext-ast", "Peachpie.Library.Ast"},        // DOES NOT EXIST!
            {"ext-dom", "Peachpie.Library.XmlDom"},
            {"ext-xsl", "Peachpie.Library.XmlDom"},
            {"ext-exif", "Peachpie.Library.Graphics"},
            {"ext-gd2", "Peachpie.Library.Graphics"},
            {"ext-pdo", "Peachpie.Library.PDO"},
            {"ext-pdo-sqlite", "Peachpie.Library.PDO.Sqlite"},
            {"ext-pdo-firebird", "Peachpie.Library.PDO.Firebird"},
            {"ext-pdo-ibm", "Peachpie.Library.PDO.IBM"},
            {"ext-pdo-mysql", "Peachpie.Library.PDO.MySQL"},
            {"ext-pdo-pgsql", "Peachpie.Library.PDO.PgSQL"},
            {"ext-pdo-sqlsrv", "Peachpie.Library.PDO.SqlSrv"},
        };

        static TaskItem PackageDependencyItem(string name, string version)
        {
            return new TaskItem("PackageDependency", new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Name", name },
                { "Version", version },
            });
        }

        IEnumerable<ITaskItem> GetDependencies(JSONNode require, bool ignoreVersion = false)
        {
            if (require == null)
            {
                yield break;
            }

            foreach (var r in require)
            {
                var name = r.Key.Trim();

                if (name.Length == 0)
                {
                    // invalid requirement name:
                    continue;
                }

                if (name.Equals("php", StringComparison.OrdinalIgnoreCase) || name.StartsWith("php-", StringComparison.OrdinalIgnoreCase))
                {
                    // php version,
                    // ignore for now
                    continue;
                }

                if (s_knowndeps_to_packageid.TryGetValue(name, out var packageid))
                {
                    yield return PackageDependencyItem(
                        name: packageid,
                        version: PeachpieSdkVersion);
                }

                if (name.StartsWith("ext-", StringComparison.OrdinalIgnoreCase))
                {
                    // php extension name
                    // ignore unknown for now
                    continue;
                }

                if (name.StartsWith("lib-", StringComparison.OrdinalIgnoreCase))
                {
                    // internal library restriction
                    // ignored
                    continue;
                }

                // regular dependency,
                // translate composer-like name to NuGet-like name

                yield return PackageDependencyItem(
                    name: IdToNuGetId(name),
                    version: VersionRangeToPackageVersion(ignoreVersion ? string.Empty : r.Value.Value, ForcePreReleaseDependency, r.Value.Location));
            }
        }

        bool ForcePreReleaseDependency => !string.IsNullOrEmpty(VersionSuffix);

        /// <summary>
        /// Convert composer-like name to NugetId-like name.
        /// </summary>
        /// <param name="value">Composer requirement name.</param>
        /// <returns></returns>
        public static string IdToNuGetId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(nameof(value));
            }

            // <vendor>/<name> => <vendor>.<name>
            // replace vendor separator with dot
            // '/' is not allows in file names and nuget id's
            value = value.Replace('/', '.');

            // ids are case insensitive
            return value.ToLowerInvariant();
        }

        string VersionRangeToPackageVersion(string value, bool forcePreRelease, Location location)
        {
            // https://getcomposer.org/doc/articles/versions.md
            // https://docs.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#floating-versions

            // convert composer version constraint to a floating version,
            // note, not all the constraints can be converted to a corresponding floating version.

            //>= 1.0
            //>= 1.0 <2.0
            //>= 1.0 <1.1 || >= 1.2
            //1.0.*
            //^1.2.3
            //~1.2.3
            //1 - 2
            //1.0.0 - 2.1.0

            Versioning.FloatingVersion version;

            if (Versioning.ComposerVersionExpression.TryParse(value, out var expression))
            {
                version = expression.Evaluate();
            }
            else
            {
                LogError(ErrorCodes.WRN_InvalidDependencyVersion, location, $"Version expression '{value}' is invalid.");
                version = new Versioning.FloatingVersion(); // any version
            }

            //
            return version.ToString(forcePreRelease: forcePreRelease);
        }

        string GetTags(JSONNode keywords)
        {
            var arr = keywords.AsArray;
            if (arr != null && arr.Count != 0)
            {
                return string.Join(";", arr.Children.Select(n => n.Value));
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
