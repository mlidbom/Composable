using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging.Events;
using Composable.System.Threading.ResourceAccess;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    ///<summary>Dispatches messages within a process.</summary>
    interface IInProcessServiceBus
    {
        void Publish(IEvent anEvent);
        TResult Get<TResult>(IQuery<TResult> query) where TResult : IQueryResult;
        TResult Send<TResult>(ICommand<TResult> command) where TResult : IMessage;
        void Send(ICommand message);
    }

    ///<summary>Dispatches messages between processes.</summary>
    public interface IServiceBus : IDisposable
    {
        void SendAtTime(DateTime sendAt, ICommand message);
        void Publish(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult;
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        void Send(ICommand command);
        Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage;
        void Start();
        void Stop();
    }

    public static class ServiceBusExtensions
    {
        public static IApiNavigator<TReturnResource> Get<TReturnResource>(this IServiceBus @this, IQuery<TReturnResource> createQuery) where TReturnResource : IQueryResult
            => new ApiNavigator<TReturnResource>(@this, () => @this.QueryAsync(createQuery));

        public static IApiNavigator<TCommandResult> Post<TCommandResult>(this IServiceBus @this, ICommand<TCommandResult> command) where TCommandResult : IMessage
            => new ApiNavigator<TCommandResult>(@this, () => @this.SendAsync(command));

    }

    public interface IMessageSpy
    {
        IEnumerable<IMessage> DispatchedMessages { get; }
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(ICommand message);

        Func<IQuery<TResult>, TResult> GetQueryHandler<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        Func<ICommand<TResult>, TResult> GetCommandHandler<TResult>(ICommand<TResult> command) where TResult : IMessage;

        IEventDispatcher<IEvent> CreateEventDispatcher();
    }

    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
        IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : ICommand<TResult>
                                                                                                  where TResult : IMessage;
        IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult>
                                                                                                      where TResult : IQueryResult;
    }

    interface IEndpoint : IDisposable
    {
        IServiceLocator ServiceLocator { get; }
        void Start();
        void Stop();
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    }

    interface IEndpointBuilder
    {
        IDependencyInjectionContainer Container { get; }
        MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
    }

    interface IEndpointHost : IDisposable
    {
        IEndpoint RegisterAndStartEndpoint(string name, Action<IEndpointBuilder> setup);
        void Stop();
    }

    interface ITestingEndpointHost : IEndpointHost
    {
        void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride);

        IServiceBus ClientBus { get; }
        IApiNavigator ClientNavigator { get; }
    }

    interface IGlobalBusStateSnapshot
    {
        IReadOnlyList<IQueuedMessageInformation> InflightMessages { get; }
        IReadOnlyList<IQueuedMessage> LocallyQueuedMessages { get; }
        IReadOnlyList<IQueuedMessage> LocallyExecutingMessages { get; }
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
        IReadOnlyList<Exception> GetExceptionsFor(IServiceBus bus);
        IExclusiveResourceAccessGuard ResourceGuard { get; }

        IQueuedMessage AwaitDispatchableMessage(IServiceBus bus, IReadOnlyList<IMessageDispatchingRule> dispatchingRules);

        void EnqueueMessageTask(IServiceBus bus, IMessage message, Action messageTask);
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    }

    public interface IApiNavigator
    {
        IApiNavigator<TReturnResource> Get<TReturnResource>(IQuery<TReturnResource> createQuery) where TReturnResource : IQueryResult;
        IApiNavigator Execute(ICommand createCommand);
    }

    public interface IApiNavigator<TCurrentResource>
    {
        IApiNavigator<TReturnResource> Get<TReturnResource>(Func<TCurrentResource, IQuery<TReturnResource>> selectQuery) where TReturnResource : IQueryResult;
        IApiNavigator<TReturnResource> Post<TReturnResource>(Func<TCurrentResource, ICommand<TReturnResource>> selectCommand) where TReturnResource : IMessage;
        Task<TCurrentResource> ExecuteNavigationAsync();
        TCurrentResource ExecuteNavigation();
    }
}
