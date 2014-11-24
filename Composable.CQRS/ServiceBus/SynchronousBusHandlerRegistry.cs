using System;
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
        private static readonly ConcurrentDictionary<Type, List<Action<object, object>>> _handlerMapper = new ConcurrentDictionary<Type, List<Action<object, object>>>();
        public static IEnumerable<Action<object, object>> Register(object handler)
        {
            List<Action<object, object>> methodList;
            var handlerType = handler.GetType();
            if (!_handlerMapper.TryGetValue(handlerType, out methodList))
            {
                _handlerMapper[handlerType] = GetHandleMethods(handler);
            }
            return _handlerMapper[handlerType];
        }

        private static List<Action<object, object>> GetHandleMethods(object handler)
        {
            var methodList = new List<Action<object, object>>();

            var baseMessages = handler.GetType().GetInterfaces()
                .Where(i =>i.IsGenericType&&i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .Select(i => i.GetGenericArguments().First())
                .ToList();

            baseMessages.ForEach(message =>
                                 {
                                     var action = CreateMethod(handler.GetType(), message);
                                     if (action != null)
                                     {
                                         methodList.Add(action);
                                     }
                                 });
            return methodList;
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
}
