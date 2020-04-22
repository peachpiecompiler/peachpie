using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }

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
                        License = GetLicense(node.Value);
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
                        Authors = GetAuthors(node.Value.AsArray).ToArray();
                        break;

                    case "require":
                        Dependencies = GetDependencies(node.Value).ToArray();
                        break;

                        // TODO: autoload { files, classmap, psr-0, psr-4 }
                }
            }

            return true;
        }

        string GetName(JSONNode name) => IdToNuGetId(name.Value);

        string GetDescription(JSONNode desc) => desc.Value;

        string GetHomepage(JSONNode url) => new Uri(url.Value, UriKind.Absolute).AbsoluteUri; // validate

        string GetLicense(JSONNode license) => license.Value;

        string SanitizeVersionValue(string value)
        {
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
                Log.LogWarning($"Invalid \"version\" = \"{prefix}\".");
                return null;
            }

            var resultVersionString = v.ToString(3);

            if (suffix.Length != 0)
            {
                resultVersionString = $"{resultVersionString}-{suffix}";
            }

            return resultVersionString;
        }

        DateTime GetReleaseDate(JSONNode date) => DateTime.Parse(date.Value);

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

        /// <summary>
        /// Map of known dependency id's to a corresponding package reference.
        /// </summary>
        static readonly Dictionary<string, string> s_knowndeps_to_packageid = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {"ext-curl", "Peachpie.Library.Network"},
            {"ext-gettext", "Peachpie.Library"},
            {"ext-fileinfo", "Peachpie.Library"},
            {"ext-mbstring", "Peachpie.Library"},
            {"ext-mysql", "Peachpie.Library.MySql"},
            {"ext-mysqli", "Peachpie.Library.MySql"},
            {"ext-mssql", "Peachpie.Library.MsSql"},
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

        IEnumerable<ITaskItem> GetDependencies(JSONNode require)
        {
            if (require == null)
            {
                yield break;
            }

            foreach (var r in require)
            {
                var name = r.Key?.Trim();
                if (string.IsNullOrEmpty(name))
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
                    version: VersionRangeToPackageVersion(r.Value.Value));
            }
        }

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

        string VersionRangeToPackageVersion(string value)
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
            if (Versioning.ComposerVersionExpression.TryParse(value.AsSpan(), out var expression))
            {
                var version = expression.Evaluate();
                return version.ToString();
            }

            // any
            return "*";
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
