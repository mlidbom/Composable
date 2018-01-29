using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;
using Composable.System.Transactions;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    [UsedImplicitly] partial class ApiBrowser : IApiBrowser, ILocalApiBrowser
    {
        readonly IInterprocessTransport _transport;
        readonly CommandScheduler _commandScheduler;
        readonly IMessageHandlerRegistry _handlerRegistry;
        readonly ISingleContextUseGuard _contextGuard;

        public ApiBrowser(IInterprocessTransport transport, CommandScheduler commandScheduler, IMessageHandlerRegistry handlerRegistry)
        {
            _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard());
            _transport = transport;
            _commandScheduler = commandScheduler;
            _handlerRegistry = handlerRegistry;
        }

        void IEventstoreEventPublisher.Publish(BusApi.Remote.ExactlyOnce.IEvent @event) => TransactionScopeCe.Execute(() =>
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSend(@event);
            _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
            _transport.DispatchIfTransactionCommits(@event);
        });

        void ITransactionalMessageHandlerApiBrowser.PostRemote(BusApi.Remote.ExactlyOnce.ICommand command) => TransactionScopeCe.Execute(() =>
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSend(command);
            CommandValidator.AssertCommandIsValid(command);
            _transport.DispatchIfTransactionCommits(command);
        });

        void ITransactionalMessageHandlerApiBrowser.SchedulePostRemote(DateTime sendAt, BusApi.Remote.ExactlyOnce.ICommand command) => TransactionScopeCe.Execute(() =>
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _commandScheduler.Schedule(sendAt, command);
        });

        Task<TResult> ITransactionalMessageHandlerApiBrowser.PostRemoteAsync<TResult>(BusApi.Remote.ExactlyOnce.ICommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return _transport.DispatchIfTransactionCommitsAsync(command);
        });

        TResult ITransactionalMessageHandlerApiBrowser.PostRemote<TResult>(BusApi.Remote.ExactlyOnce.ICommand<TResult> command) => ((ITransactionalMessageHandlerApiBrowser)this).PostRemoteAsync(command).ResultUnwrappingException();

        TResult ILocalApiBrowser.PostLocal<TResult>(BusApi.Local.ICommand<TResult> command) => TransactionScopeCe.Execute(() =>
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return _handlerRegistry.GetCommandHandler(command).Invoke(command);
        });

        void ILocalApiBrowser.PostLocal(BusApi.Local.ICommand command) => TransactionScopeCe.Execute(() =>
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _handlerRegistry.GetCommandHandler(command).Invoke(command);
        });

        TResult ILocalApiBrowser.GetLocal<TResult>(BusApi.Local.IQuery<TResult> query)
        {
            MessageInspector.AssertValidToSend(query);
            _contextGuard.AssertNoContextChangeOccurred(this);
            // ReSharper disable once SuspiciousTypeConversion.Global
            //Todo: Test and stop disabling resharper warning
            return query is BusApi.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : _handlerRegistry.GetQueryHandler(query).Invoke(query);
        }

        void IUIInteractionApiBrowser.PostRemote(BusApi.Remote.AtMostOnce.ICommand command)
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _transport.Dispatch(command);
        }

        TResult IUIInteractionApiBrowser.PostRemote<TResult>(BusApi.Remote.AtMostOnce.ICommand<TResult> command) => ((IUIInteractionApiBrowser)this).PostRemoteAsync(command).ResultUnwrappingException();

        async Task<TResult> IUIInteractionApiBrowser.PostRemoteAsync<TResult>(BusApi.Remote.AtMostOnce.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSend(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return await _transport.DispatchAsync(command).NoMarshalling();
        }

        async Task<TResult> IUIInteractionApiBrowser.GetRemoteAsync<TResult>(BusApi.Remote.NonTransactional.IQuery<TResult> query)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSend(query);
            return query is BusApi.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : await _transport.DispatchAsync(query).NoMarshalling();
        }

        TResult IUIInteractionApiBrowser.GetRemote<TResult>(BusApi.Remote.NonTransactional.IQuery<TResult> query) => ((IUIInteractionApiBrowser)this).GetRemoteAsync(query).ResultUnwrappingException();
    }
}
