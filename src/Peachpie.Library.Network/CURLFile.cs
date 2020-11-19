using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Network
{
    /// <summary>
    /// Object that should be used to upload a file with <c>curl</c> <c>CURLOPT_POSTFIELDS</c> option.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(CURLConstants.ExtensionName)]
    public sealed class CURLFile
    {
        public string name { get; private set; }

        public string mime
        {
            get => _mime;   // default: "application/octet-stream";
            set => _mime = value ?? string.Empty;
        }
        string _mime;

        public string postname
        {
            get => _postname;   // default: name;
            set => _postname = value ?? string.Empty;
        }
        string _postname;

        //[PhpFieldsOnlyCtor]
        //protected CURLFile() { }

        public CURLFile(string filename, string mimetype = null, string postname = null) => __construct(filename, mimetype, postname);

        public void __construct(string filename, string mimetype = null, string postname = null)
        {
            this.name = filename;
            this.mime = mimetype;
            this.postname = postname;
        }

        public string getFilename() => this.name;

        public string getMimeType() => this.mime;

        public void setMimeType(string name) => this.mime = name;

        public string getPostFilename() => this.postname;

        public void setPostFilename(string name) => this.postname = name;
    }
}
