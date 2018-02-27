using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Network
{
    //[PhpExtension("curl")]
    public static class CURLFunctions
    {
        /// <summary>
        /// Create a CURLFile object.
        /// </summary>
        [return: NotNull]
        public static CURLFile/*!*/curl_file_create(string filename, string mimetype = null, string postname = null) => new CURLFile(filename, mimetype, postname);
    }
}
