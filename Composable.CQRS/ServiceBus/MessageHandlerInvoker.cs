using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Composable.System.Linq;

namespace Composable.ServiceBus
{
    internal static class MessageHandlerInvoker
    {
        private static readonly ConcurrentDictionary<MapKey, List<MethodToInvokeOnMessageHandlerByMessageType>> HandlerToMessageHandlersMap = new ConcurrentDictionary<MapKey, List<MethodToInvokeOnMessageHandlerByMessageType>>();

        internal static void Invoke<TMessage>(IEnumerable<object> handlerInstances, Type interfaceType, TMessage message)
        {
            foreach (var handlerInstance in handlerInstances)
            {
                GetMethodsToInvoke(handlerInstance.GetType(), interfaceType)
                    .Where(holder => holder.HandledMessageType.IsInstanceOfType(message))
                    .Select(holder => holder.HandlerMethod)
                    .ForEach(handlerMethod => handlerMethod(handlerInstance, message));
            }
        }

        private class MapKey
        {
            public Type HandlerInstaceType { get; private set; }
            public Type HandlerInterfaceType { get; private set; }

            public MapKey(Type handlerInstaceType,Type handlerInterfaceType)
            {
                HandlerInstaceType = handlerInstaceType;
                HandlerInterfaceType = handlerInterfaceType;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;

                var key = obj as MapKey;
                return key.HandlerInstaceType == HandlerInstaceType && key.HandlerInterfaceType == HandlerInterfaceType;
            }

            public override int GetHashCode()
            {
                return HandlerInstaceType.GetHashCode() + HandlerInterfaceType.GetHashCode();
            }
        }

        //Creates a list of handlers, one per handler type. Each handler is a mapping between message type and what method to invoke when handling this specific type of message.
        private static IEnumerable<MethodToInvokeOnMessageHandlerByMessageType> GetMethodsToInvoke(Type handlerInstanceType, Type handlerInterfaceType)
        {
            List<MethodToInvokeOnMessageHandlerByMessageType> messageHandleHolders;

            if (!HandlerToMessageHandlersMap.TryGetValue(new MapKey(handlerInstanceType, handlerInterfaceType), out messageHandleHolders))
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


                    HandlerToMessageHandlersMap[new MapKey(handlerInstanceType, handlerInterfaceType)] = holders;
            }

            return HandlerToMessageHandlersMap[new MapKey(handlerInstanceType, handlerInterfaceType)];
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
