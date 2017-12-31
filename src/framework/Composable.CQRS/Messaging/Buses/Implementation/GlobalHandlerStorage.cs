using System;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        internal class HandlerStorage
        {
            internal void AddEventHandler(Type eventType)
            {
                Guid eventTypeId = TypeIdAttribute.Extract(eventType);
            }
            public void AddCommandHandler(Type commandType)
            {
                Guid commandTypeId = TypeIdAttribute.Extract(commandType);
            }
            public void AddQueryHandler(Type queryType)
            {
                Guid queryTypeId = TypeIdAttribute.Extract(queryType);
            }
        }


    }
}
