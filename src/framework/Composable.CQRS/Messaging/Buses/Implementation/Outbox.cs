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

        public void Send(ICommand command) => _transport.Dispatch(command);

        public void Publish(IEvent anEvent) => _transport.Dispatch(anEvent);

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
            => (TResult)await _transport.Dispatch(command);

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult => (TResult)await _transport.Dispatch(query);

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult => (TResult)_transport.Dispatch(query).Result;
    }
}
