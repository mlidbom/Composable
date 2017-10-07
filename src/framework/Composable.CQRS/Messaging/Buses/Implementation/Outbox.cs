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
        readonly IInterprocessTransport _transport;
        readonly CommandScheduler _commandScheduler;

        readonly IGuardedResource _guardedResource = GuardedResource.WithTimeout(1.Seconds());

        bool _running;

        public Outbox(IUtcTimeTimeSource timeSource, Inbox inbox, IInterprocessTransport transport)
        {
            _inbox = inbox;
            _transport = transport;
            _commandScheduler = new CommandScheduler(this, timeSource);
        }

        public void Start() => _guardedResource.Update(() =>
        {
            Contract.Invariant.Assert(!_running);
            _running = true;
            _commandScheduler.Start();
        });

        public void Stop() => _guardedResource.Update(() =>
        {
            Contract.Invariant.Assert(_running);
            _running = false;
            _commandScheduler.Dispose();
        });

        public void SendAtTime(DateTime sendAt, ICommand command) => _commandScheduler.Schedule(sendAt, command);

        public void Send(ICommand command)
        {
            var result = _transport.Dispatch(command);
            _inbox.Send(command);
        }

        public void Publish(IEvent anEvent)
        {
            _inbox.Publish(anEvent);
        }

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
        {
            var result = _transport.Dispatch(command);
            return await _inbox.SendAsync(command);
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            var result = _transport.Dispatch(query);
            return await _inbox.QueryAsync(query);
        }

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            var result = _transport.Dispatch(query);
            return _inbox.Query(query);
        }

        public Task<object> Dispatch(IMessage message) => Task.FromResult((object)null);
    }
}
