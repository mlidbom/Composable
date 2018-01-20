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

        //Todo: if(inprocessTransport.TryDispatchSynchronously(out var response)
        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) =>
            query is ICreateMyOwnResultQuery<TResult> selfCreating
                ? selfCreating.CreateResult()
                : await _transport.DispatchAsync(query).NoMarshalling();

        public TResult Query<TResult>(IQuery<TResult> query) => QueryAsync(query).ResultUnwrappingException();

        //Todo: inprocessTransport.Publish
        public void Publish(ITransactionalExactlyOnceDeliveryEvent @event) => TransactionScopeCe.Execute(() => _transport.DispatchIfTransactionCommits(@event));

        public void Send(ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            //Todo: if(inprocessTransport.TryDispatchSynchronously(out var response)
            _transport.DispatchIfTransactionCommits(command);
        });

        public void SendAtTime(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            _commandScheduler.Schedule(sendAt, command);
        });

        public Task<TResult> SendAsync<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            return _transport.DispatchIfTransactionCommitsAsync(command);
        });

        public TResult Send<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => SendAsync(command).ResultUnwrappingException();

        public void Dispose() { Contract.State.Assert(!_started); }
    }
}
