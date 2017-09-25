using System.Linq;

namespace Composable.Messaging.Buses
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IGlobalBusStateSnapshot busState, IMessage message)
        {
            if(!(message is IQuery))
            {
                return true;
            }

            if(busState.InflightMessages.Select(dispatching => dispatching.Message).Any(dispatching => dispatching is ICommand || dispatching is IEvent))
            {
                return false;
            }

            return true;
        }
    }
}
