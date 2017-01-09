using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Resources
{
    internal class LibResources : Resources
    {
        /// <summary>
        /// Retrieves formatted string resource.
        /// </summary>
        /// <param name="id">The string identifier.</param>
        /// <param name="arg">A format parameter.</param>
        /// <returns>The formatted string resource.</returns>
        public static string GetString(string id, object arg)
        {
            return string.Format(Resources.ResourceManager.GetString(id), arg);
        }

        /// <summary>
		/// Retrieves formatted string resource.
		/// </summary>
		/// <param name="id">The string identifier.</param>
		/// <param name="args">Format strings.</param>
		/// <returns>The formatted string resource.</returns>
		public static string GetString(string id, params object[] args)
        {
            return string.Format(Resources.ResourceManager.GetString(id), args);
        }
    }
}
