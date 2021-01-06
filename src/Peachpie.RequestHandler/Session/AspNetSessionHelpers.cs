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
        /// <summary>
        /// Field <see cref="HttpSessionState"/>.<c>_container</c>.
        /// Can be <c>null</c> in case of an API change.
        /// </summary>
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

        /// <summary>
        /// private static SessionStateSection s_config
        /// </summary>
        static FieldInfo s_SessionIDManager_config = typeof(SessionIDManager).GetField("s_config", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Gets the configured session cookie name, or <c>null</c> if the value cannot be determined.
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

            return null;
        }
    }
}
