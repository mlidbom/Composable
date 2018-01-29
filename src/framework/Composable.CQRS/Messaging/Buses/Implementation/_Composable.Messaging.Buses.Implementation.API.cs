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

        void Dispatch(BusApi.Remote.AtMostOnce.ICommand atMostOnceCommand);
        Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.Remote.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand);

        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.Remote.NonTransactional.IQuery<TQueryResult> query);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.IEvent @event);
        void DispatchIfTransactionCommits(BusApi.Remote.ExactlyOnce.ICommand command);
        Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.Remote.AtMostOnce.ICommand<TCommandResult> command);
        Task DispatchAsync(BusApi.Remote.AtMostOnce.ICommand command);

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