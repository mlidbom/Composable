using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public interface IEventMigration
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        bool Done { get; }

        ///<summary>Given the already seen history, insert any events at the end of the stream that might be required</summary>
        IEnumerable<IAggregateRootEvent> End();

        ///<summary>Inspect one event and if required mutate the event stream by replacing the event, or inserting event(s) before it</summary>
        void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier);
    }


    public abstract class EventMigration : IEventMigration
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Done { get; }
        public virtual IEnumerable<IAggregateRootEvent> End() { return Seq.Empty<IAggregateRootEvent>(); }
        public abstract void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier);
    }

    internal class EventStreamMutator
    {
                
    }
}