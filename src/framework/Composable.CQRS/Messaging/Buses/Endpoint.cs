using System;
using System.ComponentModel;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        public Endpoint(IServiceLocator serviceLocator) => ServiceLocator = serviceLocator;
        public IServiceLocator ServiceLocator { get; }

        public void Start() => Transport.Start();
        public void Stop() => Transport.Stop();
        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => GlobalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);
        public void Dispose() => ServiceLocator.Dispose();

        IGlobalBusStrateTracker GlobalStateTracker => ServiceLocator.Resolve<IGlobalBusStrateTracker>();
        IServiceBus Bus => ServiceLocator.Resolve<IServiceBus>();
        IInterprocessTransport Transport => ServiceLocator.Resolve<IInterprocessTransport>();
    }
}
