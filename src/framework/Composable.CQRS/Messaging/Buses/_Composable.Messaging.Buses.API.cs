using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DDD;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Events;
using Composable.Refactoring.Naming;

namespace Composable.Messaging.Buses
{
    public interface IEventstoreEventPublisher
    {
        void Publish(MessagingApi.Remote.ExactlyOnce.IExactlyOnceEvent anEvent);
    }

    ///<summary>Dispatches messages within a process.</summary>
    public interface ILocalServiceBusSession : IEventstoreEventPublisher
    {
        ///<summary>Syncronously executes local handler for <paramref name="query"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult GetLocal<TResult>(MessagingApi.IQuery<TResult> query);

        ///<summary>Syncronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult PostLocal<TResult>(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand<TResult> command);

        ///<summary>Syncronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        void PostLocal(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand command);
    }

    public interface IRemoteServiceBusSession
    {
        ///<summary>Sends a command if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
        void PostRemote(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand command);

        ///<summary>Schedules a command to be sent later if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
        void SchedulePostRemote(DateTime sendAt, MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand command);

        ///<summary>Syncronous wrapper for <see cref="PostRemoteAsync{TResult}"/>. Sends a command if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver. NOTE: The result CANNOT be awaited within the sending transaction since it has not been sent yet.</summary>
        TResult PostRemote<TResult>(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand<TResult> command);

        ///<summary>Sends a command if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver. NOTE: The result CANNOT be awaited within the sending transaction since it has not been sent yet.</summary>
        Task<TResult> PostRemoteAsync<TResult>(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand<TResult> command);

        ///<summary>Syncronous wrapper for: <see cref="GetRemoteAsync{TResult}"/>. Gets the result of a handler somewhere on the bus handling the <paramref name="query"/>.</summary>
        TResult GetRemote<TResult>(MessagingApi.IQuery<TResult> query);

        ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="query"/></summary>
        Task<TResult> GetRemoteAsync<TResult>(MessagingApi.IQuery<TResult> query);
    }

    ///<summary>Dispatches messages between processes.</summary>
    public interface IServiceBusSession : ILocalServiceBusSession, IRemoteServiceBusSession
    {
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand message);

        bool TryGetCommandHandler(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand message, out Action<object> handler);

        bool TryGetCommandHandlerWithResult(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand message, out Func<object, object> handler);

        Func<MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand, object> GetCommandHandler(Type commandType);
        Func<MessagingApi.IQuery, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<MessagingApi.Remote.ExactlyOnce.IExactlyOnceEvent>> GetEventHandlers(Type eventType);

        Func<MessagingApi.IQuery<TResult>, TResult> GetQueryHandler<TResult>(MessagingApi.IQuery<TResult> query);

        Func<MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand<TResult>, TResult> GetCommandHandler<TResult>(MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand<TResult> command);

        IEventDispatcher<MessagingApi.Remote.ExactlyOnce.IExactlyOnceEvent> CreateEventDispatcher();

        ISet<TypeId> HandledTypeIds();
    }

    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : MessagingApi.Remote.ExactlyOnce.IExactlyOnceEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand;
        IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand<TResult>;
        IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : MessagingApi.IQuery<TResult>;
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
        void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride = null);

        TException AssertThrown<TException>() where TException : Exception;

        IEndpoint ClientEndpoint { get; }

        IServiceBusSession ClientBusSession { get; }
    }

    interface IMessageDispatchingRule
    {
        bool CanBeDispatched(IReadOnlyList<MessagingApi.IMessage> executingMessages, MessagingApi.IMessage message);
    }

    interface IGlobalBusStateTracker
    {
        IReadOnlyList<Exception> GetExceptions();

        void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage);
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
        void DoneWith(Guid message, Exception exception);
    }

    //todo: Actually use this attribute to do caching.
    public class ClientCacheableAttribute : Attribute
    {
        public ClientCachingStrategy Strategy { get; }
        public TimeSpan ValidFor { get; }

        public ClientCacheableAttribute(ClientCachingStrategy strategy, int validForSeconds)
        {
            Strategy = strategy;
            ValidFor = TimeSpan.FromSeconds(validForSeconds);
        }
    }

    interface ICreateMyOwnResultQuery<TResult> : MessagingApi.IQuery<TResult>
    {
        TResult CreateResult();
    }

    public class SelfGeneratingResourceQuery<TResource> : ICreateMyOwnResultQuery<TResource> where TResource : new()
    {
        SelfGeneratingResourceQuery() {}
        public static readonly SelfGeneratingResourceQuery<TResource> Instance = new SelfGeneratingResourceQuery<TResource>();
        public TResource CreateResult() => new TResource();
    }

    ///<summary>Any query for this resource will be executed by simply calling the default constructor of the resource type</summary>
    public interface ISelfGeneratingResource{}

    public enum ClientCachingStrategy
    {
        ReuseSingletonInstance = 1,
        ReuseOriginalSerializedData = 2
    }
}
