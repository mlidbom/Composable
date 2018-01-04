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
        void Dispatch(ITransactionalExactlyOnceDeliveryEvent message);
        void Dispatch(ITransactionalExactlyOnceDeliveryCommand command);
        Task<TCommandResult> DispatchAsync<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> command);
        void Connect(IEndpoint endpoint);
    }

    interface IClientConnection : IDisposable
    {
        void Dispatch(ITransactionalExactlyOnceDeliveryEvent @event);
        void Dispatch(ITransactionalExactlyOnceDeliveryCommand command);
        Task<TCommandResult> DispatchAsync<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command);
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