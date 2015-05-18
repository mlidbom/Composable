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
        private static readonly ConcurrentDictionary<Type, List<MessageHandler>> HandlerToMessageHandlersMap = new ConcurrentDictionary<Type, List<MessageHandler>>();
       
        private static readonly List<Type> SupportedHandlerInterfaceTypes = new List<Type> { typeof(IHandleMessages<>), typeof(IHandleInProcessMessages<>) };
        private static readonly Type NonSupporetedHandlerInterfaceType = typeof(IHandleRemoteMessages<>);

        public static void Invoke<TMessage>(object handler, TMessage message)
        {
            List<MessageHandler> messageHandleHolders;
            var handlerType = handler.GetType();
            if (!HandlerToMessageHandlersMap.TryGetValue(handlerType, out messageHandleHolders))
            {
                HandlerToMessageHandlersMap[handlerType] = GetIHandleMessageImplementations(handler.GetType());
            }

            HandlerToMessageHandlersMap[handlerType]
                .Where(messageHandler => messageHandler.HandledMessageType.IsInstanceOfType(message))
                .Select(holder => holder.HandlerMethod)
                .ForEach(method => method(handler, message));
        }

        public static void Invoke<TMessage>(IWindsorContainer container, TMessage message, bool assertSingleHandler = false)
        {
            var handlers = container.ResolveMessageHandlers(message);

            if (assertSingleHandler)
            {
                if (handlers.None())
                {
                    throw new NoHandlerException(message.GetType());
                }

                AssertOnlyOneHandlerRegistered(message, handlers);
            }

            try
            {
                foreach (var handler in handlers)
                {
                    GetMessageHandlers(handler)
                        .Where(holder => holder.HandledMessageType.IsInstanceOfType(message))
                        .Select(holder => holder.HandlerMethod)
                        .ForEach(handlerMethod => handlerMethod(handler, message));
                }
            }
            finally
            {
                handlers.ForEach(container.Release);
            }
        }

        private static IEnumerable<MessageHandler> GetMessageHandlers(object handler)
        {
            List<MessageHandler> messageHandleHolders;

            var handlerInstanceType = handler.GetType();

            if (!HandlerToMessageHandlersMap.TryGetValue(handlerInstanceType, out messageHandleHolders))
            {
                HandlerToMessageHandlersMap[handlerInstanceType] = GetIHandleMessageImplementations(handler.GetType());
            }

            return HandlerToMessageHandlersMap[handlerInstanceType];
        }

        private static List<object> ResolveMessageHandlers<TMessage>(this IWindsorContainer @this, TMessage message)
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

        internal static IEnumerable<Type> GetHandlerTypes(IWindsorContainer container, object message)
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

        private static void AssertOnlyOneHandlerRegistered(object message, List<object> handlers)
        {
            var realHandlers = handlers.Except(handlers.OfType<ISynchronousBusMessageSpy>()).ToList();
            if (realHandlers.Count() > 1)
            {
                throw new MultipleMessageHandlersRegisteredException(message, realHandlers);
            }
        }


        //Creates a list of handlers. One per implementation of IHandleMessages in the handlerType
        private static List<MessageHandler> GetIHandleMessageImplementations(Type handlerInstanceType)
        {
            var holders = new List<MessageHandler>();

            foreach (var handlerInterfaceType in SupportedHandlerInterfaceTypes)
            {
                var handledMessageTypes = handlerInstanceType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                .Select(i => i.GetGenericArguments().First())
                .ToList();

                handledMessageTypes.ForEach(messageType =>
                {
                    var action = TryGetImplementingMethod(handlerInstanceType, messageType, handlerInterfaceType);
                    if (action != null)
                    {
                        holders.Add(new MessageHandler(messageType, action));
                    }

                });
            }
            
            return holders;
        }
        
        //If messageHandlerType implements IHandleMessages<MessageType> then returns an action that can be used to invoke this implementation for a given handler instance.
        private static Action<object, object> TryGetImplementingMethod(Type handlerInstanceType, Type messageType, Type handlerInterfaceType)
        {
            var messageHandlerInterfaceType = handlerInterfaceType.MakeGenericType(messageType); //todo: include IHandleInProcessMessages
            
            if (!messageHandlerInterfaceType.IsAssignableFrom(handlerInstanceType))
            {
                return null;
            }

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
    internal class MessageHandler
    {
        public Type HandledMessageType { get; private set; }
        public Action<object, object> HandlerMethod { get; private set; }

        public MessageHandler(Type handledMessageType, Action<object, object> handlerMethod)
        {
            HandledMessageType = handledMessageType;
            HandlerMethod = handlerMethod;
        }
    }
}
