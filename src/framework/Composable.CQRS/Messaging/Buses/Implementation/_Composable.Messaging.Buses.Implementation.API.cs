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
        void DispatchIfTransactionCommits(MessagingApi.Remote.ExactlyOnce.IEvent message);
        void DispatchIfTransactionCommits(MessagingApi.Remote.ExactlyOnce.ICommand command);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(MessagingApi.Remote.ExactlyOnce.ICommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(MessagingApi.IQuery<TQueryResult> command);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void DispatchIfTransactionCommits(MessagingApi.Remote.ExactlyOnce.IEvent @event);
        void DispatchIfTransactionCommits(MessagingApi.Remote.ExactlyOnce.ICommand command);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(MessagingApi.Remote.ExactlyOnce.ICommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(MessagingApi.IQuery<TQueryResult> query);
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        void Start();
        void Stop();
    }
}