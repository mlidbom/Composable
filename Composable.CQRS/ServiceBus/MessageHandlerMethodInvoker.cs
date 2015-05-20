using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Composable.System.Linq;

namespace Composable.ServiceBus
{
    internal static class MessageHandlerMethodInvoker
    {
        private static readonly ConcurrentDictionary<MessageHandlerId, List<MessageHandlerMethod>> MessageHandlerClassCache =
            new ConcurrentDictionary<MessageHandlerId, List<MessageHandlerMethod>>();

        private class MessageHandlerId
        {
            private Type InstanceType { get; set; }
            private Type HandlerInterfaceType { get; set; }

            public MessageHandlerId(Type instanceType, Type handlerInterfaceType)
            {
                InstanceType = instanceType;
                HandlerInterfaceType = handlerInterfaceType;
            }

            override public bool Equals(object other)
            {
                if(other == null || GetType() != other.GetType())
                {
                    return false;
                }

                return Equals((MessageHandlerId)other);
            }

            private bool Equals(MessageHandlerId other)
            {
                return other.InstanceType == InstanceType && other.HandlerInterfaceType == HandlerInterfaceType;
            }

            override public int GetHashCode()
            {
                return InstanceType.GetHashCode() + HandlerInterfaceType.GetHashCode();
            }
        }

        ///<summary>Invokes all the handlers in messageHandler that implements handlerInterfaceType and handles a message matching the type of message.</summary>
        internal static void InvokeHandlerMethods(object messageHandler, object message, Type handlerInterfaceType)
        {
            GetHandlerMethods(messageHandler, handlerInterfaceType, message)                
                .ForEach(handlerMethod => handlerMethod.Invoke(handler: messageHandler, message: message));
        }

        private static List<MessageHandlerMethod> GetHandlerMethods(object messageHandler, Type handlerInterfaceType, object message)
        {
            Type handlerInstanceType = messageHandler.GetType();

            List<MessageHandlerMethod> messageHandlerMethods;
            var messageHandlerClassId = new MessageHandlerId(handlerInstanceType, handlerInterfaceType);

            if(!MessageHandlerClassCache.TryGetValue(messageHandlerClassId, out messageHandlerMethods))
            {
                messageHandlerMethods = CreateMessageHandlerMethods(handlerInterfaceType, handlerInstanceType);
                MessageHandlerClassCache[messageHandlerClassId] = messageHandlerMethods;
            }
            return messageHandlerMethods
                .Where(messageHandlerMethodReference => messageHandlerMethodReference.HandledMessageType.IsInstanceOfType(message))
                .ToList();
        }

        private static List<MessageHandlerMethod> CreateMessageHandlerMethods(Type handlerInterfaceType, Type handlerInstanceType)
        {
            return handlerInstanceType.GetInterfaces()
                .Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == handlerInterfaceType)
                .Select(foundHandlerInterface => foundHandlerInterface.GetGenericArguments().Single())
                .Select(messageType => new MessageHandlerMethod(handlerInstanceType, messageType, handlerInterfaceType))
                .ToList();
        }

        ///<summary>Used to hold a single implementation of a message handler</summary>
        private class MessageHandlerMethod
        {
            public Type HandledMessageType { get; private set; }
            public Action<object, object> HandlerMethod { get; private set; }

            public MessageHandlerMethod(Type handlerInstanceType, Type handledMessageType, Type handlerInterfaceType)
            {
                HandledMessageType = handledMessageType;
                HandlerMethod = CreateHandlerMethodInvoker(handlerInstanceType, handledMessageType, handlerInterfaceType);
            }

            public void Invoke(object handler, object message)
            {
                HandlerMethod(handler, message);
            }

            //Returns an action that can be used to invoke this handler for a specific type of message.
            private static Action<object, object> CreateHandlerMethodInvoker(Type handlerInstanceType, Type messageType, Type handlerInterfaceType)
            {
                var messageHandlerInterfaceType = handlerInterfaceType.MakeGenericType(messageType);

                var methodInfo = handlerInstanceType.GetInterfaceMap(messageHandlerInterfaceType).TargetMethods.Single();
                var messageHandlerParameter = Expression.Parameter(typeof(object));
                var messageParameter = Expression.Parameter(typeof(object));

                var convertMessageHandlerParameter = Expression.Convert(messageHandlerParameter, handlerInstanceType);
                var convertMessageParameter = Expression.Convert(messageParameter, methodInfo.GetParameters().Single().ParameterType);
                var callMessageHandlerExpression = Expression.Call(instance: convertMessageHandlerParameter, method: methodInfo, arguments: convertMessageParameter);

                return Expression.Lambda<Action<object, object>>(
                    body: callMessageHandlerExpression,
                    parameters: new[]
                            {
                                messageHandlerParameter,
                                messageParameter
                            }).Compile();
            }
        }
    }
}
