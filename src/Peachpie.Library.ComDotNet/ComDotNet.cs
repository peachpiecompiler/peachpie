using System;
using Pchp.Core;

namespace Peachpie.Library.ComDotNet
{
    /// <summary>
    /// COM functions
    /// </summary>
    [PhpExtension("com_dotnet")]
    public static class ComDotNet
    {
        /// <summary>
        /// Generate a globally unique identifier (GUID)
        /// </summary>
        public static PhpString com_create_guid()
        {
            return Guid.NewGuid().ToString("B");
        }
    }

}
