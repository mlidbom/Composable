using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Composable.ServiceBus
{
    internal static class IndividualHandlerInvocationMethodFactory
    {
        private static readonly ConcurrentDictionary<HandlerId, List<MethodToInvokeOnMessageHandlerByMessageType>> HandlerToMessageHandlersMap = new ConcurrentDictionary<HandlerId, List<MethodToInvokeOnMessageHandlerByMessageType>>();

        private class HandlerId
        {
            public Type InstanceType { get; private set; }
            public Type HandlerInterfaceType { get; private set; }

            public HandlerId(Type instanceType, Type handlerInterfaceType)
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
        internal static IEnumerable<Action<object, object>> GetMethodsToInvoke(Type handlerInstanceType, Type handlerInterfaceType, object message)
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

            return HandlerToMessageHandlersMap[new HandlerId(handlerInstanceType, handlerInterfaceType)]
                .Where(holder => holder.HandledMessageType.IsInstanceOfType(message))
                        .Select(holder => holder.HandlerMethod);
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
}