using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    interface IServiceBusControl
    {
        void Start();
        void Stop();
    }

    interface IInterprocessTransport
    {
        void Stop();
        void Start();
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.IEvent message);
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.ICommand command);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(BusApi.Remote.ExactlyOnce.ICommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.IQuery<TQueryResult> command);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.IEvent @event);
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.ICommand command);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(BusApi.Remote.ExactlyOnce.ICommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.IQuery<TQueryResult> query);
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        void Start();
        void Stop();
    }
}