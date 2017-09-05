using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Reactive;

namespace Composable.Messaging.Buses
{
    class TestingOnlyInterprocessServiceBus : IInterProcessServiceBus, IDisposable
    {
        readonly DummyTimeSource _timeSource;
        readonly IInProcessServiceBus _inProcessServiceBus;
        readonly List<ScheduledMessage> _scheduledMessages = new List<ScheduledMessage>();
        public TestingOnlyInterprocessServiceBus(DummyTimeSource timeSource, IInProcessServiceBus inProcessServiceBus)
        {
            _timeSource = timeSource;
            _inProcessServiceBus = inProcessServiceBus;
            _managedResources = timeSource.UtcNowChanged.Subscribe(SendDueMessages);
        }

        void SendDueMessages(DateTime currentTime)
        {
            var dueMessages = _scheduledMessages.Where(message => message.SendAt <= currentTime)
                                                .ToList();
            dueMessages.ForEach(scheduledMessage => _inProcessServiceBus.Send(scheduledMessage.Message));
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

        readonly IDisposable _managedResources;
        public void Dispose()
        {
            _managedResources.Dispose();
        }

        public void Publish(IEvent anEvent) => Task.Run(() => _inProcessServiceBus.Publish(anEvent));
        public void Send(ICommand command) => Task.Run(() => _inProcessServiceBus.Send(command));
        public TResult Get<TResult>(IQuery<TResult> query) where TResult : IQueryResult => _inProcessServiceBus.Get(query);
    }
}
