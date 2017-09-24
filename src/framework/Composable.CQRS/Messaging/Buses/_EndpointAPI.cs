using System;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    interface IEndpoint
    {
        IServiceLocator ServiceLocator { get; }
    }

    interface IEndpointBuilder
    {
        IDependencyInjectionContainer Container { get; }
        IMessageHandlerRegistrar MessageHandlerRegistrar { get; }
    }

    interface IEndpointHost : IDisposable
    {
        IEndpoint RegisterEndpoint(Action<IEndpointBuilder> setup);
    }

    interface ITestingEndpointHost : IEndpointHost { }
}
