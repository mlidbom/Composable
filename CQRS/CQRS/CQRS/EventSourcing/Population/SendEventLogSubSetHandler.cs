using System;
using NServiceBus;
using Composable.System.Linq;
using System.Linq;

namespace Composable.CQRS.EventSourcing.Population
{
    public class SendEventLogSubSetHandler : IHandleMessages<SendEventLogSubSetCommand>
    {
        private readonly IEventSomethingOrOther _eventStore;
        private readonly IBus _bus;

        public SendEventLogSubSetHandler(IEventSomethingOrOther eventStore, IBus bus)
        {
            _eventStore = eventStore;
            _bus = bus;
        }

        public void Handle(SendEventLogSubSetCommand command)
        {
            var events = _eventStore
                            .StreamEventsAfterEventWithId(command.StartAfterEventId)
                            .Where(evt => command.EventTypes.Any(requestedType => requestedType.IsAssignableFrom(evt.GetType())))
                            .Take(command.NumberOfEventsToSend)
                            .ToList();

            if (events.Any())
            {
                foreach (var eventsChunk in events.ChopIntoSizesOf(command.MaxEventsPerMessage))
                {
                    _bus.Reply(eventsChunk.ToArray());
                }

                var lastEvent = events.Last();
                if (_eventStore.StreamEventsAfterEventWithId(lastEvent.EventId).Any())
                {
                    _bus.Reply(new MoreEventsAvailable()
                                   {
                                        ContinuationCommand = new SendEventLogSubSetCommand(command)
                                                                  {
                                                                      StartAfterEventId = lastEvent.EventId
                                                                  }
                                   });
                    return;
                }
            }

            _bus.Reply(new NoMoreEventsAvailable()
                           {
                               Command = command
                           });
        }
    }

    public class MoreEventsAvailableHandler : IHandleMessages<MoreEventsAvailable>
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

    public class NoMoreEventsAvailableHandler : IHandleMessages<NoMoreEventsAvailable>
    {
        public static event Action<NoMoreEventsAvailable> NoMoreEvents = _ => { }; 
        public void Handle(NoMoreEventsAvailable message)
        {
            NoMoreEvents(message);
        }
    }
}