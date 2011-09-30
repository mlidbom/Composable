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

        private readonly string perMessageKey = "PerMessageKey_" + Guid.NewGuid();

        private static readonly ILog Log = LogManager.GetLogger(typeof(PerNserviceBusMessageLifestyleManager));
        private static int _instances;

        public PerNserviceBusMessageLifestyleManager()
        {
            Interlocked.Increment(ref _instances);
            Log.DebugFormat("{0} instances after construction", _instances);
        }

        public override object Resolve(CreationContext context)
        {
            var instance = MessageContext.Current[perMessageKey];

            if(instance == null)
            {
                instance = base.Resolve(context);
                MessageContext.Current.Add(perMessageKey, instance);
                WindsorLifestyleMessageModule.RegisterForEviction(this, instance);
            }

            return instance;
        }


        public override bool Release(object instance)
        {
            if(!_evicting) return false;
            var released = base.Release(instance);
            MessageContext.Current.Remove(perMessageKey);
            return released;
        }


        public void Evict(object instance)
        {
            using(new EvictionScope(this))
            {
                Release(instance);
            }
        }


        public override void Dispose()
        {
            Interlocked.Decrement(ref _instances);
            Log.DebugFormat("{0} instances after dispose", _instances);
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