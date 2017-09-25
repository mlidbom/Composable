using System.Linq;

namespace Composable.Messaging.Buses
{
    class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
    {
        public bool CanBeDispatched(IBusStateSnapshot busState, IMessage message)
        {
            if(!(message is IQuery))
            {
                return true;
            }

            if(busState.LocallyQueued.Concat(busState.LocallyExecuting).Any(dispatching => dispatching is ICommand || dispatching is IEvent))
            {
                return false;
            }

            return true;
        }
    }
}
