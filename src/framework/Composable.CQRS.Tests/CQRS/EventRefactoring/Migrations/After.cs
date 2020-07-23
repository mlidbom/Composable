using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.SystemCE.Linq;
using Composable.SystemCE.ReflectionCE;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    class After<TEvent> : EventMigration<IRootEvent>
    {
        readonly IEnumerable<Type> _insert;

        public static After<TEvent> Insert<T1>() => new After<TEvent>(Seq.OfTypes<T1>());
        // ReSharper disable once UnusedMember.Global todo:Write test that uses this. We should have a test replacing with a collection.
        public static After<TEvent> Insert<T1, T2>() => new After<TEvent>(Seq.OfTypes<T1, T2>());

        After(IEnumerable<Type> insert) : base(Guid.Parse("544C6694-7B29-4CC0-8DAA-6C50A5F28B70"), "After", "Long description of After") => _insert = insert;

        public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => new Inspector(_insert);

        class Inspector : ISingleAggregateInstanceHandlingEventMigrator
        {
            readonly IEnumerable<Type> _insert;
            Type _lastSeenEventType;

            public Inspector(IEnumerable<Type> insert) => _insert = insert;

            public void MigrateEvent(IAggregateEvent @event, IEventModifier modifier)
            {
                if (_lastSeenEventType == typeof(TEvent) && @event.GetType() != _insert.First())
                {
                    modifier.InsertBefore(_insert.Select(Constructor.CreateInstance).Cast<AggregateEvent>().ToArray());
                }

                _lastSeenEventType = @event.GetType();
            }

        }
    }
}