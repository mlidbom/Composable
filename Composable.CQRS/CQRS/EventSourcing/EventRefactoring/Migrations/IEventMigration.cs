using System;
using System.Collections.Generic;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public interface IEventMigration
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        bool Done { get; }

        ///<summary>Given the already seen history, insert any events at the end of the stream that might be required</summary>
        IEnumerable<IAggregateRootEvent> EndOfAggregateHistoryReached();

        ///<summary>Inspect one event and if required mutate the event stream by replacing the event, or inserting event(s) before it</summary>
        void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier);
    }
}
