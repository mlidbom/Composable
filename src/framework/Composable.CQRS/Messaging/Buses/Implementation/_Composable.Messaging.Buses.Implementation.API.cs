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

        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.IEvent exectlyOnceEvent);
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.ICommand exactlyOnceCommand);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(BusApi.Remote.ExactlyOnce.ICommand<TCommandResult> exactlyOnceCommand);

        Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.Remote.ExactlyOnce.ICommand<TCommandResult> exactlyOnceCommand);

        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.Remote.NonTransactional.IQuery<TQueryResult> query);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.IEvent @event);
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.ICommand command);
        void Dispatch<TResult>(BusApi.Remote.AtMostOnce.ICommand<TResult> command);

        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(BusApi.Remote.ExactlyOnce.ICommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.Remote.NonTransactional.IQuery<TQueryResult> query);
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        void Start();
        void Stop();
    }
}