using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.System.Linq;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class After<TEvent> : EventMigration<IRootEvent>
    {
        private readonly IEnumerable<Type> _insert;

        public static After<TEvent> Insert<T1>() => new After<TEvent>(Seq.OfTypes<T1>());
        public static After<TEvent> Insert<T1, T2>() => new After<TEvent>(Seq.OfTypes<T1, T2>());
        public static After<TEvent> Insert<T1, T2, T3>() => new After<TEvent>(Seq.OfTypes<T1, T2, T3>());

        private After(IEnumerable<Type> insert) : base(Guid.Parse("544C6694-7B29-4CC0-8DAA-6C50A5F28B70"), "After", "Long description of After")
        {
            _insert = insert;
        }

        public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => new Inspector(_insert);

        private class Inspector : ISingleAggregateInstanceHandlingEventMigrator
        {
            private readonly IEnumerable<Type> _insert;
            private Type _lastSeenEventType;

            public Inspector(IEnumerable<Type> insert) { _insert = insert; }

            public void MigrateEvent(IAggregateRootEvent @event, IEventModifier modifier)
            {
                if (_lastSeenEventType == typeof(TEvent) && @event.GetType() != _insert.First())
                {
                    modifier.InsertBefore(_insert.Select(Activator.CreateInstance).Cast<AggregateRootEvent>().ToArray());
                }

                _lastSeenEventType = @event.GetType();
            }

        }
    }
}