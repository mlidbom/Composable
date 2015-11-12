using System;
using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public abstract class EventMigration : IEventMigration
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Done { get; }
        public virtual IEnumerable<IAggregateRootEvent> End() { return Seq.Empty<IAggregateRootEvent>(); }
        public abstract void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier);
    }
}