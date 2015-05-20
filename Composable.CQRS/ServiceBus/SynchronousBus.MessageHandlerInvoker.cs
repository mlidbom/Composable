using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Composable.ServiceBus
{
    public partial class SynchronousBus
    {
        private static partial class MessageHandlerInvoker
        {
            private static readonly ConcurrentDictionary<MessageHandlerId, List<MessageHandlerMethod>> MessageHandlerClassCache =
                new ConcurrentDictionary<MessageHandlerId, List<MessageHandlerMethod>>();

            ///<summary>Invokes all the handlers in messageHandler that implements handlerInterfaceType and handles a message matching the type of message.</summary>
            internal static void InvokeHandlerMethods(object messageHandler, object message, Type genericInterfaceImplemented)
            {
                GetHandlerMethods(messageHandler, genericInterfaceImplemented, message)
                    .ForEach(handlerMethod => handlerMethod.Invoke(handler: messageHandler, message: message));
            }

            private static List<MessageHandlerMethod> GetHandlerMethods(object messageHandler, Type genericInterfaceImplemented, object message)
            {
                Type implementingClass = messageHandler.GetType();

                List<MessageHandlerMethod> messageHandlerMethods;
                var messageHandlerClassId = new MessageHandlerId(implementingClass: implementingClass, genericInterfaceImplemented: genericInterfaceImplemented);

                if(!MessageHandlerClassCache.TryGetValue(messageHandlerClassId, out messageHandlerMethods))
                {
                    messageHandlerMethods = CreateMessageHandlerMethods(genericInterfaceImplemented: genericInterfaceImplemented, implementingClass: implementingClass);
                    MessageHandlerClassCache[messageHandlerClassId] = messageHandlerMethods;
                }
                return messageHandlerMethods
                    .Where(messageHandlerMethod => messageHandlerMethod.Handles(message))
                    .ToList();
            }

            private static List<MessageHandlerMethod> CreateMessageHandlerMethods(Type genericInterfaceImplemented, Type implementingClass)
            {
                return implementingClass.GetInterfaces()
                    .Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == genericInterfaceImplemented)
                    .Select(genericInterfaceImplementation => genericInterfaceImplementation.GetGenericArguments().Single())
                    .Select(
                        messageType =>
                            new MessageHandlerMethod(implementingClass: implementingClass, messageType: messageType, genericInterfaceImplemented: genericInterfaceImplemented))
                    .ToList();
            }
        }
    }
}
