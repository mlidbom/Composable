using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class ReplaceEventType<TEvent> : EventMigration<IRootEvent>
    {
        private readonly IEnumerable<Type> _replaceWith;

        public static ReplaceEventType<TEvent> With<T1>() => new ReplaceEventType<TEvent>(Seq.OfTypes<T1>());
        public static ReplaceEventType<TEvent> With<T1, T2>() => new ReplaceEventType<TEvent>(Seq.OfTypes<T1, T2>());
        public static ReplaceEventType<TEvent> With<T1, T2, T3>() => new ReplaceEventType<TEvent>(Seq.OfTypes<T1, T2, T3>());

        private ReplaceEventType(IEnumerable<Type> replaceWith) { _replaceWith = replaceWith; }

        public override ISingleAggregateInstanceEventMigrator CreateMigrator() { return new Migrator(_replaceWith); }

        private class Migrator : ISingleAggregateInstanceEventMigrator
        {
            private readonly IEnumerable<Type> _replaceWith;

            public Migrator(IEnumerable<Type> replaceWith) { _replaceWith = replaceWith; }

            public IEnumerable<IAggregateRootEvent> EndOfAggregateHistoryReached() => Seq.Empty<IAggregateRootEvent>();

            public void MigrateEvent(IAggregateRootEvent @event, IEventModifier modifier)
            {
                if (@event.GetType() == typeof(TEvent))
                {
                    modifier.Replace(_replaceWith.Select(Activator.CreateInstance).Cast<AggregateRootEvent>().ToList());
                }
            }
        }
    }
}