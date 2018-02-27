using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Network
{
    /// <summary>
    /// CURL resource.
    /// </summary>
    public sealed class CURLResource : PhpResource
    {
        #region Properties

        public string Url { get; set; }

        public bool ReturnTransfer { get; set; } = false;

        /// <summary>
        /// <c>true</c> to include the header in the output.
        /// Default is <c>false</c>.
        /// </summary>
        public bool OutputHeader { get; set; } = false;

        #endregion

        public CURLResource() : base(CURLConstants.CurlResourceName)
        {
        }

        protected override void FreeManaged()
        {
            base.FreeManaged();
        }
    }
}
