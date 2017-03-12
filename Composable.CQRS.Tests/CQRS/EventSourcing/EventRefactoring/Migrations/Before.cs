using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.EventSourcing;
using Composable.System.Linq;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    class Before<TEvent> : EventMigration<IRootEvent>
    {
        readonly IEnumerable<Type> _insert;

        public static Before<TEvent> Insert<T1>() => new Before<TEvent>(Seq.OfTypes<T1>());
        public static Before<TEvent> Insert<T1, T2>() => new Before<TEvent>(Seq.OfTypes<T1, T2>());

        Before(IEnumerable<Type> insert) : base(Guid.Parse("0533D2E4-DE78-4751-8CAE-3343726D635B"), "Before", "Long description of Before") => _insert = insert;

        public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => new Inspector(_insert);

        class Inspector : ISingleAggregateInstanceHandlingEventMigrator
        {
            readonly IEnumerable<Type> _insert;
            Type _lastSeenEventType;

            public Inspector(IEnumerable<Type> insert) => _insert = insert;

            public void MigrateEvent(IAggregateRootEvent @event, IEventModifier modifier)
            {
                if (@event.GetType() == typeof(TEvent) && _lastSeenEventType != _insert.Last())
                {
                    modifier.InsertBefore(_insert.Select(Activator.CreateInstance).Cast<AggregateRootEvent>().ToArray());
                }

                _lastSeenEventType = @event.GetType();
            }
        }
    }
}