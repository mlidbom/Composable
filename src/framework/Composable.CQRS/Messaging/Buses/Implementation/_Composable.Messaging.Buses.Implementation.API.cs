using System;
using System.Collections.Generic;
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
        Task DispatchAsync(IEvent message);
        Task DispatchAsync(IDomainCommand command);
        Task<Task<TCommandResult>> DispatchAsyncAsync<TCommandResult>(IDomainCommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> command);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void Dispatch(IEvent @event);
        void Dispatch(IDomainCommand command);
        Task<TCommandResult> DispatchAsync<TCommandResult>(IDomainCommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query);
    }

    interface IInbox : IDisposable
    {
        IReadOnlyList<Exception> ThrownExceptions { get; }
        EndPointAddress Address { get; }
        void Start();
        void Stop();
    }
}