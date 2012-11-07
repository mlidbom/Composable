using System;
using System.Collections.Generic;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.EventSourcing
{
    public class InMemoryEventStore : IEventStore
    {
        public InMemoryEventStore(IServiceBus bus)
        {
            Bus = bus;
        }

        public IList<IAggregateRootEvent> Events = new List<IAggregateRootEvent>();
        
        public IServiceBus Bus { get; private set; }

        public IEventStoreSession OpenSession()
        {
            var singleThreadedUseGuard = new SingleThreadUseGuard();
            return new EventStoreSession(Bus, new InMemoryEventSomethingOrOther(this, singleThreadedUseGuard), singleThreadedUseGuard);
        }
    }
}