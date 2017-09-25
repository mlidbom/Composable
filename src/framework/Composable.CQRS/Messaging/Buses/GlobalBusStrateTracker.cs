using System.Collections.Generic;
using System.Linq;
using Composable.System;
using Composable.System.Threading.ResourceAccess;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    class GlobalBusStrateTracker : IGlobalBusStrateTracker
    {
        readonly List<IInflightMessage> _inflightMessages = new List<IInflightMessage>();

        //It is never OK for this class to block. So make that explicit with a really strict timeout on all public operations
        readonly IExclusiveResourceAccessGuard _guard = ResourceAccessGuard.ExclusiveWithTimeout(1.Milliseconds());

        public IGlobalBusStateSnapshot CreateSnapshot()
            => _guard.ExecuteWithResourceExclusivelyLocked(
                () => new GlobalBusStateSnapshot(_inflightMessages.ToList()));

        public void QueuedMessage(IMessage message, [CanBeNull] IMessage triggeringMessage)
            => _guard.ExecuteWithResourceExclusivelyLocked(
                () => _inflightMessages.Add(new InflightMessage(message, triggeringMessage)));

        class GlobalBusStateSnapshot : IGlobalBusStateSnapshot
        {
            public GlobalBusStateSnapshot(IEnumerable<IInflightMessage> inflightMessages) => InflightMessages = inflightMessages;
            public IEnumerable<IInflightMessage> InflightMessages { get; }
        }

        class InflightMessage : IInflightMessage
        {
            public InflightMessage(IMessage message, IMessage triggeringMessage)
            {
                Message = message;
                TriggeringMessage = triggeringMessage;
            }
            public IMessage Message { get; }
            public IMessage TriggeringMessage { get; }
        }
    }
}
