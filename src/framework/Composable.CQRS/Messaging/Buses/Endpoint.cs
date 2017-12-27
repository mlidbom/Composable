using System;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        bool _running;
        public Endpoint(IServiceLocator serviceLocator) => ServiceLocator = serviceLocator;
        public IServiceLocator ServiceLocator { get; }
        public string Address => Inbox.Address;

        IServiceBus Bus => ServiceLocator.Resolve<IServiceBus>();

        public void Start()
        {
            Contract.State.Assert(!_running);
            _running = true;

            Bus.Start();
        }


        public void Stop()
        {
            Contract.State.Assert(_running);
            _running = false;
            Bus.Stop();
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
