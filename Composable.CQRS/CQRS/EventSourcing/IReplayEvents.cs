
using Composable.Messaging;

namespace Composable.CQRS.EventSourcing
{
    public interface IReplayEvents
    {
        void Replay(IEvent @event);
    }
}
