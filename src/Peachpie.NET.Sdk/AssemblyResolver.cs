using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Peachpie.NET.Sdk.Tools
{
    sealed class AssemblyResolver
    {
        /// <summary>
        /// Resolve assembly.
        /// </summary>
        static ResolveEventHandler s_AssemblyResolve = new ResolveEventHandler(AssemblyResolve);

        /// <summary>
        /// Build task directory containing our assemblies.
        /// </summary>
        static readonly string s_path = Path.GetDirectoryName(Path.GetFullPath(typeof(AssemblyResolver).Assembly.Location));

        public static void InitializeSafe()
        {
            try
            {
                var domain = AppDomain.CurrentDomain;

                // re-add the event handler

                domain.AssemblyResolve -= s_AssemblyResolve;
                domain.AssemblyResolve += s_AssemblyResolve;
            }
            catch
            {
            }
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // try to resolve assemblies within our task directory
            // we'll ignore the minor version of the requested assembly
            var assname = new AssemblyName(args.Name);

            try
            {

                var hintpath = Path.Combine(s_path, assname.Name + ".dll");
                if (File.Exists(hintpath))
                {
                    // try to load the assembly:
                    return Assembly.LoadFile(hintpath);
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
