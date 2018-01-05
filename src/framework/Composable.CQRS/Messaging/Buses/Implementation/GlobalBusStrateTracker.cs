using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    class GlobalBusStateTracker : IGlobalBusStateTracker
    {
        readonly Dictionary<Guid, InFlightMessage> _inflightMessages = new Dictionary<Guid, InFlightMessage>();

        //Todo: It is never OK for this class to block for a significant amount of time. So make that explicit with a really strict timeout on all operations waiting for access.
        //Currently we cannot make the timeout really strict because it does time out....
        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(100.Milliseconds());

        readonly List<Exception> _busExceptions = new List<Exception>();

        public IReadOnlyList<Exception> GetExceptionsFor() => _guard.Update(() => _busExceptions.ToList());

        public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage) => _guard.Update(() =>
        {
            var inFlightMessage = _inflightMessages.GetOrAdd(transportMessage.MessageId, () => new InFlightMessage(transportMessage));
            inFlightMessage.RemainingReceivers++;
        });

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride)
            => _guard.AwaitCondition(timeout: timeoutOverride ?? 30.Seconds(),
                                     condition: () => _inflightMessages.None());

        public void DoneWith(Guid messageId, Exception exception) => _guard.Update(() =>
        {
            if(exception != null)
            {
                _busExceptions.Add(exception);
            }

            var inFlightMessage = _inflightMessages[messageId];
            inFlightMessage.RemainingReceivers--;
            if(inFlightMessage.RemainingReceivers == 0)
            {
                _inflightMessages.Remove(messageId);
            }
        });

        class InFlightMessage
        {
            public InFlightMessage(TransportMessage.OutGoing message) => Message = message;
            public int RemainingReceivers { get; set; }
            public TransportMessage.OutGoing Message { get; private set; }
        }
    }
}
