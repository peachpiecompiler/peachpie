using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Pchp.Core.Reflection;

namespace Pchp.Core
{
    public class ClrEvent<TDelegate> : IPhpCallable where TDelegate : Delegate
    {
        sealed class Hook : IDisposable
        {
            public Hook(object target, EventInfo eventInfo, TDelegate callback)
            {
                EventInfo = eventInfo ?? throw new ArgumentNullException(nameof(eventInfo));
                Target = target ?? throw new ArgumentNullException(nameof(target));
                Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            [PhpHidden]
            public EventInfo EventInfo { get; }

            [PhpHidden]
            public object Target { get; }

            [PhpHidden]
            public TDelegate Callback { get; }

            /// <summary>Alias to <see cref="Dispose"/></summary>
            public void Close() => Dispose();

            /// <summary>Alias to <see cref="Dispose"/></summary>
            public void Remove() => Dispose();

            public void Dispose()
            {
                EventInfo.RemoveEventHandler(Target, Callback);
            }
        }

        [PhpHidden]
        public EventInfo EventInfo { get; }

        [PhpHidden]
        public object Target { get; }

        /// <summary>
        /// The event field name.
        /// </summary>
        public string name => EventInfo.Name;

        /// <summary>
        /// Reference to the owning object instance.
        /// </summary>        
        public string @class => EventInfo.DeclaringType.GetPhpTypeInfo().Name;

        internal ClrEvent(Context ctx, object target, EventInfo eventInfo)
        {
            this.Target = target;
            this.EventInfo = eventInfo ?? throw new ArgumentNullException(nameof(eventInfo));
        }

        public IDisposable/*!*/add(TDelegate callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            EventInfo.AddEventHandler(Target, callback);
            return new Hook(Target, EventInfo, callback);
        }

        public void remove(TDelegate callback)
        {
            EventInfo.RemoveEventHandler(Target, callback);

            throw new NotSupportedException();
        }

        #region IPhpCallable

        PhpValue IPhpCallable.Invoke(Context ctx, params PhpValue[] arguments)
        {
            //TODO: check caller, invoke EventInfo.RaiseMethod?.Invoke(Target, arguments)
            throw new NotSupportedException();
        }

        PhpValue IPhpCallable.ToPhpValue() => PhpValue.FromClr(this);

        //public PhpValue __invoke([ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] RuntimeTypeHandle caller, params PhpValue[] arguments)
        //{
        //    if (caller.Equals(default(RuntimeTypeHandle)))
        //    {
        //        return false;
        //    }

        //    return PhpValue.Null;
        //}

        #endregion
    }
}
