using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NServiceBus;

namespace Composable.ServiceBus
{
    public class SynchronousBusHandlerRegistry
    {
        private static readonly ConcurrentDictionary<Type, List<MessageHandleHolder>> _handlerMapper = new ConcurrentDictionary<Type, List<MessageHandleHolder>>();
        public static IEnumerable<Action<object, object>> Register<TMessage>(object handler, TMessage message)
        {
            List<MessageHandleHolder> messageHandleHolders;
            var handlerType = handler.GetType();
            if (!_handlerMapper.TryGetValue(handlerType, out messageHandleHolders))
            {
                _handlerMapper[handlerType] = GetMessageHandleHolders(handler);
            }

            var methodList = _handlerMapper[handlerType]
                .Where(holder => holder.MessageType.IsInstanceOfType(message))
                .Select(holder => holder.HandleMethod);
            return methodList;
        }

        private static List<MessageHandleHolder> GetMessageHandleHolders(object handler)
        {
            var holders = new List<MessageHandleHolder>();

            var baseMessages = handler.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .Select(i => i.GetGenericArguments().First())
                .ToList();

            baseMessages.ForEach(message =>
                                 {
                                     var action = CreateMethod(handler.GetType(), message);
                                     if (action != null)
                                     {

                                         holders.Add(new MessageHandleHolder(message, action));
                                     }

                                 });
            return holders;
        }


        private static Action<object, object> CreateMethod(Type targetType, Type messageType)
        {
            var interfaceType = typeof(IHandleMessages<>).MakeGenericType(messageType);

            if (interfaceType.IsAssignableFrom(targetType))
            {
                var methodInfo = targetType.GetInterfaceMap(interfaceType).TargetMethods.FirstOrDefault();
                if (methodInfo != null)
                {
                    var target = Expression.Parameter(typeof(object));
                    var param = Expression.Parameter(typeof(object));

                    var castTarget = Expression.Convert(target, targetType);
                    var castParam = Expression.Convert(param, methodInfo.GetParameters().First().ParameterType);
                    var execute = Expression.Call(castTarget, methodInfo, castParam);
                    return Expression.Lambda<Action<object, object>>(execute, target, param).Compile();
                }
            }

            return null;
        }
    }

    internal class MessageHandleHolder
    {
        public Type MessageType { get; private set; }
        public Action<object, object> HandleMethod { get; private set; }

        public MessageHandleHolder(Type messageType, Action<object, object> handleMethod)
        {
            MessageType = messageType;
            HandleMethod = handleMethod;
        }
    }
}
