using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.Windsor;
using Composable.System.Linq;

namespace Composable.ServiceBus
{
    internal class MessageHandlerInvoker
    {
        private static readonly ConcurrentDictionary<HandlerId, List<MethodToInvokeOnMessageHandlerByMessageType>> HandlerToMessageHandlersMap = new ConcurrentDictionary<HandlerId, List<MethodToInvokeOnMessageHandlerByMessageType>>();

        private readonly IWindsorContainer _container;
        private readonly MyMessageHandlerResolver _handlerResolver;

        public MessageHandlerInvoker(IWindsorContainer container)
        {
            _handlerResolver = new MyMessageHandlerResolver(container);
            _container = container;
        }

        internal bool Handles(object message)
        {
            return _handlerResolver.Handles(message);
        }

        internal void Send<TMessage>(TMessage message)
        {
            InternalInvoke(message, isSend:true);
        }

        internal void Publish<TMessage>(TMessage message)
        {
            InternalInvoke(message, isSend:false);
        }

        private void InternalInvoke<TMessage>(TMessage message, bool isSend = false)
        {
            var handlers = _handlerResolver.GetHandlers(message).ToArray();
            try
            {                
                if(isSend)
                {
                    if(handlers.Length == 0)
                    {
                        throw new NoHandlerException(message.GetType());
                    }
                    if(handlers.Length > 1)
                    {
                        var realHandlers = handlers.Select(handler => handler.Instance)
                            .Where(handler => !(handler is ISynchronousBusMessageSpy))
                            .ToList();
                        if(realHandlers.Count > 1)
                        {
                            throw new MultipleMessageHandlersRegisteredException(message, realHandlers);
                        }
                    }
                }

                foreach(var messageHandlerReference in handlers)
                {
                    GetMethodsToInvoke(messageHandlerReference.Instance.GetType(), messageHandlerReference.HandlerInterfaceType)
                        .Where(holder => holder.HandledMessageType.IsInstanceOfType(message))
                        .Select(holder => holder.HandlerMethod)
                        .ForEach(handlerMethod => handlerMethod(messageHandlerReference.Instance, message));
                }

            }
            finally
            {
                handlers.ForEach(_container.Release);
            }
        }

        private class HandlerId
        {
            public Type InstanceType { get; private set; }
            public Type HandlerInterfaceType { get; private set; }

            public HandlerId(Type instanceType,Type handlerInterfaceType)
            {
                InstanceType = instanceType;
                HandlerInterfaceType = handlerInterfaceType;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;

                var key = obj as HandlerId;
                return key.InstanceType == InstanceType && key.HandlerInterfaceType == HandlerInterfaceType;
            }

            public override int GetHashCode()
            {
                return InstanceType.GetHashCode() + HandlerInterfaceType.GetHashCode();
            }
        }

        //Creates a list of handlers, one per handler type. Each handler is a mapping between message type and what method to invoke when handling this specific type of message.
        private static IEnumerable<MethodToInvokeOnMessageHandlerByMessageType> GetMethodsToInvoke(Type handlerInstanceType, Type handlerInterfaceType)
        {
            List<MethodToInvokeOnMessageHandlerByMessageType> messageHandleHolders;

            if (!HandlerToMessageHandlersMap.TryGetValue(new HandlerId(handlerInstanceType, handlerInterfaceType), out messageHandleHolders))
            {
                var holders = new List<MethodToInvokeOnMessageHandlerByMessageType>();

                    var handledMessageTypes = handlerInstanceType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                    .Select(i => i.GetGenericArguments().First())
                    .ToList();

                    handledMessageTypes.ForEach(messageType =>
                    {
                        var action = TryGetMethodToInvokeWhenHandlingTypeOfMessage(handlerInstanceType, messageType, handlerInterfaceType);
                        if (action != null)
                        {
                            holders.Add(new MethodToInvokeOnMessageHandlerByMessageType(messageType, action));
                        }

                    });


                    HandlerToMessageHandlersMap[new HandlerId(handlerInstanceType, handlerInterfaceType)] = holders;
            }

            return HandlerToMessageHandlersMap[new HandlerId(handlerInstanceType, handlerInterfaceType)];
        }

        //Returns an action that can be used to invoke this handler for a specific type of message.
        private static Action<object, object> TryGetMethodToInvokeWhenHandlingTypeOfMessage(Type handlerInstanceType, Type messageType, Type handlerInterfaceType)
        {
            var messageHandlerInterfaceType = handlerInterfaceType.MakeGenericType(messageType);
            
            var methodInfo = handlerInstanceType.GetInterfaceMap(messageHandlerInterfaceType).TargetMethods.First();
            var messageHandlerParameter = Expression.Parameter(typeof(object));
            var parameter = Expression.Parameter(typeof(object));

            var convertMessageHandler = Expression.Convert(messageHandlerParameter, handlerInstanceType);
            var convertParameter = Expression.Convert(parameter, methodInfo.GetParameters().First().ParameterType);
            var execute = Expression.Call(convertMessageHandler, methodInfo, convertParameter);
            return Expression.Lambda<Action<object, object>>(execute, messageHandlerParameter, parameter).Compile();
        }
    }

    ///<summary>Used to hold a single implementation of a message handler</summary>
    internal class MethodToInvokeOnMessageHandlerByMessageType
    {
        public Type HandledMessageType { get; private set; }
        public Action<object, object> HandlerMethod { get; private set; }

        public MethodToInvokeOnMessageHandlerByMessageType(Type handledMessageType, Action<object, object> handlerMethod)
        {
            HandledMessageType = handledMessageType;
            HandlerMethod = handlerMethod;
        }
    }
}
