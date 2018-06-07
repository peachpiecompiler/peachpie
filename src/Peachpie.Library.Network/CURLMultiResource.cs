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

        internal Queue<PhpArray> MessageQueue { get; } = new Queue<PhpArray>();

        internal int LastError { get; private set; }

        public CURLMultiResource() : base(CURLConstants.CurlMultiResourceName)
        {
        }

        internal int TryAddHandle(CURLResource handle)
        {
            LastError = Handles.Add(handle) ? CURLConstants.CURLM_OK : CURLConstants.CURLM_ADDED_ALREADY;
            return LastError;
        }

        internal void AddResultMessage(CURLResource handle)
        {
            var msg = new PhpArray
            {
                { "msg", CURLConstants.CURLMSG_DONE },
                { "result", handle.Result.ErrorCode },
                { "handle", handle }
            };

            MessageQueue.Enqueue(msg);
        }

        protected override void FreeManaged()
        {
            foreach (var handle in Handles)
            {
                handle.Dispose();
            }

            Handles.Clear();
            MessageQueue.Clear();

            base.FreeManaged();
        }
    }
}
