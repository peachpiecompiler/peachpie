using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    [PhpExtension("standard")]
    public static class CLI
    {
        ///// <summary>
        ///// Set the process title.
        ///// </summary>
        ///// <param name="title">The title to use as the process title.</param>
        //public static void setproctitle(string title)
        //{
        //    cli_set_process_title(title);
        //}

        /// <summary>
        /// Sets the process title.
        /// </summary>
        /// <param name="title">The new title.</param>
        /// <returns>True if the function succeeded.</returns>
        public static bool cli_set_process_title(string title)
        {
            try
            {
                Console.Title = title;

                return true;
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Returns the current process title as supported by the operating system.
        /// </summary>
        /// <returns>The process title or <c>null</c> in case of error.</returns>
        public static string cli_get_process_title()
        {

            try
            {
                // Process.GetCurrentProcess().MainWindowTitle ?
                return Console.Title;
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return null;
            }
        }
    }
}
