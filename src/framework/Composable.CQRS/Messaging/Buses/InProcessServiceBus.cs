using System.Collections.Generic;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    [UsedImplicitly] class InProcessServiceBus : IInProcessServiceBus, IMessageSpy
    {
        readonly IMessageHandlerRegistry _handlerRegistry;

        public InProcessServiceBus(IMessageHandlerRegistry handlerRegistry) => _handlerRegistry = handlerRegistry;

        void IInProcessServiceBus.Publish(IEvent anEvent)
        {
            _handlerRegistry.CreateEventDispatcher()
                            .Dispatch(anEvent);
            AfterDispatchingMessage(anEvent);
        }


        public TResult Send<TResult>(ICommand<TResult> command) where TResult : IMessage
        {

            var returnValue = _handlerRegistry.GetCommandHandler(command)
                                              .Invoke(command);
            AfterDispatchingMessage(command);
            AfterDispatchingMessage(returnValue);
            return returnValue;
        }


        void IInProcessServiceBus.Send(ICommand message)
        {
            _handlerRegistry.GetCommandHandler(message)(message);
            AfterDispatchingMessage(message);
        }

        TResult IInProcessServiceBus.Get<TResult>(IQuery<TResult> query)
        {
            var returnValue = _handlerRegistry.GetQueryHandler(query)
                                              .Invoke(query);
            AfterDispatchingMessage(query);
            AfterDispatchingMessage(returnValue);
            return returnValue;
        }

        readonly List<IMessage> _dispatchedMessages = new List<IMessage>();
        public IEnumerable<IMessage> DispatchedMessages => _dispatchedMessages;

        protected virtual void AfterDispatchingMessage(IMessage message)
        {
            _dispatchedMessages.Add(message);
        }
    }
}
