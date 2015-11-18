using System;
using System.Diagnostics.Contracts;
using Composable.System;

namespace Composable.CQRS.EventSourcing.Refactoring.Migrations
{
    public abstract class EventMigration<TMigratedAggregateEventHierarchyRootInterface> : IEventMigration
        where TMigratedAggregateEventHierarchyRootInterface : IAggregateRootEvent
    {
        protected EventMigration(Guid id, string name, string description)
        {
            Contract.Requires(id != Guid.Empty);
            Contract.Requires(!name.IsNullOrWhiteSpace());
            Contract.Requires(!description.IsNullOrWhiteSpace());

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