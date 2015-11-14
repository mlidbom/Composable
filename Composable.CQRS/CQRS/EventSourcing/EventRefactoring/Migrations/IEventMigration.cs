using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    ///<summary>Defines an identity for migration of events into other events. Creates </summary>
    public interface IEventMigration
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        bool Done { get; }
        Type MigratedAggregateEventHierarchyRootInterface { get; }

        ISingleAggregateInstanceEventMigrator CreateMigrator();
    }

    ///<summary>Responsible for migrating the events of a single instance of an aggregate.</summary>
    public interface ISingleAggregateInstanceEventMigrator
    {
        ///<summary>Inspect one event and if required mutate the event stream by calling methods on the modifier</summary>
        void MigrateEvent(IAggregateRootEvent @event, IEventModifier modifier);
    }
}
