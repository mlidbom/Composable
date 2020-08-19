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
        void PublishTransactionally(MessageTypes.Remotable.ExactlyOnce.IEvent exactlyOnceEvent);
        void SendTransactionally(MessageTypes.Remotable.ExactlyOnce.ICommand exactlyOnceCommand);
    }

    interface ITransport
    {
        Task ConnectAsync(EndPointAddress remoteEndpoint);
        void Stop();

        IInboxConnection ConnectionToHandlerFor(MessageTypes.Remotable.ICommand command);
        IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(MessageTypes.Remotable.ExactlyOnce.IEvent @event);

        Task PostAsync(MessageTypes.Remotable.AtMostOnce.IAtMostOnceHypermediaCommand command);
        Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.IAtMostOnceCommand<TCommandResult> command);
        Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query);
    }

    interface IInboxConnection : IDisposable
    {
        MessageTypes.Internal.EndpointInformation EndpointInformation { get; }
        Task SendAsync(MessageTypes.Remotable.ExactlyOnce.IEvent @event);
        Task SendAsync(MessageTypes.Remotable.ExactlyOnce.ICommand command);

        Task PostAsync(MessageTypes.Remotable.AtMostOnce.IAtMostOnceHypermediaCommand command);
        Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.IAtMostOnceCommand<TCommandResult> command);
        Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query);
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

        Func<MessageTypes.StrictlyLocal.IQuery<TQuery, TResult>, TResult> GetQueryHandler<TQuery, TResult>(MessageTypes.StrictlyLocal.IQuery<TQuery, TResult> query) where TQuery : MessageTypes.StrictlyLocal.IQuery<TQuery, TResult>;

        Func<MessageTypes.ICommand<TResult>, TResult> GetCommandHandler<TResult>(MessageTypes.ICommand<TResult> command);

        IEventDispatcher<MessageTypes.IEvent> CreateEventDispatcher();

        ISet<TypeId> HandledRemoteMessageTypeIds();
    }
}