using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore;

namespace Composable.Messaging.Buses.Implementation
{
    interface IEventstoreEventPublisher
    {
        void Publish(IAggregateEvent anEvent);
    }

    interface IInterprocessTransport
    {
        void Stop();
        Task StartAsync();
        Task ConnectAsync(EndPointAddress remoteEndpoint);

        void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.IEvent exactlyOnceEvent);
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

    interface IMessageHandlerRegistry
    {
        IReadOnlyList<Type> GetTypesNeedingMappings();

        Action<object> GetCommandHandler(BusApi.ICommand message);

        Func<BusApi.ICommand, object> GetCommandHandler(Type commandType);
        Func<BusApi.IQuery, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<BusApi.IEvent>> GetEventHandlers(Type eventType);

        Func<BusApi.IQuery<TResult>, TResult> GetQueryHandler<TResult>(BusApi.IQuery<TResult> query);

        Func<BusApi.ICommand<TResult>, TResult> GetCommandHandler<TResult>(BusApi.ICommand<TResult> command);

        IEventDispatcher<BusApi.IEvent> CreateEventDispatcher();

        ISet<TypeId> HandledRemoteMessageTypeIds();
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        Task StartAsync();
        void Stop();
    }
}