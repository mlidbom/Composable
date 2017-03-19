using System;
using System.Collections.Generic;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS.EventSourcing
{
    public interface IEventStored
    {
        Guid Id { get; }
        int Version { get; }
        IEnumerable<IAggregateRootEvent> GetChanges();
        void AcceptChanges();
        void LoadFromHistory(IEnumerable<IAggregateRootEvent> history);
        void SetTimeSource(IUtcTimeTimeSource timeSource);
    }
}