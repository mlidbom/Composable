using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DDD;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Events;

namespace Composable.Messaging.Buses
{
    ///<summary>Dispatches messages within a process.</summary>
    public interface IInProcessServiceBus
    {
        void Publish(ITransactionalExactlyOnceDeliveryEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query);
        TResult Send<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command);
        void Send(ITransactionalExactlyOnceDeliveryCommand message);
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

    public interface IMessageSpy
    {
        IEnumerable<object> DispatchedMessages { get; }
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(ITransactionalExactlyOnceDeliveryCommand message);

        Func<ITransactionalExactlyOnceDeliveryCommand, object> GetCommandHandler(Type commandType);
        Func<IQuery, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<ITransactionalExactlyOnceDeliveryEvent>> GetEventHandlers(Type eventType);

        Func<IQuery<TResult>, TResult> GetQueryHandler<TResult>(IQuery<TResult> query);

        Func<ITransactionalExactlyOnceDeliveryCommand<TResult>, TResult> GetCommandHandler<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command);

        IEventDispatcher<ITransactionalExactlyOnceDeliveryEvent> CreateEventDispatcher();

        ISet<Type> HandledTypes();
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
        IApiNavigator ClientNavigator { get; }
    }

    interface IGlobalBusStateSnapshot
    {
        IReadOnlyList<IQueuedMessageInformation> MessagesQueuedForExecution { get; }
        IReadOnlyList<IQueuedMessage> MessagesQueuedForExecutionLocally { get; }
        IReadOnlyList<TransportMessage.OutGoing> InFlightMessages { get; }
    }

    interface IQueuedMessageInformation
    {
        IMessage Message { get; }
        bool IsExecuting { get; }
    }

    interface IMessageDispatchingRule
    {
        bool CanBeDispatched(IGlobalBusStateSnapshot busState, IQueuedMessageInformation queuedMessageInformation);
    }

    interface IQueuedMessage : IQueuedMessageInformation
    {
        void Run();
    }

    interface IGlobalBusStateTracker
    {
        IReadOnlyList<Exception> GetExceptionsFor(IInbox bus);

        IQueuedMessage AwaitDispatchableMessage(IInbox bus, IReadOnlyList<IMessageDispatchingRule> dispatchingRules);

        void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage, IMessage message);
        void EnqueueMessageTask(IInbox bus, TransportMessage.InComing message, Action messageTask);
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    }

    public interface IApiNavigator
    {
        IApiNavigator<TReturnResource> Get<TReturnResource>(IQuery<TReturnResource> createQuery);
        IApiNavigator<TCommandResult> Post<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command);
    }

    public interface IApiNavigator<TCurrentResource>
    {
        IApiNavigator<TReturnResource> Get<TReturnResource>(Func<TCurrentResource, IQuery<TReturnResource>> selectQuery);
        IApiNavigator<TReturnResource> Post<TReturnResource>(Func<TCurrentResource, ITransactionalExactlyOnceDeliveryCommand<TReturnResource>> selectCommand);
        Task<TCurrentResource> ExecuteAsync();
        TCurrentResource Execute();
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

    public enum ClientCachingStrategy
    {
        ReuseSingletonInstance = 1,
        ReuseOriginalSerializedData = 2 ,
        CreateNewInstanceWithDefaultConstructor =  3
    }
}
