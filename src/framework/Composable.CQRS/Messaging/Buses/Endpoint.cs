using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;

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
        public EndPointAddress Address => Inbox.Address;

        IServiceBusControl BusControl => ServiceLocator.Resolve<IServiceBusControl>();

        public void Start()
        {
            Assert.State.Assert(!IsRunning);

            IsRunning = true;

            RunSanityChecks();

            BusControl.Start();
        }

        void RunSanityChecks()
        {
            AssertAllTypesNeedingMappingsAreMapped();
        }

        //todo: figure out how to do this sanely
        void AssertAllTypesNeedingMappingsAreMapped()
        {
        }

        public void Stop()
        {
            Assert.State.Assert(IsRunning);
            IsRunning = false;
            BusControl.Stop();
        }

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => GlobalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);

        public void Dispose()
        {
            if(IsRunning)
            {
                Stop();
            }
            ServiceLocator.Dispose();
        }

        IGlobalBusStateTracker GlobalStateTracker => ServiceLocator.Resolve<IGlobalBusStateTracker>();
        IInbox Inbox => ServiceLocator.Resolve<IInbox>();
    }
}
