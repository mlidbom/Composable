using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Castle.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;
using NServiceBus;

namespace Composable.ServiceBus
{
    internal static class MessageHandlerInvoker
    {
        private static readonly ConcurrentDictionary<Type, List<MethodToInvokeForSpecificTypeOfMessage>> HandlerToMessageHandlersMap = new ConcurrentDictionary<Type, List<MethodToInvokeForSpecificTypeOfMessage>>();
       
        private static readonly List<Type> SupportedHandlerInterfaceTypes = new List<Type> { typeof(IHandleMessages<>), typeof(IHandleInProcessMessages<>) };
        private static readonly Type NonSupporetedHandlerInterfaceType = typeof(IHandleRemoteMessages<>);

        public static void Invoke<TMessage>(IEnumerable<object> handlerInstances, TMessage message)
        {
            foreach (var handlerInstance in handlerInstances)
            {
                GetMethodsToInvoke(handlerInstance.GetType())
                    .Where(holder => holder.HandledMessageType.IsInstanceOfType(message))
                    .Select(holder => holder.HandlerMethod)
                    .ForEach(handlerMethod => handlerMethod(handlerInstance, message));
            }
        }

        internal static List<object> ResolveMessageHandlers<TMessage>(this IWindsorContainer @this, TMessage message)
        {
            var handlers = new List<object>();
            foreach(var handlerType in GetHandlerTypes(@this, message))
            {
                foreach(var handlerInstance in @this.ResolveAll(handlerType).Cast<object>()) //if one handler implements many interfaces, it will be invoked many times.
                {
                    if(!handlers.Contains(handlerInstance))
                    {
                        handlers.Add(handlerInstance);
                    }
                }
            }
           
            return handlers;
        }

        private static IEnumerable<Type> GetHandlerTypes(IWindsorContainer container, object message)
        {
            var handlerInterfaces = new List<Type>();
            foreach (var supportedHandlerInterfaceType in SupportedHandlerInterfaceTypes)
            {
                handlerInterfaces.AddRange(message.GetType().GetAllTypesInheritedOrImplemented()
                    .Where(m => m.Implements(typeof(IMessage)))
                    .Select(m => supportedHandlerInterfaceType.MakeGenericType(m)));
            }
        
            return handlerInterfaces  
                .Where(i => container.Kernel.HasComponent(i))
                .Where(i => !NonSupporetedHandlerInterfaceType.IsAssignableFrom(i))
                .ToArray();
        }

        internal static bool CanResolveMessageHandler(this IWindsorContainer @this, object message)
        {
            return @this.ResolveMessageHandlers(message).Any();
        }

        //Creates a list of handlers, one per handler type. Each handler is a mapping between message type and what method to invoke when handling this specific type of message.
        private static IEnumerable<MethodToInvokeForSpecificTypeOfMessage> GetMethodsToInvoke(Type handlerInstanceType)
        {
            List<MethodToInvokeForSpecificTypeOfMessage> messageHandleHolders;

            if (!HandlerToMessageHandlersMap.TryGetValue(handlerInstanceType, out messageHandleHolders))
            {
                var holders = new List<MethodToInvokeForSpecificTypeOfMessage>();

                foreach (var handlerInterfaceType in SupportedHandlerInterfaceTypes)
                {
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
                }

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
