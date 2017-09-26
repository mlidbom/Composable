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
        void Send(ICommand message);
    }

    ///<summary>Dispatches messages between processes.</summary>
    interface IServiceBus : IDisposable
    {
        void SendAtTime(DateTime sendAt, ICommand message);
        void Publish(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult;
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        void Send(ICommand command);
        void Start();
        void Stop();
    }

    public interface IMessageSpy
    {
        IEnumerable<IMessage> DispatchedMessages { get; }
    }

    interface IMessageHandlerRegistry
    {
        Action<object> GetCommandHandler(ICommand message);

        Func<IQuery<TResult>, TResult> GetQueryHandler<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        IEventDispatcher<IEvent> CreateEventDispatcher();
    }

    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
        IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult>
                                                                                                      where TResult : IQueryResult;
    }

    interface IEndpoint : IDisposable
    {
        IServiceLocator ServiceLocator { get; }
        void Start();
        void Stop();
        void AwaitNoMessagesInFlight();
    }

    interface IEndpointBuilder
    {
        IDependencyInjectionContainer Container { get; }
        MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandler { get; }
    }

    interface IEndpointHost : IDisposable
    {
        IEndpoint RegisterAndStartEndpoint(string name, Action<IEndpointBuilder> setup);
        void Stop();
    }

    interface ITestingEndpointHost : IEndpointHost
    {
        void WaitForEndpointsToBeAtRest();

        IServiceBus ClientBus { get; }
    }

    interface IGlobalBusStateSnapshot
    {
        IEnumerable<IInflightMessage> InflightMessages { get; }
    }

    interface IInflightMessage
    {
        IMessage Message { get; }
        [CanBeNull] IMessage TriggeringMessage { get; }
    }

    interface IMessageDispatchingRule
    {
        bool CanBeDispatched(IGlobalBusStateSnapshot busState, IMessage message);
    }

    interface IMessageDispatchingTracker
    {
        void Succeeded();
        void Failed();
    }

    interface IGlobalBusStrateTracker
    {
        IExclusiveResourceAccessGuard ResourceGuard { get; }
        IGlobalBusStateSnapshot CreateSnapshot();
        IMessageDispatchingTracker QueuedMessage(IMessage message, IMessage triggeringMessage);
        void AwaitNoMessagesInFlight();
    }
}
