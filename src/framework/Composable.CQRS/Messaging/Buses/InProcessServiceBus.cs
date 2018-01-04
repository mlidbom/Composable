using System.Collections.Generic;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    [UsedImplicitly] class InProcessServiceBus : IInProcessServiceBus, IMessageSpy
    {
        readonly IMessageHandlerRegistry _handlerRegistry;

        public InProcessServiceBus(IMessageHandlerRegistry handlerRegistry) => _handlerRegistry = handlerRegistry;

        void IInProcessServiceBus.Publish(ITransactionalExactlyOnceDeliveryEvent anEvent)
        {
            _handlerRegistry.CreateEventDispatcher()
                            .Dispatch(anEvent);
            AfterDispatchingMessage(anEvent);
        }


        public TResult Send<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command)
        {

            var returnValue = _handlerRegistry.GetCommandHandler(command)
                                              .Invoke(command);
            AfterDispatchingMessage(command);
            AfterDispatchingMessage(returnValue);
            return returnValue;
        }


        void IInProcessServiceBus.Send(ITransactionalExactlyOnceDeliveryCommand message)
        {
            _handlerRegistry.GetCommandHandler(message)(message);
            AfterDispatchingMessage(message);
        }

        TResult IInProcessServiceBus.Query<TResult>(IQuery<TResult> query)
        {
            var returnValue = _handlerRegistry.GetQueryHandler(query)
                                              .Invoke(query);
            AfterDispatchingMessage(query);
            AfterDispatchingMessage(returnValue);
            return returnValue;
        }

        readonly List<object> _dispatchedMessages = new List<object>();
        public IEnumerable<object> DispatchedMessages => _dispatchedMessages;

        protected virtual void AfterDispatchingMessage(object message)
        {
            _dispatchedMessages.Add(message);
        }
    }
}
