using System;
using System.Collections.Generic;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
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
        IMessageHandlerRegistrar MessageHandlerRegistrar { get; }
    }

    interface IEndpointHost : IDisposable
    {
        IEndpoint RegisterEndpoint(Action<IEndpointBuilder> setup);
        void Start();
        void Stop();
    }

    interface ITestingEndpointHost : IEndpointHost
    {
        void WaitForEndpointsToBeAtRest();
    }

    interface IBusStateSnapshot
    {
        IReadOnlyList<IMessage> LocallyQueued { get; }
        IReadOnlyList<IMessage> LocallyExecuting { get; }
    }

    interface IMessageDispatchingRule
    {
        bool CanBeDispatched(IBusStateSnapshot busState, IMessage message);
    }
}
