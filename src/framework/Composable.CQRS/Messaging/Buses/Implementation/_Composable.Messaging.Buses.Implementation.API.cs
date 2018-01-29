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

        void DispatchIfTransactionCommits(BusApi.RemoteSupport.ExactlyOnce.IEvent exectlyOnceEvent);
        void DispatchIfTransactionCommits(BusApi.RemoteSupport.ExactlyOnce.ICommand exactlyOnceCommand);
        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(BusApi.RemoteSupport.ExactlyOnce.ICommand<TCommandResult> exactlyOnceCommand);

        Task DispatchAsync(BusApi.RemoteSupport.AtMostOnce.ICommand atMostOnceCommand);
        Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand);

        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TQueryResult> query);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void DispatchIfTransactionCommits(BusApi.RemoteSupport.ExactlyOnce.IEvent @event);
        void DispatchIfTransactionCommits(BusApi.RemoteSupport.ExactlyOnce.ICommand command);
        Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TCommandResult> command);
        Task DispatchAsync(BusApi.RemoteSupport.AtMostOnce.ICommand command);

        Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(BusApi.RemoteSupport.ExactlyOnce.ICommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TQueryResult> query);
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        void Start();
        void Stop();
    }
}