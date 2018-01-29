using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.System.Threading;
using Composable.System.Transactions;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    [UsedImplicitly] partial class ApiBrowserSession : ITransactionalMessageHandlerServiceBusSession, ILocalApiBrowserSession
    {
        readonly IInterprocessTransport _transport;
        readonly CommandScheduler _commandScheduler;
        readonly IMessageHandlerRegistry _handlerRegistry;
        readonly ISingleContextUseGuard _contextGuard;

        public ApiBrowserSession(IInterprocessTransport transport, CommandScheduler commandScheduler, IMessageHandlerRegistry handlerRegistry)
        {
            _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard());
            _transport = transport;
            _commandScheduler = commandScheduler;
            _handlerRegistry = handlerRegistry;
        }

        //todo: This _REALLY_ does not belong here.
        void IEventstoreEventPublisher.Publish(IAggregateEvent @event)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(@event);
            _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
            _transport.DispatchIfTransactionCommits(@event);
        }

        void IIntegrationBusSession.Send(BusApi.RemoteSupport.ExactlyOnce.ICommand command)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            _transport.DispatchIfTransactionCommits(command);
        }

        void IIntegrationBusSession.ScheduleSend(DateTime sendAt, BusApi.RemoteSupport.ExactlyOnce.ICommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _commandScheduler.Schedule(sendAt, command);
        }

        TResult ILocalApiBrowserSession.Execute<TResult>(BusApi.StrictlyLocal.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSendLocal(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return _handlerRegistry.GetCommandHandler(command).Invoke(command);
        }

        void ILocalApiBrowserSession.Execute(BusApi.StrictlyLocal.ICommand command)
        {
            MessageInspector.AssertValidToSendLocal(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _handlerRegistry.GetCommandHandler(command).Invoke(command);
        }

        TResult ILocalApiBrowserSession.Execute<TResult>(BusApi.StrictlyLocal.IQuery<TResult> query)
        {
            MessageInspector.AssertValidToSendLocal(query);
            _contextGuard.AssertNoContextChangeOccurred(this);
            // ReSharper disable once SuspiciousTypeConversion.Global
            //Todo: Test and stop disabling resharper warning
            return query is BusApi.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : _handlerRegistry.GetQueryHandler(query).Invoke(query);
        }

        void IRemoteApiBrowserSession.Post(BusApi.RemoteSupport.AtMostOnce.ICommand command) => ((IRemoteApiBrowserSession)this).PostAsync(command).WaitUnwrappingException();

        async Task IRemoteApiBrowserSession.PostAsync(BusApi.RemoteSupport.AtMostOnce.ICommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            await _transport.DispatchAsync(command);
        }

        TResult IRemoteApiBrowserSession.Post<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => ((IRemoteApiBrowserSession)this).PostAsync(command).ResultUnwrappingException();

        async Task<TResult> IRemoteApiBrowserSession.PostAsync<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return await _transport.DispatchAsync(command).NoMarshalling();
        }

        async Task<TResult> IRemoteApiBrowserSession.GetAsync<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(query);
            return query is BusApi.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : await _transport.DispatchAsync(query).NoMarshalling();
        }

        TResult IRemoteApiBrowserSession.Get<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => ((IRemoteApiBrowserSession)this).GetAsync(query).ResultUnwrappingException();
    }
}
