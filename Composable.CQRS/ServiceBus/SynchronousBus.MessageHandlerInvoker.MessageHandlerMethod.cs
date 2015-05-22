using System;
using System.Linq;
using System.Linq.Expressions;
using Composable.System;

namespace Composable.ServiceBus
{
    public partial class SynchronousBus
    {
        private static partial class MessageHandlerInvoker
        {
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
