using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing
{
    public class InMemoryEventStore : IEventStore
    {
        public IList<IAggregateRootEvent> Events = new List<IAggregateRootEvent>();

        public IEventStoreSession OpenSession()
        {
            return new InMemoryEventStoreSession(this);
        }
    }
}