using Composable.Messaging.Buses.Implementation;
using Composable.SystemExtensions.Threading;

namespace Composable.Messaging.Hypermedia
{
    class LocalHypermediaNavigator : ILocalHypermediaNavigator
    {
        readonly IMessageHandlerRegistry _handlerRegistry;
        readonly ISingleContextUseGuard _contextGuard;

        public LocalHypermediaNavigator(IMessageHandlerRegistry handlerRegistry)
        {
            _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard());
            _handlerRegistry = handlerRegistry;
        }

        public TResult Execute<TResult>(MessageTypes.StrictlyLocal.ICommand<TResult> command)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendLocal(command);
            CommandValidator.AssertCommandIsValid(command);
            return _handlerRegistry.GetCommandHandler(command).Invoke(command);
        }

        public void Execute(MessageTypes.StrictlyLocal.ICommand command)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendLocal(command);
            CommandValidator.AssertCommandIsValid(command);
            _handlerRegistry.GetCommandHandler(command).Invoke(command);
        }

        public TResult Execute<TResult>(MessageTypes.StrictlyLocal.IQuery<TResult> query)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendLocal(query);
            // ReSharper disable once SuspiciousTypeConversion.Global
            //Todo: Test and stop disabling ReSharper warning
            return query is MessageTypes.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : _handlerRegistry.GetQueryHandler(query).Invoke(query);
        }
    }
}
