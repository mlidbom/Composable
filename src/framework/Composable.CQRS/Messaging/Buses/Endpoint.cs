using System;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        public bool IsRunning { get; private set; }
        public Endpoint(IServiceLocator serviceLocator, EndpointId id, string name)
        {
            Assert.Argument.Assert(serviceLocator != null, id != null);
            ServiceLocator = serviceLocator;
            Id = id;
            Name = name;
        }
        public EndpointId Id { get; }
        public string Name { get; }
        public IServiceLocator ServiceLocator { get; }

        public EndPointAddress Address => _inbox.Address;
        IGlobalBusStateTracker _globalStateTracker;
        IInbox _inbox;

        IServiceBusControl BusControl => ServiceLocator.Resolve<IServiceBusControl>();

        public void Start()
        {
            Assert.State.Assert(!IsRunning);

            IsRunning = true;

            _globalStateTracker = ServiceLocator.Resolve<IGlobalBusStateTracker>();
            _inbox = ServiceLocator.Resolve<IInbox>();

            RunSanityChecks();

            BusControl.Start();
        }

        static void RunSanityChecks()
        {
            AssertAllTypesNeedingMappingsAreMapped();
        }

        //todo: figure out how to do this sanely
        static void AssertAllTypesNeedingMappingsAreMapped()
        {
        }

        public void Stop()
        {
            Assert.State.Assert(IsRunning);
            IsRunning = false;
            BusControl.Stop();
        }

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker?.AwaitNoMessagesInFlight(timeoutOverride);

        public void Dispose()
        {
            if(IsRunning) Stop();
            ServiceLocator.Dispose();
        }
    }
}
