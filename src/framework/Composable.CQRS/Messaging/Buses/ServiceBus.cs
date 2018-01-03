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

        #region IServicebus

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

        public async Task SendAtTimeAsync(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand command) =>
            await TransactionScopeCe.ExecuteAsync(async () => await _commandScheduler.Schedule(sendAt, command).NoMarshalling());

        public async Task SendAsync(ITransactionalExactlyOnceDeliveryCommand command) =>
            await TransactionScopeCe.ExecuteAsync(async () =>
            {
                CommandValidator.AssertCommandIsValid(command);
                await _transport.DispatchAsync(command).NoMarshalling();
            });

        public async Task PublishAsync(ITransactionalExactlyOnceDeliveryEvent anEvent) =>
            await TransactionScopeCe.ExecuteAsync(async () => await _transport.DispatchAsync(anEvent).NoMarshalling());

        public async Task<Task<TResult>> SendAsyncAsync<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) =>
            await TransactionScopeCe.ExecuteAsync(async () =>
            {
                CommandValidator.AssertCommandIsValid(command);
                return await _transport.DispatchAsyncAsync(command).NoMarshalling();
            });

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) =>
            await _transport.DispatchAsync(query).NoMarshalling();

        #endregion

        #region ISimpleServicebus

        public void Publish(ITransactionalExactlyOnceDeliveryEvent @event) => PublishAsync(@event).Wait();
        public void Send(ITransactionalExactlyOnceDeliveryCommand command) => SendAsync(command).Wait();
        public void SendAtTime(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand command) => SendAtTimeAsync(sendAt, command).Wait();
        public TResult Send<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => SendAsync(command).Result;
        public TResult Query<TResult>(IQuery<TResult> query) => QueryAsync(query).Result;
        public async Task<TResult> SendAsync<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => await SendAsyncAsync(command).Result.NoMarshalling();

        #endregion

        public void Dispose() { Contract.State.Assert(!_started); }
    }
}
