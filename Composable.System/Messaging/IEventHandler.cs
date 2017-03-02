using Composable.CQRS.EventSourcing;

namespace Composable.Messaging
{
    ///<summary>An <see cref="IEventListener{TEvent}"/> for the purpose of executing logic in some domain.
    /// <para>Should only be called when events are published. Not when they are replayed.</para>
    /// </summary>
    public interface IEventHandler<in TEvent> : IEventListener<TEvent>
        where TEvent : IEvent {}
}
