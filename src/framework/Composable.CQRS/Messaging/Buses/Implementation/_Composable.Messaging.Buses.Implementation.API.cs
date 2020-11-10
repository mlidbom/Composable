using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore;

namespace Composable.Messaging.Buses.Implementation
{
    interface IEventStoreEventPublisher
    {
        void Publish(IAggregateEvent anEvent);
    }

    interface IInbox
    {
        EndPointAddress Address { get; }
        Task StartAsync();
        void Stop();
    }

    interface IOutbox
    {
        Task StartAsync();
        void PublishTransactionally(IExactlyOnceEvent exactlyOnceEvent);
        void SendTransactionally(IExactlyOnceCommand exactlyOnceCommand);
    }

    interface ITransport
    {
        Task ConnectAsync(EndPointAddress remoteEndpoint);
        void Stop();

        IInboxConnection ConnectionToHandlerFor(IRemotableCommand command);
        IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(IExactlyOnceEvent @event);

        Task PostAsync(IAtMostOnceHypermediaCommand command);
        Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command);
        Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query);
    }

    interface IInboxConnection : IDisposable
    {
        MessageTypes.Internal.EndpointInformation EndpointInformation { get; }
        Task SendAsync(IExactlyOnceEvent @event);
        Task SendAsync(IExactlyOnceCommand command);

        Task PostAsync(IAtMostOnceHypermediaCommand command);
        Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command);
        Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query);
    }

    interface IEndpointRegistry
    {
        IEnumerable<EndPointAddress> ServerEndpoints { get; }
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(ICommand message);

        Action<ICommand> GetCommandHandler(Type commandType);
        Func<ICommand, object> GetCommandHandlerWithReturnValue(Type commandType);
        Func<IQuery<object>, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<IEvent>> GetEventHandlers(Type eventType);

        Func<IStrictlyLocalQuery<TQuery, TResult>, TResult> GetQueryHandler<TQuery, TResult>(IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : IStrictlyLocalQuery<TQuery, TResult>;

        Func<ICommand<TResult>, TResult> GetCommandHandler<TResult>(ICommand<TResult> command);

        IEventDispatcher<IEvent> CreateEventDispatcher();

        ISet<TypeId> HandledRemoteMessageTypeIds();
    }
}