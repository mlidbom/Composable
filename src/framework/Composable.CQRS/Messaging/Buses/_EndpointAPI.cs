using System;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    interface IEndpoint : IDisposable
    {
        IServiceLocator ServiceLocator { get; }
        void Start();
        void Stop();
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

    interface ITestingEndpointHost : IEndpointHost { }
}
