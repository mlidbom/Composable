using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.ThreadingCE;

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

        public TResult Execute<TResult>(MessageTypes.IStrictlyLocalCommand<TResult> command)
        {
            CommonAssertion(command);

            var commandHandler = _handlerRegistry.GetCommandHandler(command);
            return commandHandler.Invoke(command);
        }

        public void Execute(MessageTypes.IStrictlyLocalCommand command)
        {
            CommonAssertion(command);

            var commandHandler = _handlerRegistry.GetCommandHandler(command);
            commandHandler.Invoke(command);
        }

        public TResult Execute<TQuery, TResult>(MessageTypes.IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : MessageTypes.IStrictlyLocalQuery<TQuery, TResult>
        {
            CommonAssertion(query);

            // ReSharper disable once SuspiciousTypeConversion.Global
            //Todo: Test and stop disabling ReSharper warning
            if(query is MessageTypes.ICreateMyOwnResultQuery<TResult> selfCreating)
                return selfCreating.CreateResult();

            var queryHandler = _handlerRegistry.GetQueryHandler(query);
            return queryHandler.Invoke(query);
        }

        void CommonAssertion(MessageTypes.IMessage message)
        {
            _contextGuard.AssertNoContextChangeOccurred(this);
            MessageInspector.AssertValidToExecuteLocally(message);
        }
    }
}
