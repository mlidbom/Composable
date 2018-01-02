using System;
using Composable.Contracts;
using Composable.Messaging;

namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    abstract class EventMigration<TMigratedAggregateEventHierarchyRootInterface> : IEventMigration
        where TMigratedAggregateEventHierarchyRootInterface : IAggregateRootEvent
    {
        protected EventMigration(Guid id, string name, string description)
        {
            OldContract.Argument(() => id)
                        .NotNullOrDefault();

            OldContract.Argument(() => description, () => name)
                        .NotNullEmptyOrWhiteSpace();

            OldContract.Assert.That(typeof(TMigratedAggregateEventHierarchyRootInterface).IsInterface, "typeof(TMigratedAggregateEventHierarchyRootInterface).IsInterface");

            Id = id;
            Name = name;
            Description = description;
            Done = false;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Done { get; }
        public Type MigratedAggregateEventHierarchyRootInterface => typeof(TMigratedAggregateEventHierarchyRootInterface);
        public abstract ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator();
    }
}