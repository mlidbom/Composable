using System;
using System.Collections.Generic;
using System.Linq;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Reactive;

namespace Composable.Messaging.Buses
{
    public class TestingOnlyServiceBus : InProcessServiceBus, IServiceBus, IDisposable, IMessageSpy
    {
        readonly DummyTimeSource _timeSource;
        readonly List<ScheduledMessage> _scheduledMessages = new List<ScheduledMessage>();
        public TestingOnlyServiceBus(DummyTimeSource timeSource, IMessageHandlerRegistry registry) : base(registry)
        {
            _timeSource = timeSource;
            _managedResources = timeSource.UtcNowChanged.Subscribe(SendDueMessages);
        }

        void SendDueMessages(DateTime currentTime)
        {
            var dueMessages = _scheduledMessages.Where(message => message.SendAt <= currentTime)
                                                .ToList();
            dueMessages.ForEach(scheduledMessage => ((IInProcessServiceBus)this).Send(scheduledMessage.Message));
            dueMessages.ForEach(message => _scheduledMessages.Remove(message));
        }

        public void SendAtTime(DateTime sendAt, ICommand message)
        {
            if(_timeSource.UtcNow > sendAt.ToUniversalTime())
            {
                throw new InvalidOperationException("You cannot schedule a message to be sent in the past.");
            }

            _scheduledMessages.Add(new ScheduledMessage(sendAt, message));
        }

        class ScheduledMessage
        {
            public DateTime SendAt { get; }
            public ICommand Message { get; }

            public ScheduledMessage(DateTime sendAt, ICommand message)
            {
                SendAt = sendAt.SafeToUniversalTime();
                Message = message;
            }
        }

        readonly List<IMessage> _dispatchedMessages = new List<IMessage>();
        public IEnumerable<IMessage> DispatchedMessages => _dispatchedMessages;

        protected override void AfterDispatchingMessage(IMessage message) { _dispatchedMessages.Add(message); }

        readonly IDisposable _managedResources;
        public void Dispose()
        {
            _managedResources.Dispose();
        }
    }
}
