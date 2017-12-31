using System;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        class HandlerStorage
        {
            internal void AddEventHandler(Type eventType)
            {
                var eventTypeId = TypeIdAttribute.Extract(eventType);
            }
            public void AddCommandHandler(Type commandType)
            {
                var commandTypeId = TypeIdAttribute.Extract(commandType);
            }
            public void AddQueryHandler(Type queryType)
            {
                var queryTypeId = TypeIdAttribute.Extract(queryType);
            }
        }
    }
}
