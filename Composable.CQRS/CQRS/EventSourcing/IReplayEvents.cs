using NServiceBus;

namespace Composable.CQRS.EventSourcing
{
    public interface IReplayEvents
    {
        void Replay(IEvent @event);
    }

    public class EventsReplayer : IReplayEvents
    {
//        private readonly IEventHandlerInvoker _eventHandlerInvoker;
//
//        public EventsReplayer(IEventHandlerInvoker eventHandlerInvoker)
//        {
//            _eventHandlerInvoker = eventHandlerInvoker;
//        }
//
//        public void Replay(IEvent @event)
//        {
//            _eventHandlerInvoker.Invoke(@event);
//        }
        public void Replay(IEvent @event)
        {
            throw new global::System.NotImplementedException();
        }
    }
}
