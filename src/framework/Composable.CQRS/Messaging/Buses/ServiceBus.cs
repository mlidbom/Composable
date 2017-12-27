using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus
    {
        readonly IInterprocessTransport _transport;
        readonly IInbox _inbox;
        readonly CommandScheduler _commandScheduler;
        bool _started;

        public ServiceBus(IInterprocessTransport transport, IInbox inbox, CommandScheduler commandScheduler)
        {
            _transport = transport;
            _inbox = inbox;
            _commandScheduler = commandScheduler;
        }

        public void Start()
        {
            Contract.State.Assert(!_started);
            _started = true;

            _inbox.Start();
            _transport.Start();
            _commandScheduler.Start();
        }

        public void Stop()
        {
            Contract.State.Assert(_started);
            _started = false;
            _commandScheduler.Stop();
            _transport.Stop();
            _inbox.Stop();
        }

        public async Task SendAtTimeAsync(DateTime sendAt, IDomainCommand command) =>
            await _commandScheduler.Schedule(sendAt, command).NoMarshalling();

        public async Task SendAsync(IDomainCommand command)
        {
            CommandValidator.AssertCommandIsValid(command);
            await _transport.DispatchAsync(command).NoMarshalling();
        }

        public async Task PublishAsync(IEvent anEvent) => await _transport.DispatchAsync(anEvent).NoMarshalling();

        public async Task<Task<TResult>> SendAsyncAsync<TResult>(IDomainCommand<TResult> command)
        {
            CommandValidator.AssertCommandIsValid(command);
            return await Task.FromResult(_transport.DispatchAsync(command)).NoMarshalling();
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) =>
            await _transport.DispatchAsync(query).NoMarshalling();

        public void Dispose() { Contract.State.Assert(!_started); }

        #region ISimpleServicebus

        public void Publish(IEvent @event) => PublishAsync(@event).Wait();
        public void Send(IDomainCommand command) => SendAsync(command).Wait();
        public TResult Send<TResult>(IDomainCommand<TResult> command) => SendAsync(command).Result;
        public TResult Query<TResult>(IQuery<TResult> query) => QueryAsync(query).Result;
        public async Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command) => await SendAsyncAsync(command).Result.NoMarshalling();

        #endregion
    }
}
