using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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

    internal class EventModifier : IEventModifier
    {
        private IAggregateRootEvent _event;
        private IEnumerable<IAggregateRootEvent> _replacementEvents;
        private IEnumerable<IAggregateRootEvent> _insertedEvents;

        public EventModifier(IAggregateRootEvent @event) { _event = @event; }

        public void Replace(IEnumerable<IAggregateRootEvent> events)
        {
            Contract.Assert(_replacementEvents == null, "You can only call Replace once");

            _replacementEvents = events;
        }

        public void InsertBefore(IEnumerable<IAggregateRootEvent> events)
        {
            Contract.Assert(_insertedEvents == null, "You can only call InsertBefore once");

            _insertedEvents = events;
        }

        public IEnumerable<IAggregateRootEvent> GetMutated()
        {
            if(_insertedEvents != null)
            {
                foreach(var inserted in _insertedEvents)
                {
                    yield return inserted;
                }
            }

            if(_replacementEvents != null)
            {
                foreach(var inserted in _replacementEvents)
                {
                    yield return inserted;
                }
            }
            else
            {
                yield return _event;
            }
        }
    }

    internal class EventStreamMutator
    {
        private readonly IEventMigration[] _eventMigrations;
        public EventStreamMutator(params IEventMigration[] eventMigrations) { _eventMigrations = eventMigrations; }

        private IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event, IEventMigration migration)
        {
            var modifier = new EventModifier(@event);
            migration.InspectEvent(@event, modifier);
            return modifier.GetMutated();
        }

        public IEnumerable<IAggregateRootEvent> Mutate(IAggregateRootEvent @event)
        {
            return Mutate(@event, _eventMigrations.Single());
        }
    }
}
