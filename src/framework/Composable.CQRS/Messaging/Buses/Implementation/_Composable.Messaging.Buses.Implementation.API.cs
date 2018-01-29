using System;
using System.Threading.Tasks;
using Composable.Persistence.EventStore;

namespace Composable.Messaging.Buses.Implementation
{
    interface IServiceBusControl
    {
        void Start();
        void Stop();
    }

    interface IEventstoreEventPublisher
    {
        void Publish(IAggregateEvent anEvent);
    }

    interface IInterprocessTransport
    {
        void Stop();
        void Start();
        void Connect(IEndpoint endpoint);

        void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.IEvent exectlyOnceEvent);
        void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.ICommand exactlyOnceCommand);

        Task DispatchAsync(BusApi.Remotable.AtMostOnce.ICommand atMostOnceCommand);
        Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.Remotable.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand);

        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.Remotable.NonTransactional.IQuery<TQueryResult> query);
    }

    interface IClientConnection : IDisposable
    {
        void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.IEvent @event);
        void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.ICommand command);

        Task DispatchAsync(BusApi.Remotable.AtMostOnce.ICommand command);
        Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.Remotable.AtMostOnce.ICommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.Remotable.NonTransactional.IQuery<TQueryResult> query);
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        void Start();
        void Stop();
    }
}