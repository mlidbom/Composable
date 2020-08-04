using System;
using System.Collections.Generic;
using System.Linq;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    class GlobalBusStateTracker : IGlobalBusStateTracker
    {
        readonly IThreadShared<NonThreadSafeImplementation> _implementation = ThreadShared.Create<NonThreadSafeImplementation>(new NonThreadSafeImplementation());

        public IReadOnlyList<Exception> GetExceptions() => _implementation.Update(implementation => implementation.GetExceptions());

        //performance: Do we care about queries here? Could we exclude them and lessen the contention a lot?
        public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage) => _implementation.Update(implementation => implementation.SendingMessageOnTransport(transportMessage));

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) =>
            _implementation.Await(implementation => implementation.InflightMessages.Count == 0);

        public void DoneWith(Guid messageId, Exception? exception) =>
            _implementation.Update(implementation => implementation.DoneWith(messageId, exception));

        class InFlightMessage
        {
            public int RemainingReceivers { get; set; }
        }

        class NonThreadSafeImplementation
        {
            internal readonly Dictionary<Guid, InFlightMessage> InflightMessages = new Dictionary<Guid, InFlightMessage>();

            readonly List<Exception> _busExceptions = new List<Exception>();

            public IReadOnlyList<Exception> GetExceptions() => _busExceptions.ToList();

            public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage)
            {
                var inFlightMessage = InflightMessages.GetOrAdd(transportMessage.MessageId, () => new InFlightMessage());
                inFlightMessage.RemainingReceivers++;
            }

            public void DoneWith(Guid messageId, Exception? exception)
            {
                if(exception != null)
                {
                    _busExceptions.Add(exception);
                }

                var inFlightMessage = InflightMessages[messageId];
                inFlightMessage.RemainingReceivers--;
                if(inFlightMessage.RemainingReceivers == 0)
                {
                    InflightMessages.Remove(messageId);
                }
            }
        }
    }

    class NullOpGlobalBusStateTracker : IGlobalBusStateTracker
    {
        public IReadOnlyList<Exception> GetExceptions() => new List<Exception>();
        public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage) { }
        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) { }
        public void DoneWith(Guid message, Exception? exception) { }
    }
}
