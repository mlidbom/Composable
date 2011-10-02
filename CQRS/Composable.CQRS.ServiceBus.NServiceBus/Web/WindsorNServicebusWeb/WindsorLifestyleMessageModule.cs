#region usings

using System;
using System.Collections.Generic;
using NServiceBus;
using log4net;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus.Web.WindsorNServicebusWeb
{
    public class WindsorLifestyleMessageModule : IMessageModule
    {
        [ThreadStatic] private static IDictionary<PerNserviceBusMessageLifestyleManager, object> perThreadEvict;
        private static readonly ILog Log = LogManager.GetLogger(typeof (WindsorLifestyleMessageModule));

        public static void RegisterForEviction(PerNserviceBusMessageLifestyleManager manager, object instance)
        {
            if (perThreadEvict == null)
            {
                perThreadEvict = new Dictionary<PerNserviceBusMessageLifestyleManager, object>();
            }
            perThreadEvict.Add(manager, instance);
        }


        public void HandleBeginMessage()
        {
        }


        public void HandleEndMessage()
        {
            try
            {
                EvictInstancesCreatedDuringMessageHandling();
            }
            catch (Exception e)
            {
                Log.Error("HandleEndMessage failed", e);
                throw;
            }
        }


        public void HandleError()
        {
            try
            {
                EvictInstancesCreatedDuringMessageHandling();
            }
            catch (Exception e)
            {
                Log.Error("HandleEndMessage failed", e);
                throw;
            }
        }


        private static void EvictInstancesCreatedDuringMessageHandling()
        {
            if (perThreadEvict == null)
                return;

            foreach (var itemToEvict in perThreadEvict)
            {
                var manager = itemToEvict.Key;
                manager.Evict(itemToEvict.Value);
            }

            perThreadEvict.Clear();
            perThreadEvict = null;
        }
    }
}