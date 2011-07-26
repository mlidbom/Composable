using System;
using NServiceBus;

namespace Composable.CQRS.Population.Client
{
    public class MoreEventsAvailableHandler : IHandleMessages<CQRS.Population.Client.MoreEventsAvailable>
    {
        public static event Action<MoreEventsAvailable> MoreEvents = _ => { }; 

        private readonly IBus _bus;

        public MoreEventsAvailableHandler(IBus bus)
        {
            _bus = bus;
        }

        public void Handle(MoreEventsAvailable message)
        {
            _bus.Reply(message.ContinuationCommand);
            MoreEvents(message);
        }
    }
}