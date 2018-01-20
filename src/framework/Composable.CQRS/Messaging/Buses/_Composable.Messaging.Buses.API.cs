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
    ///<summary>Dispatches messages within a process.</summary>
    public interface IInProcessServiceBus
    {
        void Publish(ITransactionalExactlyOnceDeliveryEvent anEvent);
        TResult QueryInProcess<TResult>(IQuery<TResult> query);
        TResult SendInProcess<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command);
        void SendInProcess(ITransactionalExactlyOnceDeliveryCommand message);
    }


    ///<summary>Dispatches messages between processes.</summary>
    public interface IServiceBus : IDisposable
    {
        void Send(ITransactionalExactlyOnceDeliveryCommand command);
        void SendAtTime(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand command);
        TResult Send<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command);
        Task<TResult> SendAsync<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command);

        void Publish(ITransactionalExactlyOnceDeliveryEvent @event);

        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);
        TResult Query<TResult>(IQuery<TResult> query);
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(ITransactionalExactlyOnceDeliveryCommand message);

        bool TryGetCommandHandler(ITransactionalExactlyOnceDeliveryCommand message, out Action<object> handler);

        bool TryGetCommandHandlerWithResult(ITransactionalExactlyOnceDeliveryCommand message, out Func<object, object> handler);

        Func<ITransactionalExactlyOnceDeliveryCommand, object> GetCommandHandler(Type commandType);
        Func<IQuery, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<ITransactionalExactlyOnceDeliveryEvent>> GetEventHandlers(Type eventType);

        Func<IQuery<TResult>, TResult> GetQueryHandler<TResult>(IQuery<TResult> query);

        Func<ITransactionalExactlyOnceDeliveryCommand<TResult>, TResult> GetCommandHandler<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command);

        IEventDispatcher<ITransactionalExactlyOnceDeliveryEvent> CreateEventDispatcher();

        ISet<TypeId> HandledTypeIds();
    }

    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : ITransactionalExactlyOnceDeliveryEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ITransactionalExactlyOnceDeliveryCommand;
        IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : ITransactionalExactlyOnceDeliveryCommand<TResult>;
        IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult>;
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
            Contract.Argument.Assert(guidValue != Guid.Empty);
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

        IServiceBus ClientBus { get; }
    }

    interface IMessageDispatchingRule
    {
        bool CanBeDispatched(IReadOnlyList<IMessage> executingMessages, IMessage message);
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

    interface ICreateMyOwnResultQuery<TResult> : IQuery<TResult>
    {
        TResult CreateResult();
    }

    public class SelfGeneratingResourceQuery<TResource> : ICreateMyOwnResultQuery<TResource> where TResource : new()
    {
        SelfGeneratingResourceQuery() {}
        public static SelfGeneratingResourceQuery<TResource> Instance = new SelfGeneratingResourceQuery<TResource>();
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
