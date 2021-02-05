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
    sealed class PhpSessionStateContainer : IHttpSessionState
    {
        /// <summary>
        /// The original session container.
        /// </summary>
        public IHttpSessionState UnderlyingContainer { get; }

        /// <summary>
        /// The PHP <c>$_SESSION</c> variable.
        /// It contains all the session variables.
        /// </summary>
        public PhpArray PhpSession { get; private set; }

        // same exception as ArrayList
        static Exception IndexOutOfRangeException() => new ArgumentOutOfRangeException("index");

        public PhpSessionStateContainer(IHttpSessionState container, PhpArray phpsession)
        {
            UnderlyingContainer = container ?? throw new ArgumentNullException(nameof(container));
            PhpSession = phpsession ?? throw new ArgumentNullException(nameof(phpsession));
        }

        public object this[string name]
        {
            get
            {
                return PhpSession.TryGetValue(name, out var value)
                    ? value.ToClr()
                    : UnderlyingContainer[name];
            }
            set
            {
                PhpSession[name] = PhpValue.FromClr(value);

                // copy to underlying container as well
                // in case PhpSession won't get persisted
                UnderlyingContainer[name] = value;
            }
        }

        public object this[int index]
        {
            get
            {
                var enumerator = PhpSession.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    if (0 == index--)
                    {
                        return enumerator.CurrentValue.ToClr();
                    }
                }

                throw IndexOutOfRangeException();
            }
            set
            {
                var enumerator = PhpSession.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    if (0 == index--)
                    {
                        Operators.SetValue(ref enumerator.CurrentValue, PhpValue.FromClr(value));
                        return;
                    }
                }

                throw IndexOutOfRangeException();
            }
        }

        public string SessionID => UnderlyingContainer.SessionID;

        public int Timeout
        {
            get => UnderlyingContainer.Timeout;
            set => UnderlyingContainer.Timeout = value;
        }

        public bool IsNewSession => UnderlyingContainer.IsNewSession;

        public SessionStateMode Mode => UnderlyingContainer.Mode;

        public bool IsCookieless => UnderlyingContainer.IsCookieless;

        public HttpCookieMode CookieMode => UnderlyingContainer.CookieMode;

        public int LCID
        {
            get => UnderlyingContainer.LCID;
            set => UnderlyingContainer.LCID = value;
        }

        public int CodePage
        {
            get => UnderlyingContainer.CodePage;
            set => UnderlyingContainer.CodePage = value;
        }

        public HttpStaticObjectsCollection StaticObjects => UnderlyingContainer.StaticObjects;

        public int Count => PhpSession.Count;

        public NameObjectCollectionBase.KeysCollection Keys => UnderlyingContainer.Keys; // TODO

        public object SyncRoot => UnderlyingContainer.SyncRoot;

        public bool IsReadOnly => UnderlyingContainer.IsReadOnly;

        public bool IsSynchronized => UnderlyingContainer.IsSynchronized;

        public void Abandon()
        {
            UnderlyingContainer.Abandon();
        }

        public void Add(string name, object value)
        {
            PhpSession[name] = PhpValue.FromClr(value);

            // copy to underlying container as well
            // in case PhpSession won't get persisted
            UnderlyingContainer[name] = value;
        }

        public void Clear()
        {
            UnderlyingContainer.Clear();
            PhpSession.Clear();
        }

        /// <summary>
        /// Copies keys to the given array.
        /// </summary>
        public void CopyTo(Array array, int index)
        {
            // UnderlyingContainer.CopyTo(array, index);

            var enumerator = PhpSession.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                array.SetValue(enumerator.CurrentKey.ToString(), index++);
            }
        }

        /// <summary>
        /// Enumerates keys as <see cref="string"/> values.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            // return UnderlyingContainer.GetEnumerator();

            // PhpSession contains all the keys from UnderlyingSession
            var enumerator = PhpSession.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                yield return enumerator.CurrentKey.ToString();
            }
        }

        public void Remove(string name)
        {
            UnderlyingContainer.Remove(name);
            PhpSession.Remove(name);
        }

        public void RemoveAll()
        {
            UnderlyingContainer.RemoveAll();
            PhpSession.Clear();
        }

        public void RemoveAt(int index)
        {
            var enumerator = PhpSession.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                if (0 == index--)
                {
                    var key = enumerator.CurrentKey;
                    PhpSession.Remove(key);
                    UnderlyingContainer.Remove(key.ToString());
                    return;
                }
            }

            throw IndexOutOfRangeException();
        }
    }
}
