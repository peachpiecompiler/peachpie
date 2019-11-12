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
    }
}
