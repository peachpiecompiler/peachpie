using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using Pchp.Core;

namespace Peachpie.RequestHandler.Session
{
    /// <summary>
    /// <see cref="IHttpSessionState"/> implementation getting value from both PHP and AspNetCore session states.
    /// </summary>
    sealed class SharedSession : IHttpSessionState
    {
        public IHttpSessionState UnderlayingContainer { get; private set; }

        public PhpArray PhpSession { get; private set; }

        public SharedSession(IHttpSessionState container, PhpArray phpsession)
        {
            UnderlayingContainer = container ?? throw new ArgumentNullException(nameof(container));
            PhpSession = phpsession ?? throw new ArgumentNullException(nameof(phpsession));
        }

        public object this[string name]
        {
            get => UnderlayingContainer[name];          // TODO: try get PHP session var
            set => UnderlayingContainer[name] = value;  // TODO: store to PHP session vars
        }

        public object this[int index]
        {
            get => UnderlayingContainer[index];
            set => UnderlayingContainer[index] = value;
        }

        public string SessionID => UnderlayingContainer.SessionID;

        public int Timeout
        {
            get => UnderlayingContainer.Timeout;
            set => UnderlayingContainer.Timeout = value;
        }

        public bool IsNewSession => UnderlayingContainer.IsNewSession;

        public SessionStateMode Mode => UnderlayingContainer.Mode;

        public bool IsCookieless => UnderlayingContainer.IsCookieless;

        public HttpCookieMode CookieMode => UnderlayingContainer.CookieMode;

        public int LCID
        {
            get => UnderlayingContainer.LCID;
            set => UnderlayingContainer.LCID = value;
        }

        public int CodePage
        {
            get => UnderlayingContainer.CodePage;
            set => UnderlayingContainer.CodePage = value;
        }

        public HttpStaticObjectsCollection StaticObjects => UnderlayingContainer.StaticObjects;

        public int Count => UnderlayingContainer.Count; // TODO: add PHP session vars indexed by int?

        public NameObjectCollectionBase.KeysCollection Keys => UnderlayingContainer.Keys;

        public object SyncRoot => UnderlayingContainer.SyncRoot;

        public bool IsReadOnly => UnderlayingContainer.IsReadOnly;

        public bool IsSynchronized => UnderlayingContainer.IsSynchronized;

        public void Abandon()
        {
            UnderlayingContainer.Abandon();
        }

        public void Add(string name, object value)
        {
            // TODO: store to PHP session vars
            UnderlayingContainer.Add(name, value);
        }

        public void Clear()
        {
            UnderlayingContainer.Clear();
        }

        public void CopyTo(Array array, int index)
        {
            UnderlayingContainer.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            // TODO: add PHP session vars
            return UnderlayingContainer.GetEnumerator();
        }

        public void Remove(string name)
        {
            UnderlayingContainer.Remove(name);
        }

        public void RemoveAll()
        {
            UnderlayingContainer.RemoveAll();
        }

        public void RemoveAt(int index)
        {
            UnderlayingContainer.RemoveAt(index);
        }
    }
}
