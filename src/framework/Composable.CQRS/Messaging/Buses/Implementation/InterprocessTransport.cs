using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox : IOutbox
    {
        readonly Inbox _inbox;
        readonly CommandScheduler _commandScheduler;

        readonly IGuardedResource _guardedResource = GuardedResource.WithTimeout(1.Seconds());

        bool _running;

        public Outbox(IUtcTimeTimeSource timeSource, Inbox inbox)
        {
            _inbox = inbox;
            _commandScheduler = new CommandScheduler(this, timeSource);
        }

        public void Start() => _guardedResource.Update(() =>
        {
            Contract.Assert.That(!_running, message: "!_running");
            _running = true;
            _commandScheduler.Start();
        });

        public void Stop() => _guardedResource.Update(() =>
        {
            Contract.Assert.That(_running, message: "_running");
            _running = false;
            _commandScheduler.Dispose();
        });

        public void SendAtTime(DateTime sendAt, ICommand command) => _commandScheduler.Schedule(sendAt, command);

        public void Send(ICommand command)
        {
            _inbox.Send(command);
        }

        public void Publish(IEvent anEvent)
        {
            _inbox.Publish(anEvent);
        }

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
        {
            return await _inbox.SendAsync(command);
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            return await _inbox.QueryAsync(query);
        }

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            return _inbox.Query(query);
        }

        public Task<object> Dispatch(IMessage message) => Task.FromResult((object)null);
    }
}
