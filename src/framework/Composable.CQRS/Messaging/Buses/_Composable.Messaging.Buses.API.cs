using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Events;

namespace Composable.Messaging.Buses
{
    ///<summary>Dispatches messages within a process.</summary>
    interface IInProcessServiceBus
    {
        void Publish(IEvent anEvent);
        TResult Get<TResult>(IQuery<TResult> query);
        TResult Send<TResult>(IDomainCommand<TResult> command);
        void Send(IDomainCommand message);
    }

    ///<summary>Dispatches messages between processes.</summary>
    public interface IServiceBus
    {
        void SendAtTime(DateTime sendAt, IDomainCommand command);
        void Publish(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query);
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);

        void Send(IDomainCommand command);
        Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command);
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
        IServiceLocator ServiceLocator { get; }
        string Address { get; } //todo: not a string!
        void Start();
        void Stop();
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    }

    public interface IEndpointBuilder
    {
        IDependencyInjectionContainer Container { get; }
        MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
    }

    public interface IEndpointHost : IDisposable
    {
        IEndpoint RegisterAndStartEndpoint(string name, Action<IEndpointBuilder> setup);
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

    interface IGlobalBusStrateTracker
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
        IApiNavigator Execute(IDomainCommand createCommand);
    }

    public interface IApiNavigator<TCurrentResource>
    {
        IApiNavigator<TReturnResource> Get<TReturnResource>(Func<TCurrentResource, IQuery<TReturnResource>> selectQuery);
        IApiNavigator<TReturnResource> Post<TReturnResource>(Func<TCurrentResource, IDomainCommand<TReturnResource>> selectCommand) where TReturnResource : IMessage;
        Task<TCurrentResource> ExecuteNavigationAsync();
        TCurrentResource ExecuteNavigation();
    }
}
