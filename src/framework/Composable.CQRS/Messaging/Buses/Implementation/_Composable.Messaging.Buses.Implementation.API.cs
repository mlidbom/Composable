using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    interface IServiceBus
    {
        void Start();
        void Stop();
    }

    interface IInterprocessTransport
    {
        void Stop();
        void Start();
        void DispatchIfTransactionCommits(ITransactionalExactlyOnceDeliveryEvent message);
        void DispatchIfTransactionCommits(ITransactionalExactlyOnceDeliveryCommand command);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> command);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void DispatchIfTransactionCommits(ITransactionalExactlyOnceDeliveryEvent @event);
        void DispatchIfTransactionCommits(ITransactionalExactlyOnceDeliveryCommand command);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query);
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        void Start();
        void Stop();
    }
}