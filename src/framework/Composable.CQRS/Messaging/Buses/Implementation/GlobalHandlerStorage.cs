using System;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        internal class HandlerStorage
        {
            internal void AddEventHandler(Type eventType)
            {
                Guid eventId = TypeIdAttribute.Extract(eventType);
            }
        }


    }
}
