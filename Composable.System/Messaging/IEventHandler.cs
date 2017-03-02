namespace Composable.Messaging
{
    using Composable.CQRS.EventSourcing;

    ///<summary>An <see cref="IEventSubscriber{TEvent}"/> for the purpose of executing logic in some domain.
    /// <para>Should only be called when events are published. Not when they are replayed.</para>
    /// </summary>
    public interface IEventHandler<in TEvent> : IEventSubscriber<TEvent>
        where TEvent : IEvent {}
}
