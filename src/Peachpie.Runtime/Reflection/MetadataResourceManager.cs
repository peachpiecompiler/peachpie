using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;

namespace Peachpie.Runtime.Reflection
{
    /// <summary>
    /// Provides access to resources embedded within the compiled assembly.
    /// </summary>
    public static class MetadataResourceManager
    {
        /// <summary>
        /// Cache or resource managers created for '.source.metadata.resources' of assemblies.
        /// </summary>
        static Dictionary<Assembly, ResourceManager> s_resourceManagers;

        /// <summary>
        /// Gets resource manager for assembly metadata (<c>.source.metadata.resources</c>).
        /// </summary>
        /// <returns>Gets the resource manager or <c>null</c> if there are no metadata.</returns>
        public static ResourceManager GetResourceManager(Assembly ass)
        {
            var managers = s_resourceManagers;

            ResourceManager rm;

            if (managers == null || !managers.TryGetValue(ass, out rm))
            {
                var dict = (managers != null) ? new Dictionary<Assembly, ResourceManager>(managers) : new Dictionary<Assembly, ResourceManager>();
                dict[ass] = rm = new ResourceManager(".source.metadata", ass);
                Interlocked.CompareExchange(ref s_resourceManagers, dict, managers);
            }

            //
            return rm;
        }

        public static string GetMetadata(Assembly assembly, string symbolId)
        {
            var rm = GetResourceManager(assembly);
            if (rm != null)
            {
                try
                {
                    return rm.GetString(symbolId);
                }
                catch
                {
                    // resource is missing
                }
            }

            return null;
        }
    }
}
