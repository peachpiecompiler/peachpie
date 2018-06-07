using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Network
{
    /// <summary>
    /// CURL multi handle resource.
    /// </summary>
    public sealed class CURLMultiResource : PhpResource
    {
        internal HashSet<CURLResource> Handles { get; } = new HashSet<CURLResource>();

        public CURLMultiResource() : base(CURLConstants.CurlMultiResourceName)
        {
        }

        protected override void FreeManaged()
        {
            foreach (var handle in Handles)
            {
                handle.Dispose();
            }

            Handles.Clear();

            base.FreeManaged();
        }
    }
}
