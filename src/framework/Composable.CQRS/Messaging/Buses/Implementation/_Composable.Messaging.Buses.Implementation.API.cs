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
        void PublishTransactionally(MessageTypes.IExactlyOnceEvent exactlyOnceEvent);
        void SendTransactionally(MessageTypes.IExactlyOnceCommand exactlyOnceCommand);
    }

    interface ITransport
    {
        Task ConnectAsync(EndPointAddress remoteEndpoint);
        void Stop();

        IInboxConnection ConnectionToHandlerFor(MessageTypes.IRemotableCommand command);
        IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(MessageTypes.IExactlyOnceEvent @event);

        Task PostAsync(MessageTypes.IAtMostOnceHypermediaCommand command);
        Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.IAtMostOnceCommand<TCommandResult> command);
        Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.IRemotableQuery<TQueryResult> query);
    }

    interface IInboxConnection : IDisposable
    {
        MessageTypes.Internal.EndpointInformation EndpointInformation { get; }
        Task SendAsync(MessageTypes.IExactlyOnceEvent @event);
        Task SendAsync(MessageTypes.IExactlyOnceCommand command);

        Task PostAsync(MessageTypes.IAtMostOnceHypermediaCommand command);
        Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.IAtMostOnceCommand<TCommandResult> command);
        Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.IRemotableQuery<TQueryResult> query);
    }

    interface IEndpointRegistry
    {
        IEnumerable<EndPointAddress> ServerEndpoints { get; }
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(MessageTypes.ICommand message);

        Action<MessageTypes.ICommand> GetCommandHandler(Type commandType);
        Func<MessageTypes.ICommand, object> GetCommandHandlerWithReturnValue(Type commandType);
        Func<MessageTypes.IQuery<object>, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<MessageTypes.IEvent>> GetEventHandlers(Type eventType);

        Func<MessageTypes.IStrictlyLocalQuery<TQuery, TResult>, TResult> GetQueryHandler<TQuery, TResult>(MessageTypes.IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : MessageTypes.IStrictlyLocalQuery<TQuery, TResult>;

        Func<MessageTypes.ICommand<TResult>, TResult> GetCommandHandler<TResult>(MessageTypes.ICommand<TResult> command);

        IEventDispatcher<MessageTypes.IEvent> CreateEventDispatcher();

        ISet<TypeId> HandledRemoteMessageTypeIds();
    }
}