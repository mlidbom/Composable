using System;
using NServiceBus;

namespace Composable.CQRS.EventSourcing.Population
{
    /// <summary>
    /// When recieving this command the system recieving the command will reply by with messages batching 
    /// together at most <see cref="MaxEventsPerMessage"/> events per message. 
    /// 
    /// If before reaching <see cref="NumberOfEventsToSend"/> the eventlog is exhausted a <see cref="NoMoreEventsAvailable"/> message will be sent last.
    /// 
    /// If there are more messages in the log after sending <see cref="NumberOfEventsToSend"/> the last reply message sent will be a <see cref="MoreEventsAvailable"/>
    /// message.
    /// </summary>
    public class SendEventLogSubSetCommand : ICommandMessage
    {
        /// <summary>The returned list of events should start with the first event in chronological order AFTER this event 
        /// in the source systems event log.
        /// 
        /// If the event id is null the list should start from the first event in the source system.
        /// </summary>
        public Guid? StartAfterEventId { get; set; }

        /// <summary>The number of events that the source system should send before concluding with a <see cref="MoreEventsAvailable"/> message.</summary>
        public int NumberOfEventsToSend { get; set; }

        /// <summary>The maximum number of events that the source system may batch into a single message.</summary>
        public int MaxEventsPerMessage { get; set; }

        /// <summary>Specifies that only events of this type or subtypes should be included in the list sent.
        /// If you want to get all events use null;
        /// </summary>
        public Type EventType { get; set; }
    }

    /// <summary>Signals to the requesting party that there are more events left in the log.
    /// The requesting party should probably respond to this message by sending a new <see cref="SendEventLogSubSetCommand"/> message
    /// using <see cref="LastEventSent"/> for the <see cref="SendEventLogSubSetCommand.StartAfterEventId"/> command parameter value
    /// </summary>
    public class MoreEventsAvailable : IMessage
    {
        public Guid LastEventSent { get; set; }
    }

    /// <summary>Signals to the requesting party that all events in the log have been sent.</summary>
    public class NoMoreEventsAvailable:IMessage
    {
        
    }
}