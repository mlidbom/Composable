using System;
using System.Diagnostics.Contracts;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public abstract class EventMigration<TMigratedAggregateEventHierarchyRootInterface> : IEventMigration
        where TMigratedAggregateEventHierarchyRootInterface : IAggregateRootEvent
    {
        protected EventMigration()
        {
            Contract.Assert(typeof(TMigratedAggregateEventHierarchyRootInterface).IsInterface, $"{nameof(TMigratedAggregateEventHierarchyRootInterface)} must be an interface.");
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Done { get; }
        public Type MigratedAggregateEventHierarchyRootInterface => typeof(TMigratedAggregateEventHierarchyRootInterface);
        public abstract ISingleAggregateInstanceEventMigrator CreateMigrator();
    }
}