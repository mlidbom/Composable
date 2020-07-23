using System.Collections.Generic;
using System.Linq;
using Composable.Persistence.EventStore;
using Composable.SystemCE.Linq;

namespace Composable.Tests.CQRS
{
    static class EventStorageTestHelper
    {
        //Not all storage providers stores with more than 6 decimal points precision
        internal static void StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(IReadOnlyList<IAggregateEvent> events)
            => events.Cast<AggregateEvent>().ForEach(@event => @event.UtcTimeStamp = @event.UtcTimeStamp.AddTicks(-(@event.UtcTimeStamp.Ticks % (10))));
    }
}
