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
        void DispatchIfTransactionCommits(IExactlyOnceEvent message);
        void DispatchIfTransactionCommits(IExactlyOnceCommand command);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(IExactlyOnceCommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> command);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void DispatchIfTransactionCommits(IExactlyOnceEvent @event);
        void DispatchIfTransactionCommits(IExactlyOnceCommand command);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(IExactlyOnceCommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query);
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        void Start();
        void Stop();
    }
}