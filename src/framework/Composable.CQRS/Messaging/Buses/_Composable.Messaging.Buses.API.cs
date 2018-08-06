﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DDD;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Newtonsoft.Json;

namespace Composable.Messaging.Buses
{
    ///<summary>Dispatches messages within a process.</summary>
    public interface ILocalApiNavigatorSession
    {
        ///<summary>Synchronously executes local handler for <paramref name="query"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TResult>(BusApi.StrictlyLocal.IQuery<TResult> query);

        ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        TResult Execute<TResult>(BusApi.StrictlyLocal.ICommand<TResult> command);

        ///<summary>Synchronously executes local handler for <paramref name="command"/>. The handler takes part in the active transaction and guarantees consistent results within a transaction.</summary>
        void Execute(BusApi.StrictlyLocal.ICommand command);
    }


    public interface IRemoteApiNavigatorSession
    {
        Task PostAsync(BusApi.Remotable.AtMostOnce.ICommand command);
        void Post(BusApi.Remotable.AtMostOnce.ICommand command);

        Task<TResult> PostAsync<TResult>(BusApi.Remotable.AtMostOnce.ICommand<TResult> command);
        TResult Post<TResult>(BusApi.Remotable.AtMostOnce.ICommand<TResult> command);

        ///<summary>Gets the result of a handler somewhere on the bus handling the <paramref name="query"/></summary>
        Task<TResult> GetAsync<TResult>(BusApi.Remotable.NonTransactional.IQuery<TResult> query);

        ///<summary>Synchronous wrapper for: <see cref="GetAsync{TResult}"/>.</summary>
        TResult Get<TResult>(BusApi.Remotable.NonTransactional.IQuery<TResult> query);
    }

    public interface IIntegrationBusSession
    {
        ///<summary>Sends a command if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
        void Send(BusApi.Remotable.ExactlyOnce.ICommand command);

        ///<summary>Schedules a command to be sent later if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
        void ScheduleSend(DateTime sendAt, BusApi.Remotable.ExactlyOnce.ICommand command);
    }

    ///<summary>Dispatches messages between processes.</summary>
    public interface IServiceBusSession : ILocalApiNavigatorSession, IRemoteApiNavigatorSession, IIntegrationBusSession
    {
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
        bool IsRunning { get; }
        Task InitAsync();
        Task ConnectAsync();
        void Stop();
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    }

    public class EndpointId : ValueObject<EndpointId>
    {
        public Guid GuidValue { get; }
        [JsonConstructor]public EndpointId(Guid guidValue)
        {
            Assert.Argument.Assert(guidValue != Guid.Empty);
            GuidValue = guidValue;
        }
    }

    public interface IEndpointBuilder : IDisposable
    {
        IDependencyInjectionContainer Container { get; }
        ITypeMappingRegistar TypeMapper { get; }
        EndpointConfiguration Configuration { get; }
        MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
    }

    public interface IEndpointHost : IDisposable
    {
        IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);
        ///<summary>Registers a default client endpoint with a host. Can be called only once per host instance.</summary>
        IEndpoint RegisterClientEndpoint(Action<IEndpointBuilder> setup);
        Task StartAsync();
        void Start();
        void Stop();
    }

    public interface ITestingEndpointHost : IEndpointHost
    {
        IEndpoint RegisterTestingEndpoint(string name = null, EndpointId id = null, Action<IEndpointBuilder> setup = null);
        IEndpoint RegisterClientEndpointForRegisteredEndpoints();
        TException AssertThrown<TException>() where TException : Exception;
    }

    interface IExecutingMessagesSnapshot
    {
        IReadOnlyList<TransportMessage.InComing> AtMostOnceCommands { get; }
        IReadOnlyList<TransportMessage.InComing> ExactlyOnceCommands { get; }
        IReadOnlyList<TransportMessage.InComing> ExactlyOnceEvents { get; }
        IReadOnlyList<TransportMessage.InComing> ExecutingNonTransactionalQueries { get; }
    }

    interface IMessageDispatchingPolicy
    {

    }

    interface IMessageDispatchingRule
    {
        bool CanBeDispatched(IExecutingMessagesSnapshot executing, TransportMessage.InComing candidateMessage);
    }

    interface IGlobalBusStateTracker
    {
        IReadOnlyList<Exception> GetExceptions();

        void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage);
        void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
        void DoneWith(Guid message, Exception exception);
    }
}
