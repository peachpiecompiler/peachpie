using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Pchp.Core;

namespace Peachpie.AspNetCore.Web.Session
{
    /// <summary>
    /// <see cref="ISession"/> implementation getting value from both PHP and AspNetCore session states.
    /// </summary>
    sealed class SharedSession : ISession
    {
        public ISession UnderlayingSession { get; private set; }

        public PhpArray PhpSession { get; private set; }

        public SharedSession(ISession session, PhpArray phpsession)
        {
            UnderlayingSession = session ?? throw new ArgumentNullException(nameof(session));
            PhpSession = phpsession ?? throw new ArgumentNullException(nameof(phpsession));
        }

        public bool IsAvailable => UnderlayingSession.IsAvailable;

        public string Id => UnderlayingSession.Id;

        public IEnumerable<string> Keys => UnderlayingSession.Keys;

        public void Clear()
        {
            UnderlayingSession.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return UnderlayingSession.CommitAsync(cancellationToken);
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return UnderlayingSession.LoadAsync(cancellationToken);
        }

        public void Remove(string key)
        {
            PhpSession.RemoveKey(new IntStringKey(key));
            UnderlayingSession.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            UnderlayingSession.Set(key, value);

            // TODO: pass the session variable to PhpSession
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return UnderlayingSession.TryGetValue(key, out value);

            // TODO: get the session variable from PhpSession
        }
    }
}
