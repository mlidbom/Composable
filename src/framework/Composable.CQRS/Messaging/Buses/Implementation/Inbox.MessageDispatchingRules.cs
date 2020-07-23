﻿using Composable.SystemCE.Linq;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
        {
            public bool CanBeDispatched(IExecutingMessagesSnapshot executing, TransportMessage.InComing candidateMessage)
            {
                if(candidateMessage.MessageTypeEnum != TransportMessage.TransportMessageType.NonTransactionalQuery) return true;

                return executing.AtMostOnceCommands.None() && executing.ExactlyOnceCommands.None() && executing.ExactlyOnceEvents.None();
            }
        }

        class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
        {
            public bool CanBeDispatched(IExecutingMessagesSnapshot executing, TransportMessage.InComing candidateMessage)
            {
                if(candidateMessage.MessageTypeEnum == TransportMessage.TransportMessageType.NonTransactionalQuery) return true;

                return executing.AtMostOnceCommands.None() && executing.ExactlyOnceCommands.None() && executing.ExactlyOnceEvents.None();
            }
        }
    }
}
