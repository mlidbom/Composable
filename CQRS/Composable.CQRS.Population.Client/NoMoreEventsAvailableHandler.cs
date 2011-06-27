using System;
using NServiceBus;

namespace Composable.CQRS.Population.Client
{
    public class NoMoreEventsAvailableHandler : IHandleMessages<CQRS.Population.Client.NoMoreEventsAvailable>
    {
        public static event Action<CQRS.Population.Client.NoMoreEventsAvailable> NoMoreEvents = _ => { }; 
        public void Handle(CQRS.Population.Client.NoMoreEventsAvailable message)
        {
            NoMoreEvents(message);
        }
    }
}