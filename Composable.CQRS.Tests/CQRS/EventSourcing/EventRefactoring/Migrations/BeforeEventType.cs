using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class BeforeEventType<TEvent> : EventMigration<IRootEvent>
    {
        private readonly IEnumerable<Type> _insert;

        public static BeforeEventType<TEvent> Insert<T1>() => new BeforeEventType<TEvent>(Seq.OfTypes<T1>());
        public static BeforeEventType<TEvent> Insert<T1, T2>() => new BeforeEventType<TEvent>(Seq.OfTypes<T1, T2>());
        public static BeforeEventType<TEvent> Insert<T1, T2, T3>() => new BeforeEventType<TEvent>(Seq.OfTypes<T1, T2, T3>());

        private BeforeEventType(IEnumerable<Type> insert) { _insert = insert; }

        public override ISingleAggregateInstanceEventMigrator CreateMigrator() => new Inspector(_insert);

        private class Inspector : ISingleAggregateInstanceEventMigrator
        {
            private readonly IEnumerable<Type> _insert;
            private Type _lastSeenEventType;

            public Inspector(IEnumerable<Type> insert) { _insert = insert; }

            public IEnumerable<IAggregateRootEvent> EndOfAggregateHistoryReached() { return Seq.Empty<IAggregateRootEvent>(); }

            public void MigrateEvent(IAggregateRootEvent @event, IEventModifier modifier)
            {
                if (@event.GetType() == typeof(TEvent) && _lastSeenEventType != _insert.Last())
                {
                    modifier.InsertBefore(_insert.Select(Activator.CreateInstance).Cast<AggregateRootEvent>().ToList());
                }

                _lastSeenEventType = @event.GetType();
            }
        }
    }
}