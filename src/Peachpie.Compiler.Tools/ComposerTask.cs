using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Peachpie.Compiler.Tools
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
        /// - package name
        /// - version range (optional)
        /// </summary>
        [Output]
        public ITaskItem[] Dependencies { get; private set; }

        /// <summary>
        /// Processes the <c>composer.json</c> file.
        /// </summary>
        public override bool Execute()
        {
            Name = "project name";
            Description = "project description";
            Tags = "composer;tag";
            Homepage = "https://www.example.org/";
            License = "MIT";

            // Log.LogMessage(MessageImportance.High, "ComposerTask finished!");

            return true;
        }
    }
}
