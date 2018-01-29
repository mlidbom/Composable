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

        void IEventstoreEventPublisher.Publish(BusApi.RemoteSupport.ExactlyOnce.IEvent @event)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(@event);
            _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
            _transport.DispatchIfTransactionCommits(@event);
        }

        void ITransactionalMessageHandlerApiBrowser.PostRemote(BusApi.RemoteSupport.ExactlyOnce.ICommand command)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            _transport.DispatchIfTransactionCommits(command);
        }

        void ITransactionalMessageHandlerApiBrowser.SchedulePostRemote(DateTime sendAt, BusApi.RemoteSupport.ExactlyOnce.ICommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _commandScheduler.Schedule(sendAt, command);
        }

        TResult ILocalApiBrowser.PostLocal<TResult>(BusApi.StrictlyLocal.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSendLocal(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return _handlerRegistry.GetCommandHandler(command).Invoke(command);
        }

        void ILocalApiBrowser.PostLocal(BusApi.StrictlyLocal.ICommand command)
        {
            MessageInspector.AssertValidToSendLocal(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _handlerRegistry.GetCommandHandler(command).Invoke(command);
        }

        TResult ILocalApiBrowser.GetLocal<TResult>(BusApi.StrictlyLocal.IQuery<TResult> query)
        {
            MessageInspector.AssertValidToSendLocal(query);
            _contextGuard.AssertNoContextChangeOccurred(this);
            // ReSharper disable once SuspiciousTypeConversion.Global
            //Todo: Test and stop disabling resharper warning
            return query is BusApi.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : _handlerRegistry.GetQueryHandler(query).Invoke(query);
        }

        void IRemoteApiBrowser.PostRemote(BusApi.RemoteSupport.AtMostOnce.ICommand command) => ((IRemoteApiBrowser)this).PostRemoteAsync(command).WaitUnwrappingException();

        async Task IRemoteApiBrowser.PostRemoteAsync(BusApi.RemoteSupport.AtMostOnce.ICommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            await _transport.DispatchAsync(command);
        }

        TResult IRemoteApiBrowser.PostRemote<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => ((IRemoteApiBrowser)this).PostRemoteAsync(command).ResultUnwrappingException();

        async Task<TResult> IRemoteApiBrowser.PostRemoteAsync<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return await _transport.DispatchAsync(command).NoMarshalling();
        }

        async Task<TResult> IRemoteApiBrowser.GetRemoteAsync<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(query);
            return query is BusApi.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : await _transport.DispatchAsync(query).NoMarshalling();
        }

        TResult IRemoteApiBrowser.GetRemote<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => ((IRemoteApiBrowser)this).GetRemoteAsync(query).ResultUnwrappingException();
    }
}
