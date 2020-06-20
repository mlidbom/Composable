using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Hypermedia;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    [UsedImplicitly] class ApiNavigatorSession : IServiceBusSession, ILocalHypermediaNavigator
    {
        readonly IInterprocessTransport _transport;
        readonly CommandScheduler _commandScheduler;
        readonly IMessageHandlerRegistry _handlerRegistry;
        readonly IRemoteHypermediaNavigator _remoteNavigator;
        readonly ISingleContextUseGuard _contextGuard;

        public ApiNavigatorSession(IInterprocessTransport transport, CommandScheduler commandScheduler, IMessageHandlerRegistry handlerRegistry, IRemoteHypermediaNavigator remoteNavigator)
        {
            _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard());
            _transport = transport;
            _commandScheduler = commandScheduler;
            _handlerRegistry = handlerRegistry;
            _remoteNavigator = remoteNavigator;
        }

        void IIntegrationBusSession.Send(MessageTypes.Remotable.ExactlyOnce.ICommand command)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            _transport.DispatchIfTransactionCommits(command);
        }

        void IIntegrationBusSession.ScheduleSend(DateTime sendAt, MessageTypes.Remotable.ExactlyOnce.ICommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _commandScheduler.Schedule(sendAt, command);
        }

        TResult ILocalHypermediaNavigator.Execute<TResult>(MessageTypes.StrictlyLocal.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSendLocal(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            return _handlerRegistry.GetCommandHandler(command).Invoke(command);
        }

        void ILocalHypermediaNavigator.Execute(MessageTypes.StrictlyLocal.ICommand command)
        {
            MessageInspector.AssertValidToSendLocal(command);
            _contextGuard.AssertNoContextChangeOccurred(this);
            CommandValidator.AssertCommandIsValid(command);
            _handlerRegistry.GetCommandHandler(command).Invoke(command);
        }

        TResult ILocalHypermediaNavigator.Execute<TResult>(MessageTypes.StrictlyLocal.IQuery<TResult> query)
        {
            MessageInspector.AssertValidToSendLocal(query);
            _contextGuard.AssertNoContextChangeOccurred(this);
            // ReSharper disable once SuspiciousTypeConversion.Global
            //Todo: Test and stop disabling resharper warning
            return query is MessageTypes.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : _handlerRegistry.GetQueryHandler(query).Invoke(query);
        }

        Task IRemoteHypermediaNavigator.PostAsync(MessageTypes.Remotable.AtMostOnce.ICommand command) => _remoteNavigator.PostAsync(command);
        void IRemoteHypermediaNavigator.Post(MessageTypes.Remotable.AtMostOnce.ICommand command) => _remoteNavigator.Post(command);
        Task<TResult> IRemoteHypermediaNavigator.PostAsync<TResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> command) => _remoteNavigator.PostAsync(command);
        TResult IRemoteHypermediaNavigator.Post<TResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> command) => _remoteNavigator.Post(command);
        Task<TResult> IRemoteHypermediaNavigator.GetAsync<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query) => _remoteNavigator.GetAsync(query);
        TResult IRemoteHypermediaNavigator.Get<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query) => _remoteNavigator.Get(query);
    }
}
