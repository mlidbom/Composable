using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReflectionCE;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    class Before<TEvent> : EventMigration<IRootEvent>
    {
        readonly IEnumerable<Type> _insert;

        public static Before<TEvent> Insert<T1>() => new Before<TEvent>(EnumerableCE.OfTypes<T1>());
        public static Before<TEvent> Insert<T1, T2>() => new Before<TEvent>(EnumerableCE.OfTypes<T1, T2>());

        Before(IEnumerable<Type> insert) : base(Guid.Parse("0533D2E4-DE78-4751-8CAE-3343726D635B"), "Before", "Long description of Before") => _insert = insert;

        public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => new Inspector(_insert);

        class Inspector : ISingleAggregateInstanceHandlingEventMigrator
        {
            readonly IEnumerable<Type> _insert;
            Type _lastSeenEventType;

            public Inspector(IEnumerable<Type> insert) => _insert = insert;

            public void MigrateEvent(IAggregateEvent @event, IEventModifier modifier)
            {
                if (@event.GetType() == typeof(TEvent) && _lastSeenEventType != _insert.Last())
                {
                    modifier.InsertBefore(_insert.Select(Constructor.CreateInstance).Cast<AggregateEvent>().ToArray());
                }

                _lastSeenEventType = @event.GetType();
            }
        }
    }
}