using Composable.CQRS.EventSourcing;

namespace Composable.Messaging.Buses
{
    public interface IInProcessServiceBus
    {
        void Publish(IEvent anEvent);
        TResult Get<TResult>(IQuery<TResult> query) where TResult : IQueryResult; 
        void Send(ICommand message);
    }
}
