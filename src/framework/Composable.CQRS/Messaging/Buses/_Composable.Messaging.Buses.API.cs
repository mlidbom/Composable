using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DDD;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;

namespace Composable.Messaging.Buses
{
    public interface IEventstoreEventPublisher
    {
        void Publish(IAggregateEvent anEvent);
    }

    ///<summary>Dispatches messages within a process.</summary>
    public interface ILocalApiBrowserSession : IEventstoreEventPublisher
    {
        ///<summary>Syncronously executes local handler for <paramref name="query"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TResult>(BusApi.StrictlyLocal.IQuery<TResult> query);

        ///<summary>Syncronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TResult>(BusApi.StrictlyLocal.ICommand<TResult> command);

        ///<summary>Syncronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        void Execute(BusApi.StrictlyLocal.ICommand command);
    }


    public interface IRemoteApiBrowserSession
    {
        Task PostAsync(BusApi.RemoteSupport.AtMostOnce.ICommand command);
        void Post(BusApi.RemoteSupport.AtMostOnce.ICommand command);

        Task<TResult> PostAsync<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command);
        TResult Post<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command);

        ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="query"/></summary>
        Task<TResult> GetAsync<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query);

        ///<summary>Syncronous wrapper for: <see cref="GetAsync{TResult}"/>.</summary>
        TResult Get<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query);
    }

    public interface IIntegrationBusSession
    {
        ///<summary>Sends a command if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
        void Send(BusApi.RemoteSupport.ExactlyOnce.ICommand command);

        ///<summary>Schedules a command to be sent later if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
        void ScheduleSend(DateTime sendAt, BusApi.RemoteSupport.ExactlyOnce.ICommand command);
    }

    ///<summary>Dispatches messages between processes.</summary>
    public interface ITransactionalMessageHandlerServiceBusSession : ILocalApiBrowserSession, IRemoteApiBrowserSession, IIntegrationBusSession
    {
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(BusApi.ICommand message);

        Func<BusApi.ICommand, object> GetCommandHandler(Type commandType);
        Func<BusApi.IQuery, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<BusApi.IEvent>> GetEventHandlers(Type eventType);

        Func<BusApi.IQuery<TResult>, TResult> GetQueryHandler<TResult>(BusApi.IQuery<TResult> query);

        Func<BusApi.ICommand<TResult>, TResult> GetCommandHandler<TResult>(BusApi.ICommand<TResult> command);

        IEventDispatcher<BusApi.IEvent> CreateEventDispatcher();

        ISet<TypeId> HandledRemoteMessageTypeIds();
    }

    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : BusApi.IEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : BusApi.ICommand;
        IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : BusApi.ICommand<TResult>;
        IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : BusApi.IQuery<TResult>;
    }

    public interface IEndpoint : IDisposable
    {
        EndpointId Id { get; }
        IServiceLocator ServiceLocator { get; }
        EndPointAddress Address { get; }
        void Start();
        void Stop();
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    }

    public class EndpointId : ValueObject<EndpointId>
    {
        public Guid GuidValue { get; }
        public EndpointId(Guid guidValue)
        {
            Assert.Argument.Assert(guidValue != Guid.Empty);
            GuidValue = guidValue;
        }
    }

    public interface IEndpointBuilder
    {
        IDependencyInjectionContainer Container { get; }
        ITypeMappingRegistar TypeMapper { get; }
        EndpointConfiguration Configuration { get; }
        MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
    }

    public interface IEndpointHost : IDisposable
    {
        IEndpoint RegisterAndStartEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);
        void Stop();
    }

    public interface ITestingEndpointHost : IEndpointHost
    {
        TException AssertThrown<TException>() where TException : Exception;

        IEndpoint ClientEndpoint { get; }

        ITransactionalMessageHandlerServiceBusSession ClientBusSession { get; }
    }

    interface IMessageDispatchingRule
    {
        bool CanBeDispatched(IReadOnlyList<BusApi.IMessage> executingMessages, BusApi.IMessage message);
    }

    interface IGlobalBusStateTracker
    {
        IReadOnlyList<Exception> GetExceptions();

        void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage);
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
        void DoneWith(Guid message, Exception exception);
    }
}
