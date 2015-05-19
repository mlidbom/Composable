using Composable.System.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Composable.ServiceBus
{
    internal static class MessageHandlerInvoker
    {
        private static readonly ConcurrentDictionary<Type, List<MethodToInvokeForSpecificTypeOfMessage>> HandlerToMessageHandlersMap = new ConcurrentDictionary<Type, List<MethodToInvokeForSpecificTypeOfMessage>>();

        public static void Invoke(this MessageHandlerResolver.MessageHandlers messageHandlers)
        {
            foreach (var handlerInstance in messageHandlers.HandlerInstances)
            {
                GetMethodsToInvoke(handlerInstance.GetType(), messageHandlers.HandlerInterfaceType)
                    .Where(holder => holder.HandledMessageType.IsInstanceOfType(messageHandlers.Message))
                    .Select(holder => holder.HandlerMethod)
                    .ForEach(handlerMethod => handlerMethod(handlerInstance, messageHandlers.Message));
            }
        }

        //Creates a list of handlers, one per handler type. Each handler is a mapping between message type and what method to invoke when handling this specific type of message.
        private static IEnumerable<MethodToInvokeForSpecificTypeOfMessage> GetMethodsToInvoke(Type handlerInstanceType, Type handlerInterfaceType)
        {
            List<MethodToInvokeForSpecificTypeOfMessage> messageHandleHolders;

            if (!HandlerToMessageHandlersMap.TryGetValue(handlerInstanceType, out messageHandleHolders))
            {
                var holders = new List<MethodToInvokeForSpecificTypeOfMessage>();

                    var handledMessageTypes = handlerInstanceType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                    .Select(i => i.GetGenericArguments().First())
                    .ToList();

                    handledMessageTypes.ForEach(messageType =>
                    {
                        var action = TryGetMethodToInvokeWhenHandlingTypeOfMessage(handlerInstanceType, messageType, handlerInterfaceType);
                        if (action != null)
                        {
                            holders.Add(new MethodToInvokeForSpecificTypeOfMessage(messageType, action));
                        }

                    });
                

                HandlerToMessageHandlersMap[handlerInstanceType] = holders;
            }

            return HandlerToMessageHandlersMap[handlerInstanceType];
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

    ///<summary>Used to hold a single implementation of IHandleMessages</summary>
    internal class MethodToInvokeForSpecificTypeOfMessage
    {
        public Type HandledMessageType { get; private set; }
        public Action<object, object> HandlerMethod { get; private set; }

        public MethodToInvokeForSpecificTypeOfMessage(Type handledMessageType, Action<object, object> handlerMethod)
        {
            HandledMessageType = handledMessageType;
            HandlerMethod = handlerMethod;
        }
    }
}
