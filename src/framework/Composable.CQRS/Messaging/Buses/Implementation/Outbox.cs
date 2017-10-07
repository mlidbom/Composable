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
        readonly IInterprocessTransport _transport;
        readonly CommandScheduler _commandScheduler;

        readonly IGuardedResource _guardedResource = GuardedResource.WithTimeout(1.Seconds());

        bool _running;

        public Outbox(IUtcTimeTimeSource timeSource, IInterprocessTransport transport)
        {
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

        public void SendAtTime(DateTime sendAt, ICommand command)
            => _commandScheduler.Schedule(sendAt, command);

        public void Send(ICommand command)
            => _transport.Dispatch(command);

        public void Publish(IEvent anEvent)
            => _transport.Dispatch(anEvent);

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _transport.Dispatch(query).Result;

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
            => await _transport.Dispatch(command);

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => await _transport.Dispatch(query);
    }
}
