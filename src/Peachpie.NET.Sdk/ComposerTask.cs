using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                switch (node.Key)
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
                         * This must follow the format of `X.Y.Z` or `vX.Y.Z`
                         * with an optional suffix of `-dev`, `-patch` (-p), `-alpha` (-a), `-beta` (-b) or `-RC`.
                         * The patch, alpha, beta and RC suffixes can also be followed by a number.
                         */
                        Version = GetVersion(node.Value);
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

        string GetVersion(JSONNode version)
        {
            var value = version.Value;
            if (string.IsNullOrEmpty(value))
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

        IEnumerable<ITaskItem> GetDependencies(JSONNode require)
        {
            if (require == null)
            {
                yield break;
            }

            foreach (var r in require)
            {
                if (r.Key.Equals("php", StringComparison.OrdinalIgnoreCase))
                {
                    // php version,
                    // ignore for now
                    continue;
                }

                if (r.Key.StartsWith("ext-", StringComparison.OrdinalIgnoreCase))
                {
                    // php extension,
                    continue;   // TODO: translate to PeachPie-like library name
                }

                // regular dependency,
                // translate composer-like name to NuGet-like name

                yield return new TaskItem("PackageDependency", new Dictionary<string, string>()
                {
                    { "Name", IdToNuGetId(r.Key) },
                    { "Version", VersionRangeToNuGetVersion(r.Value.Value) },
                });
            }
        }

        string IdToNuGetId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace('/', '.');
        }

        string VersionRangeToNuGetVersion(string value)
        {
            // TODO see https://getcomposer.org/doc/articles/versions.md

            //*
            //>= 1.0
            //>= 1.0 < 2.0
            //>= 1.0 < 1.1 || >= 1.2
            //1.0.*
            //^1.2.3
            //~1.2.3
            //1 - 2

            return value;
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
