using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        public Endpoint(IServiceLocator serviceLocator) => ServiceLocator = serviceLocator;
        public IServiceLocator ServiceLocator { get; }

        public void Start() => Bus.Start();
        public void Stop() => Bus.Stop();
        public void AwaitNoMessagesInFlight() => Bus.AwaitNoMessagesInFlight();
        public void Dispose() => Bus.Dispose();

        IInterProcessServiceBus Bus { get { return ServiceLocator.Resolve<IInterProcessServiceBus>(); } }
    }
}
