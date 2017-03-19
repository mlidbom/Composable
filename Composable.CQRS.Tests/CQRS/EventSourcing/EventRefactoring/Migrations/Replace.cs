using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Persistence.EventSourcing;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    class Replace<TEvent> : EventMigration<IRootEvent>
    {
        readonly Migrator _migratorSingleton;

        public static Replace<TEvent> With<T1>() => new Replace<TEvent>(Seq.OfTypes<T1>());
        public static Replace<TEvent> With<T1, T2>() => new Replace<TEvent>(Seq.OfTypes<T1, T2>());

        Replace(IEnumerable<Type> replaceWith) : base(Guid.Parse("9B51F7BC-D9B3-43C7-A183-76CA5E662091"), "Replace", "Long description of Replace") => _migratorSingleton = new Migrator(replaceWith);

        public override ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator() => _migratorSingleton;

        class Migrator : ISingleAggregateInstanceHandlingEventMigrator
        {
            readonly IEnumerable<Type> _replaceWith;

            public Migrator(IEnumerable<Type> replaceWith) => _replaceWith = replaceWith;

            public void MigrateEvent(IAggregateRootEvent @event, IEventModifier modifier)
            {
                if (@event.GetType() == typeof(TEvent))
                {
                    modifier.Replace(_replaceWith.Select(Activator.CreateInstance).Cast<AggregateRootEvent>().ToArray());
                }
            }
        }
    }
}