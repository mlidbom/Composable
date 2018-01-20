using System.Collections.Generic;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    [UsedImplicitly] class InProcessServiceBus : IInProcessServiceBus
    {
        readonly IMessageHandlerRegistry _handlerRegistry;

        public InProcessServiceBus(IMessageHandlerRegistry handlerRegistry) => _handlerRegistry = handlerRegistry;

        void IInProcessServiceBus.Publish(ITransactionalExactlyOnceDeliveryEvent anEvent)
        {
            _handlerRegistry.CreateEventDispatcher()
                            .Dispatch(anEvent);
        }


        public TResult Send<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command)
        {
            var returnValue = _handlerRegistry.GetCommandHandler(command)
                                              .Invoke(command);
            return returnValue;
        }


        void IInProcessServiceBus.Send(ITransactionalExactlyOnceDeliveryCommand message)
        {
            _handlerRegistry.GetCommandHandler(message)(message);
        }

        TResult IInProcessServiceBus.Query<TResult>(IQuery<TResult> query)
        {
            var returnValue = _handlerRegistry.GetQueryHandler(query)
                                              .Invoke(query);
            return returnValue;
        }
    }
}
