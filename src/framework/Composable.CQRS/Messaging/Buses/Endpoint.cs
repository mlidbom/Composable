using System;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        bool _running;
        public Endpoint(IServiceLocator serviceLocator, EndpointId id, string name)
        {
            Contract.Argument.Assert(serviceLocator != null, id != null);
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
            Contract.State.Assert(!_running);

            _running = true;

            BusControl.Start();
        }


        public void Stop()
        {
            Contract.State.Assert(_running);
            _running = false;
            BusControl.Stop();
        }

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => GlobalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);

        public void Dispose()
        {
            if(_running)
            {
                Stop();
            }
            ServiceLocator.Dispose();
        }

        IGlobalBusStateTracker GlobalStateTracker => ServiceLocator.Resolve<IGlobalBusStateTracker>();
        IInbox Inbox => ServiceLocator.Resolve<IInbox>();
    }
}
