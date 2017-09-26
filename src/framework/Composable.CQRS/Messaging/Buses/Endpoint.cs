using System;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        public Endpoint(IServiceLocator serviceLocator) => ServiceLocator = serviceLocator;
        public IServiceLocator ServiceLocator { get; }

        public void Start() => Bus.Start();
        public void Stop() => Bus.Stop();
        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => GlobalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);
        public void Dispose() => Bus.Dispose();

        IGlobalBusStrateTracker GlobalStateTracker => ServiceLocator.Resolve<IGlobalBusStrateTracker>();
        IServiceBus Bus => ServiceLocator.Resolve<IServiceBus>();
    }
}
