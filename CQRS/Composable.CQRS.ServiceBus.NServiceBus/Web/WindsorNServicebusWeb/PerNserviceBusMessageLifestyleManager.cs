#region usings

using System;
using System.Threading;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Lifestyle;
using log4net;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus.Web.WindsorNServicebusWeb
{
    public class PerNserviceBusMessageLifestyleManager : AbstractLifestyleManager
    {
        private bool _evicting;

        private readonly string _perMessageKey = "PerMessageKey_" + Guid.NewGuid();

        private static readonly ILog Log = LogManager.GetLogger(typeof(PerNserviceBusMessageLifestyleManager));
        private static int _instances;

        public PerNserviceBusMessageLifestyleManager()
        {
            Interlocked.Increment(ref _instances);
            Log.DebugFormat("{0}: {1} instances after construction", _perMessageKey, _instances);
        }

        public override object Resolve(CreationContext context)
        {
            lock (_perMessageKey)
            {
                var instance = MessageContext.Current[_perMessageKey];

                if (instance == null)
                {
                    instance = base.Resolve(context);
                    Log.DebugFormat("{0}: base resolved: {1}: {2}", _perMessageKey, instance.GetType(), instance);
                    MessageContext.Current.Add(_perMessageKey, instance);
                    WindsorLifestyleMessageModule.RegisterForEviction(this, instance);
                }
                Log.DebugFormat("{0}: resolved from context: {1}: {2}", _perMessageKey, instance.GetType(), instance);
                return instance;
            }
        }


        public override bool Release(object instance)
        {
            lock (_perMessageKey)
            {
                // Since this method is called by the kernel when an external
                // request to release the component is made, it must do nothing
                // to ensure the component is available during the duration of 
                // the web request.  An internal Evict method is provided to
                // allow the actual releasing of the component at the end of
                // the web request.

                if (!_evicting)
                {
                    Log.DebugFormat("{0}: not evicting, bailing out of release of {1}", _perMessageKey, instance);
                    return false;
                }
                var released = base.Release(instance);
                Log.DebugFormat("{0}: base released: {1}: {2} with result: {3}", _perMessageKey, instance.GetType(), instance, released);
                MessageContext.Current.Remove(_perMessageKey);
                return released;
            }
        }


        public void Evict(object instance)
        {
            lock (_perMessageKey)
            {
                Log.DebugFormat("{0}: evict called on: {1}", _perMessageKey, instance);
                using (new EvictionScope(this))
                {
                    Release(instance);
                }
            }
        }


        public override void Dispose()
        {
            lock (_perMessageKey)
            {
                Interlocked.Decrement(ref _instances);
                Log.DebugFormat("{0} instances after dispose", _instances);
            }
        }


        private class EvictionScope : IDisposable
        {
            private readonly PerNserviceBusMessageLifestyleManager _owner;
            public EvictionScope(PerNserviceBusMessageLifestyleManager owner)
            {
                _owner = owner;
                _owner._evicting = true;
            }

            public void Dispose()
            {
                _owner._evicting = false;
            }
        }
    }
}