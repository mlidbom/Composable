using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Composable.System;

namespace Composable.ServiceBus
{
    public partial class SynchronousBus
    {
        private static class MessageHandlerInvoker
        {
            private static readonly ConcurrentDictionary<MessageHandlerId, List<MessageHandlerMethod>> MessageHandlerClassCache =
                new ConcurrentDictionary<MessageHandlerId, List<MessageHandlerMethod>>();

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
                    if(other == null || GetType() != other.GetType())
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

            ///<summary>Used to hold a single implementation of a message handler</summary>
            private class MessageHandlerMethod
            {
                private Type MessageType { get; set; }
                private Action<object, object> HandlerMethod { get; set; }

                public MessageHandlerMethod(Type implementingClass, Type messageType, Type genericInterfaceImplemented)
                {
                    MessageType = messageType;
                    HandlerMethod = CreateHandlerMethodInvoker(implementingClass: implementingClass,
                        messageType: messageType,
                        genericInterfaceImplemented: genericInterfaceImplemented);
                }

                public void Invoke(object handler, object message)
                {
                    HandlerMethod(handler, message);
                }

                public bool Handles(object message)
                {
                    return message.IsInstanceOf(MessageType);
                }

                //Returns an action that can be used to invoke this handler for a specific type of message.
                private static Action<object, object> CreateHandlerMethodInvoker(Type implementingClass, Type messageType, Type genericInterfaceImplemented)
                {
                    var messageHandlerInterfaceType = genericInterfaceImplemented.MakeGenericType(messageType);

                    var methodInfo = implementingClass.GetInterfaceMap(messageHandlerInterfaceType).TargetMethods.Single();
                    var messageHandlerParameter = Expression.Parameter(typeof(object));
                    var messageParameter = Expression.Parameter(typeof(object));

                    var convertMessageHandlerParameter = Expression.Convert(messageHandlerParameter, implementingClass);
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
}