using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses
{
    //Todo: Refactor responsibility for managing transactions somehow.
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    partial class ServiceBusSession : IServiceBusSession, IServiceBusControl
    {
        readonly IInterprocessTransport _transport;
        readonly IInbox _inbox;
        readonly CommandScheduler _commandScheduler;
        readonly IMessageHandlerRegistry _handlerRegistry;
        bool _started;

        public ServiceBusSession(IInterprocessTransport transport, IInbox inbox, CommandScheduler commandScheduler, IMessageHandlerRegistry handlerRegistry)
        {
            _transport = transport;
            _inbox = inbox;
            _commandScheduler = commandScheduler;
            _handlerRegistry = handlerRegistry;
        }

        void IServiceBusControl.Start()
        {
            Contract.State.Assert(!_started);

            _started = true;

            _inbox.Start();
            _transport.Start();
            _commandScheduler.Start();
        }

        void IServiceBusControl.Stop()
        {
            Contract.State.Assert(_started);
            _started = false;
            _commandScheduler.Stop();
            _transport.Stop();
            _inbox.Stop();
        }

        async Task<TResult> IRemoteServiceBusSession.GetRemoteAsync<TResult>(IQuery<TResult> query) =>
            query is ICreateMyOwnResultQuery<TResult> selfCreating
                ? selfCreating.CreateResult()
                : await _transport.DispatchAsync(query).NoMarshalling();

        TResult IRemoteServiceBusSession.GetRemote<TResult>(IQuery<TResult> query) => ((IRemoteServiceBusSession)this).GetRemoteAsync(query).ResultUnwrappingException();

        void IEventstoreEventPublisher.Publish(ITransactionalExactlyOnceDeliveryEvent @event) => TransactionScopeCe.Execute(() =>
        {
            _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
            _transport.DispatchIfTransactionCommits(@event);
        });

        void IRemoteServiceBusSession.PostRemote(ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            _transport.DispatchIfTransactionCommits(command);
        });

        void IRemoteServiceBusSession.SchedulePostRemote(DateTime sendAt, ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            _commandScheduler.Schedule(sendAt, command);
        });

        Task<TResult> IRemoteServiceBusSession.PostRemoteAsync<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            return _transport.DispatchIfTransactionCommitsAsync(command);
        });

        TResult IRemoteServiceBusSession.PostRemote<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => ((IRemoteServiceBusSession)this).PostRemoteAsync(command).ResultUnwrappingException();

        TResult ILocalServiceBusSession.Post<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            return _handlerRegistry.GetCommandHandler(command).Invoke(command);
        });

        void ILocalServiceBusSession.Post(ITransactionalExactlyOnceDeliveryCommand command) => TransactionScopeCe.Execute(() =>
        {
            CommandValidator.AssertCommandIsValid(command);
            _handlerRegistry.GetCommandHandler(command).Invoke(command);
        });

        TResult ILocalServiceBusSession.Get<TResult>(IQuery<TResult> query) =>
            query is ICreateMyOwnResultQuery<TResult> selfCreating
                ? selfCreating.CreateResult()
                : _handlerRegistry.GetQueryHandler(query).Invoke(query);

        public void Dispose() { Contract.State.Assert(!_started); }
    }
}
