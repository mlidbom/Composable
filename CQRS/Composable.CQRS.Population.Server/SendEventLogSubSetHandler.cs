using Composable.CQRS.EventSourcing;
using Composable.CQRS.Population.Client;
using NServiceBus;
using Composable.System.Linq;
using System.Linq;

namespace Composable.CQRS.Population.Server
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
                            //.Where(evt => command.EventTypes.Any(requestedType => requestedType.IsAssignableFrom(evt.GetType())))
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
}