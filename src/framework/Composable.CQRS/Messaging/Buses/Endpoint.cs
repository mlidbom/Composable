using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        public Endpoint(IServiceLocator serviceLocator) => ServiceLocator = serviceLocator;
        public IServiceLocator ServiceLocator { get; }

        public void Start() => Bus.Start();
        public void Stop() => Bus.Stop();
        public void AwaitNoMessagesInFlight() => GlobalStateTracker.AwaitNoMessagesInFlight();
        public void Dispose() => Bus.Dispose();

        IGlobalBusStrateTracker GlobalStateTracker => ServiceLocator.Resolve<IGlobalBusStrateTracker>();
        IInterProcessServiceBus Bus => ServiceLocator.Resolve<IInterProcessServiceBus>();
    }
}
