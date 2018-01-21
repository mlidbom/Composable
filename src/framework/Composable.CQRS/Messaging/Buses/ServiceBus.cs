using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus, IServiceBusControl, IInProcessServiceBus, IEventstoreEventPublisher
    {
        readonly IInterprocessTransport _transport;
        readonly IInbox _inbox;
        readonly CommandScheduler _commandScheduler;
        readonly IMessageHandlerRegistry _handlerRegistry;
        bool _started;

        public ServiceBus(IInterprocessTransport transport, IInbox inbox, CommandScheduler commandScheduler, IMessageHandlerRegistry handlerRegistry)
        {
            _transport = transport;
            _inbox = inbox;
            _commandScheduler = commandScheduler;
            _handlerRegistry = handlerRegistry;
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
        public async Task<TResult> GetAsync<TResult>(IQuery<TResult> query) =>
            query is ICreateMyOwnResultQuery<TResult> selfCreating
                ? selfCreating.CreateResult()
                : await _transport.DispatchAsync(query).NoMarshalling();

        public TResult Get<TResult>(IQuery<TResult> query) => GetAsync(query).ResultUnwrappingException();

        //Todo: inprocessTransport.Publish
        public void Publish(ITransactionalExactlyOnceDeliveryEvent @event) => TransactionScopeCe.Execute(() =>
        {
            _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
            _transport.DispatchIfTransactionCommits(@event);
        });

        public void Post(ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);

            if(_handlerRegistry.TryGetCommandHandler(command, out var handler))
            {
                handler(command);
                return;
            }

            _transport.DispatchIfTransactionCommits(command);
        });

        public void PostAtTime(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            _commandScheduler.Schedule(sendAt, command);
        });

        public Task<TResult> PostAsync<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);

            if(_handlerRegistry.TryGetCommandHandlerWithResult(command, out var handler))
            {
                return Task.FromResult((TResult)handler(command));
            }

            return _transport.DispatchIfTransactionCommitsAsync(command);
        });

        public TResult Post<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => PostAsync(command).ResultUnwrappingException();

        public TResult PostInProcess<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => _handlerRegistry.GetCommandHandler(command).Invoke(command);

        void IInProcessServiceBus.PostInProcess(ITransactionalExactlyOnceDeliveryCommand message) => _handlerRegistry.GetCommandHandler(message).Invoke(message);

        TResult IInProcessServiceBus.GetInProcess<TResult>(IQuery<TResult> query) => _handlerRegistry.GetQueryHandler(query).Invoke(query);

        public void Dispose() { Contract.State.Assert(!_started); }
    }
}
