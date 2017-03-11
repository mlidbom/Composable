using System;
using System.Collections.Generic;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS.CQRS.EventSourcing
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