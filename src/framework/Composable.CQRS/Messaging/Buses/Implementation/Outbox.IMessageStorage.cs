﻿using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox
    {
        public interface IMessageStorage
        {
            void SaveMessage(MessageTypes.Remotable.ExactlyOnce.IMessage message, params EndpointId[] receiverEndpointIds);
            void MarkAsReceived(Guid messageId, EndpointId receiverId);
            Task StartAsync();
        }
    }
}
