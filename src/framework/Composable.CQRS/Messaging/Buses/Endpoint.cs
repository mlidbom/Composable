using System;
using System.ComponentModel;
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

        public void Start()
        {
            Contract.State.Assert(!_running);
            _running = true;
            Outbox.Start();
            Inbox.Start();
            InterprocessTransport.Connect(this);
        }


        public void Stop()
        {
            Contract.State.Assert(_running);
            _running = false;
            Outbox.Stop();
            Inbox.Stop();
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

        IGlobalBusStrateTracker GlobalStateTracker => ServiceLocator.Resolve<IGlobalBusStrateTracker>();
        IOutbox Outbox => ServiceLocator.Resolve<IOutbox>();
        InterprocessTransport InterprocessTransport => ServiceLocator.Resolve<InterprocessTransport>();
        Inbox Inbox => ServiceLocator.Resolve<Inbox>();
    }
}
