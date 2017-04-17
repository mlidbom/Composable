namespace Composable.Persistence.EventStore
{
    /// <summary>
    /// Marks an event as meaning that the aggregate was created.
    /// <para>Can be used by clients to perform logic that should happen whenever an aggregate is created. </para>
    /// <para>Is used in several places in the infrastructure and the infrastructure will fail in various ways if this events is not inherited correctly. For example:</para>
    /// <para>AggregateRoot: Id is only set when such an event is raised. It is only ever possibly to raise 1 such event. More than one will cause an exception</para>
    /// <para>SingleAggregateQueryModelUpdater: Creates the initial query model when it receives such an event</para>
    /// </summary>
    public interface IAggregateRootCreatedEvent : IAggregateRootEvent
    {

    }
}