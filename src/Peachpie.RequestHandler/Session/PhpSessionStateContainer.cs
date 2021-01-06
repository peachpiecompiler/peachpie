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
        public IHttpSessionState UnderlayingContainer { get; private set; }

        /// <summary>
        /// The PHP <c>$_SESSION</c> variable.
        /// It contains all the session variables.
        /// </summary>
        public PhpArray PhpSession { get; private set; }

        // same exception as ArrayList
        static Exception IndexOutOfRangeException() => new ArgumentOutOfRangeException("index");

        public PhpSessionStateContainer(IHttpSessionState container, PhpArray phpsession)
        {
            UnderlayingContainer = container ?? throw new ArgumentNullException(nameof(container));
            PhpSession = phpsession ?? throw new ArgumentNullException(nameof(phpsession));
        }

        public object this[string name]
        {
            get
            {
                return PhpSession.TryGetValue(name, out var value)
                    ? value.ToClr()
                    : UnderlayingContainer[name];
            }
            set
            {
                PhpSession[name] = PhpValue.FromClr(value);

                // copy to underlaying container as well
                // in case PhpSession won't get persisted
                UnderlayingContainer[name] = value;
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

        public int Count => PhpSession.Count;

        public NameObjectCollectionBase.KeysCollection Keys => UnderlayingContainer.Keys; // TODO

        public object SyncRoot => UnderlayingContainer.SyncRoot;

        public bool IsReadOnly => UnderlayingContainer.IsReadOnly;

        public bool IsSynchronized => UnderlayingContainer.IsSynchronized;

        public void Abandon()
        {
            UnderlayingContainer.Abandon();
        }

        public void Add(string name, object value)
        {
            PhpSession[name] = PhpValue.FromClr(value);

            // copy to underlaying container as well
            // in case PhpSession won't get persisted
            UnderlayingContainer[name] = value;
        }

        public void Clear()
        {
            UnderlayingContainer.Clear();
            PhpSession.Clear();
        }

        /// <summary>
        /// Copies keys to the given array.
        /// </summary>
        public void CopyTo(Array array, int index)
        {
            // UnderlayingContainer.CopyTo(array, index);

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
            // return UnderlayingContainer.GetEnumerator();

            // PhpSession conains all the keys from UnderlayingSession
            var enumerator = PhpSession.GetFastEnumerator();
            while (enumerator.MoveNext())
            {
                yield return enumerator.CurrentKey.ToString();
            }
        }

        public void Remove(string name)
        {
            UnderlayingContainer.Remove(name);
            PhpSession.Remove(name);
        }

        public void RemoveAll()
        {
            UnderlayingContainer.RemoveAll();
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
                    UnderlayingContainer.Remove(key.ToString());
                    return;
                }
            }

            throw IndexOutOfRangeException();
        }
    }
}
