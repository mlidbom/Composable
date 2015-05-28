using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Composable.ServiceBus
{
    internal static class MessageHandlerReferenceExtensions
    {
        private static readonly ConcurrentDictionary<MessageHandlerId, List<MessageHandlerMethod>> MessageHandlerClassCache =
            new ConcurrentDictionary<MessageHandlerId, List<MessageHandlerMethod>>();

        ///<summary>Invokes all the handlers in messageHandler that implements handlerInterfaceType and handles a message matching the type of message.</summary>
        internal static void Invoke(this MessageHandlersResolver.MessageHandlerReference messageHandlerReference, object message)
        {
            var handlerType = messageHandlerReference.Instance.GetType();
            new MessageHandlerMethod(handlerType, messageHandlerReference.GenericInterfaceImplemented.GetGenericArguments()[0], messageHandlerReference.GenericInterfaceImplemented)
            .Invoke(messageHandlerReference.Instance, message);
        }

        private static List<MessageHandlerMethod> GetAvailableMethodsFor(this MessageHandlersResolver.MessageHandlerReference messageHandlerReference, object message)
        {


            var messageHandler = messageHandlerReference.Instance;
            Type implementingClass = messageHandler.GetType();

            List<MessageHandlerMethod> messageHandlerMethods;
            var messageHandlerClassId = new MessageHandlerId(implementingClass: implementingClass, genericInterfaceImplemented: messageHandlerReference.GenericInterfaceImplemented);

            if (!MessageHandlerClassCache.TryGetValue(messageHandlerClassId, out messageHandlerMethods))
            {
                messageHandlerMethods = CreateMessageHandlerMethods(genericInterfaceImplemented: messageHandlerReference.GenericInterfaceImplemented, implementingClass: implementingClass);
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
                .Select(messageType => new MessageHandlerMethod(implementingClass: implementingClass, messageType: messageType, genericInterfaceImplemented: genericInterfaceImplemented))
                .ToList();
        }

        private class MessageHandlerId
        {
            private Type ImplementingClass { get; set; }
            private Type GenericInterfaceImplemented { get; set; }

            public MessageHandlerId(Type implementingClass, Type genericInterfaceImplemented)
            {
                ImplementingClass = implementingClass;
                GenericInterfaceImplemented = genericInterfaceImplemented;
            }

            override public bool Equals(object other)
            {
                if (other == null || GetType() != other.GetType())
                {
                    return false;
                }

                return Equals((MessageHandlerId)other);
            }

            private bool Equals(MessageHandlerId other)
            {
                return other.ImplementingClass == ImplementingClass && other.GenericInterfaceImplemented == GenericInterfaceImplemented;
            }

            override public int GetHashCode()
            {
                return ImplementingClass.GetHashCode() + GenericInterfaceImplemented.GetHashCode();
            }
        }
    }


}
