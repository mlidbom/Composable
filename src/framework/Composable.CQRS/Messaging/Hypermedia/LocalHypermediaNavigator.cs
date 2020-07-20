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

            var commandHandler = _handlerRegistry.GetCommandHandler(command);
            return commandHandler.Invoke(command);
        }

        public void Execute(MessageTypes.StrictlyLocal.ICommand command)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendLocal(command);
            CommandValidator.AssertCommandIsValid(command);

            var commandHandler = _handlerRegistry.GetCommandHandler(command);
            commandHandler.Invoke(command);
        }

        public TResult Execute<TResult>(MessageTypes.StrictlyLocal.IQuery<TResult> query)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToSendLocal(query);

            // ReSharper disable once SuspiciousTypeConversion.Global
            //Todo: Test and stop disabling ReSharper warning
            if(query is MessageTypes.ICreateMyOwnResultQuery<TResult> selfCreating)
                return selfCreating.CreateResult();

            var queryHandler = _handlerRegistry.GetQueryHandler(query);
            return queryHandler.Invoke(query);
        }
    }
}
