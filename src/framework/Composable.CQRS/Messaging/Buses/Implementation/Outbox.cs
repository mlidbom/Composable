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

        readonly IResourceGuard _resourceGuard = ResourceGuard.WithTimeout(1.Seconds());

        bool _running;

        public Outbox(IUtcTimeTimeSource timeSource, IInterprocessTransport transport)
        {
            _transport = transport;
            _commandScheduler = new CommandScheduler(this, timeSource);
        }

        public void Start() => _resourceGuard.Update(() =>
        {
            Contract.State.Assert(!_running);
            _running = true;
            _commandScheduler.Start();
        });

        public void Stop() => _resourceGuard.Update(() =>
        {
            Contract.State.Assert(_running);
            _running = false;
            _commandScheduler.Dispose();
        });

        public Task SendAtTimeAsync(DateTime sendAt, IDomainCommand command)
            => _commandScheduler.Schedule(sendAt, command);

        public Task SendAsync(IDomainCommand command)
            => _transport.DispatchAsync(command);

        public Task PublishAsync(IEvent anEvent)
            => _transport.DispatchAsync(anEvent);

        public TResult Query<TResult>(IQuery<TResult> query)
            => _transport.DispatchAsync(query).Result;

        public async Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command)
            => await _transport.DispatchAsync(command);

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
            => await _transport.DispatchAsync(query);
    }
}
