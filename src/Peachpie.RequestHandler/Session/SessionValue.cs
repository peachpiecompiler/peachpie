using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.SessionState;
using Pchp.Core;

namespace Peachpie.RequestHandler.Session
{
    /// <summary>
    /// A PHP value representing a session state variable.
    /// </summary>
    /// <remarks>
    /// The value itself is retrieved from the session state lazily.
    /// This is important for session state servers out-of-proc,
    /// and to avoid unnecessary deserialization of session variables.
    /// </remarks>
    sealed class SessionValue : PhpAlias
    {
        readonly IHttpSessionState _session;
        readonly string _name;

        public SessionValue(IHttpSessionState session, string name) : base()
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override PhpValue Value
        {
            get => PhpValue.FromClr(_session[_name]);
            set => _session[_name] = value.ToClr();
        }

        public override IPhpArray EnsureArray()
        {
            if (Value.IsPhpArray(out var array))
            {
                return array;
            }

            //
            var value = this.Value;
            var iarray = PhpValue.EnsureArray(ref value);
            this.Value = PhpValue.Create(iarray);
            return iarray;
        }

        public override object EnsureObject()
        {
            var value = this.Value;
            var obj = PhpValue.EnsureObject(ref value);
            this.Value = PhpValue.FromClass(obj);
            return obj;
        }

        public override PhpString.Blob EnsureWritableString()
        {
            var value = this.Value;
            var str = Operators.EnsureWritableString(ref value);
            this.Value = new PhpString(str);
            return str;
        }

        public override PhpAlias EnsureItemAlias(PhpValue index, bool quiet = false)
        {
            var value = this.Value;
            var alias = Operators.EnsureItemAlias(ref value, index, quiet);
            this.Value = value;
            return alias;
        }
    }
}
