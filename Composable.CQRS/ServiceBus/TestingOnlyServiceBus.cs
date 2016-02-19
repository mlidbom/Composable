using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Castle.Windsor;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Reactive;

namespace Composable.ServiceBus
{
    public class TestingOnlyServiceBus : SynchronousBus
    {
        private readonly DummyTimeSource _timeSource;
        private readonly List<ScheduledMessage> _scheduledMessages = new List<ScheduledMessage>(); 
        public TestingOnlyServiceBus(IWindsorContainer container, DummyTimeSource timeSource) : base(container)
        {
            _timeSource = timeSource;
            timeSource.UtcNowChanged.Subscribe(SendDueMessages);
        }

        private void SendDueMessages(DateTime currentTime)
        {
            var dueMessages = _scheduledMessages.Where(message => message.SendAt <= currentTime).ToList();
            dueMessages.ForEach(scheduledMessage => Send(scheduledMessage.Message));
            dueMessages.ForEach(message => _scheduledMessages.Remove(message));
        }

        public override void SendAtTime(DateTime sendAt, object message)
        {
            if(_timeSource.UtcNow > sendAt.ToUniversalTime())
            {
                throw new InvalidOperationException("You cannot schedule a message to be sent in the past.");
            }
            _scheduledMessages.Add(new ScheduledMessage(sendAt, message));
        }

        private class ScheduledMessage
        {
            public Guid Id { get; }
            public DateTime SendAt { get; }
            public object Message { get; }

            public ScheduledMessage(DateTime sendAt, object message)
            {
                Id = Guid.NewGuid();
                SendAt = sendAt.SafeToUniversalTime();
                Message = message;
            }
        }
    }    
}