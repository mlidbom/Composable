using System;
using Composable.Messaging;
using Newtonsoft.Json;

namespace Composable.Persistence.EventStore
{
    public interface IAggregateEvent<out TEventInterface> : MessageTypes.IWrapperEvent<TEventInterface>
        where TEventInterface : IAggregateEvent
    {
    }

    public interface IAggregateEvent : MessageTypes.Remotable.ExactlyOnce.IEvent
    {
        Guid EventId { get; }
        int AggregateVersion { get; }
        Guid AggregateId { get; }
        //Todo:Consider using DateTimeOffset instead of DateTime for the timestamp in events. DateTime is fragile and requires every bit of code that deals with it in composable to remember to translate dates to UTC. Even if it does comparison of datetimes is incorrect if we ever compare with a  non-utc value. All of these problems disappear with DateTimeOffset.
        DateTime UtcTimeStamp { get; }

#pragma warning disable CA1033 // Interface methods should be callable by child types
        //Todo: having this here feels icky. Why both EventId and MessageID? Can't we make this cleaner?
        [JsonIgnore] Guid MessageTypes.Remotable.IAtMostOnceMessage.MessageId => EventId;
#pragma warning restore CA1033 // Interface methods should be callable by child types
    }
}
