
using Composable.Messaging;

namespace Composable.CQRS.CQRS.EventSourcing
{
    public interface IReplayEvents
    {
        void Replay(IEvent @event);
    }
}
