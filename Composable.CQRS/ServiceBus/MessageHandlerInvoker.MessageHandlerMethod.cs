using System;
using System.Linq;
using System.Linq.Expressions;

namespace Composable.ServiceBus
{
    internal class MessageHandlerMethod
    {
        private Action<object, object> HandlerMethod { get; set; }

        public MessageHandlerMethod(Type implementingClass, Type genericInterfaceImplemented)
        {
            HandlerMethod = CreateHandlerMethodInvoker(implementingClass, genericInterfaceImplemented);
        }

        public void Invoke(object handler, object message)
        {
            HandlerMethod(handler, message);
        }

        //Returns an action that can be used to invoke this handler for a specific type of message.
        private static Action<object, object> CreateHandlerMethodInvoker(Type implementingClass, Type genericInterfaceImplemented)
        {
            var methodInfo = implementingClass.GetInterfaceMap(genericInterfaceImplemented).TargetMethods.Single();
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
