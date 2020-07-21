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
        void Stop();
        Task StartAsync();
        Task ConnectAsync(EndPointAddress remoteEndpoint);

        void PublishIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.IEvent exactlyOnceEvent);
        void SendIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.ICommand exactlyOnceCommand);

        //Urgent: These hypermedia client methods should be moved to another abstraction that does not need persistence etc. A client, not an endpoint.
        Task PostAsync(MessageTypes.Remotable.AtMostOnce.ICommand atMostOnceCommand);
        Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand);
        Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query);
    }

    interface IInboxConnection : IDisposable
    {
        void SendIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.IEvent @event);
        void SendIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.ICommand command);

        //Urgent: These hypermedia client methods should be moved to another abstraction that does not need persistence etc. A client, not an endpoint.
        Task PostAsync(MessageTypes.Remotable.AtMostOnce.ICommand command);
        Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TCommandResult> command);
        Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query);
    }

    interface IEndpointRegistry
    {
        IEnumerable<EndPointAddress> ServerEndpoints { get; }
    }

    interface IMessageHandlerRegistry
    {
        IReadOnlyList<Type> GetTypesNeedingMappings();

        Action<object> GetCommandHandler(MessageTypes.ICommand message);

        Func<MessageTypes.ICommand, object> GetCommandHandler(Type commandType);
        Func<MessageTypes.IQuery, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<MessageTypes.IEvent>> GetEventHandlers(Type eventType);

        Func<MessageTypes.IQuery<TResult>, TResult> GetQueryHandler<TResult>(MessageTypes.IQuery<TResult> query);

        Func<MessageTypes.ICommand<TResult>, TResult> GetCommandHandler<TResult>(MessageTypes.ICommand<TResult> command);

        IEventDispatcher<MessageTypes.IEvent> CreateEventDispatcher();

        ISet<TypeId> HandledRemoteMessageTypeIds();
    }
}