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
        void Publish(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query);
        TResult Send<TResult>(IDomainCommand<TResult> command);
        void Send(IDomainCommand message);
    }

    public interface ISimpleServiceBus
    {
        void Publish(IEvent @event);
        void Send(IDomainCommand command);

        void SendAtTime(DateTime sendAt, IDomainCommand command);

        TResult Send<TResult>(IDomainCommand<TResult> command);
        TResult Query<TResult>(IQuery<TResult> query);

        Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command);
    }

    ///<summary>Dispatches messages between processes.</summary>
    public interface IServiceBus : ISimpleServiceBus, IDisposable
    {
        Task SendAtTimeAsync(DateTime sendAt, IDomainCommand command);
        Task PublishAsync(IEvent anEvent);
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);

        Task SendAsync(IDomainCommand command);
        Task<Task<TResult>> SendAsyncAsync<TResult>(IDomainCommand<TResult> command);
    }

    public interface IMessageSpy
    {
        IEnumerable<object> DispatchedMessages { get; }
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(IDomainCommand message);

        Func<IDomainCommand, object> GetCommandHandler(Type commandType);
        Func<IQuery, object> GetQueryHandler(Type commandType);
        IReadOnlyList<Action<IEvent>> GetEventHandlers(Type eventType);

        Func<IQuery<TResult>, TResult> GetQueryHandler<TResult>(IQuery<TResult> query);

        Func<IDomainCommand<TResult>, TResult> GetCommandHandler<TResult>(IDomainCommand<TResult> command);

        IEventDispatcher<IEvent> CreateEventDispatcher();

        ISet<Type> HandledTypes();
    }

    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : IDomainCommand;
        IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : IDomainCommand<TResult>;
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
        IApiNavigator<TCommandResult> Post<TCommandResult>(IDomainCommand<TCommandResult> command);
    }

    public interface IApiNavigator<TCurrentResource>
    {
        IApiNavigator<TReturnResource> Get<TReturnResource>(Func<TCurrentResource, IQuery<TReturnResource>> selectQuery);
        IApiNavigator<TReturnResource> Post<TReturnResource>(Func<TCurrentResource, IDomainCommand<TReturnResource>> selectCommand);
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
