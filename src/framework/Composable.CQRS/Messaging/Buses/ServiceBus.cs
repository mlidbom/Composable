using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus, IServiceBusControl
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


        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) => await _transport.DispatchAsync(query).NoMarshalling();

        public TResult Query<TResult>(IQuery<TResult> query) => QueryAsync(query).ResultUnwrappingException();

        public void Publish(ITransactionalExactlyOnceDeliveryEvent @event) => TransactionScopeCe.Execute(() => _transport.Dispatch(@event));

        public void Send(ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            _transport.Dispatch(command);
        });

        public void SendAtTime(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() => _commandScheduler.Schedule(sendAt, command));

        public Task<TResult> SendAsync<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            return _transport.DispatchAsync(command);
        });

        public TResult Send<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => SendAsync(command).ResultUnwrappingException();

        public void Dispose() { Contract.State.Assert(!_started); }
    }
}
