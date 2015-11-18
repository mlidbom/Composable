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

        ///<summary>The event interface that is the root of the event hierarchy for the aggregate whose events this migration modifies</summary>
        Type MigratedAggregateEventHierarchyRootInterface { get; }

        ISingleAggregateInstanceEventMigrator CreateMigrator();
    }

    ///<summary>
    /// <para>Responsible for migrating the events of a single instance of an aggregate.</para>
    /// </summary>
    public interface ISingleAggregateInstanceEventMigrator
    {
        ///<summary>
        /// <para>Inspect one event and if required mutate the event stream by calling methods on the modifier</para>
        /// <para>Called once for each event in the aggregate's history. </para>
        /// <para>Then it is called once with an instance of <see cref="EndOfAggregateHistoryEventPlaceHolder"/>. </para>
        /// </summary>
        void MigrateEvent(IAggregateRootEvent @event, IEventModifier modifier);
    }
}
