using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.SessionState;

namespace Peachpie.RequestHandler.Session
{
    static class AspNetSessionHelpers
    {
        /// <summary>Field <see cref="HttpSessionState"/>.<c>_container</c>.</summary>
        static FieldInfo s_HttpSessionState_container = typeof(HttpSessionState).GetField("_container", BindingFlags.Instance | BindingFlags.NonPublic);

        public static IHttpSessionState GetContainer(this HttpSessionState state)
        {
            Debug.Assert(s_HttpSessionState_container != null, "HttpSessionState._container missing!");

            if (s_HttpSessionState_container != null)
            {
                return (IHttpSessionState)s_HttpSessionState_container.GetValue(state);
            }
            else
            {
                return null;
            }
        }

        public static void SetContainer(this HttpSessionState state, IHttpSessionState container)
        {
            Debug.Assert(s_HttpSessionState_container != null, "HttpSessionState._container missing!");

            if (s_HttpSessionState_container != null)
            {
                s_HttpSessionState_container.SetValue(state, container ?? throw new ArgumentNullException(nameof(container)));
            }
        }

        /// <summary>Field <see cref="HttpSessionStateContainer"/>.<c>_sessionItems</c>.</summary>
        static FieldInfo s_HttpSessionStateContainer_sessionItems = typeof(HttpSessionStateContainer).GetField("_sessionItems", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Gets session items name avoiding deserialization of items.
        /// </summary>
        public static string[] GetSessionItemsName(this HttpSessionState state)
        {
            if (state.Count == 0)
            {
                return Array.Empty<string>();
            }

            if (s_HttpSessionStateContainer_sessionItems != null)
            {
                var HttpSessionStateContainer = GetContainer(state);

                // private ISessionStateItemCollection _sessionItems;
                var _sessionItems = (ISessionStateItemCollection)s_HttpSessionStateContainer_sessionItems.GetValue(HttpSessionStateContainer);
                if (_sessionItems != null)
                {
                    // NOTE: _sessionItems.Keys causes deserialization of all items

                    // NameObjectCollectionBase.BaseGetAllKeys() : string[]
                    var BaseGetAllKeys = typeof(System.Collections.Specialized.NameObjectCollectionBase).GetMethod("BaseGetAllKeys", BindingFlags.Instance | BindingFlags.NonPublic);
                    return (string[])BaseGetAllKeys.Invoke(_sessionItems, Array.Empty<object>());
                }
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// The default value of "cookieName" configuration property.
        /// Defined in <see cref="System.Web.SessionState.SessionIDManager"/>.<c>SESSION_COOKIE_DEFAULT</c>.
        /// </summary>
        public const string AspNetSessionCookieName = "ASP.NET_SessionId";

        /// <summary>
        /// private static SessionStateSection s_config
        /// </summary>
        static FieldInfo s_SessionIDManager_config = typeof(SessionIDManager).GetField("s_config", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Gets the configured session cookie name, or default value if configuration cannot be obtained.
        /// </summary>
        /// <returns></returns>
        public static string GetConfigCookieName()
        {
            if (s_SessionIDManager_config != null)
            {
                var section = s_SessionIDManager_config.GetValue(null) as System.Web.Configuration.SessionStateSection;
                if (section != null)
                {
                    return section.CookieName;
                }
            }

            return AspNetSessionCookieName;
        }
    }
}
