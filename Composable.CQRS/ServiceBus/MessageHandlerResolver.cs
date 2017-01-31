using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.ServiceBus
{
    class MessageHandlersResolver
    {
        readonly IWindsorContainer _container;
        readonly IEnumerable<Type> _handlerInterfaces;
        readonly IEnumerable<Type> _excludedHandlerInterfaces;
        readonly IDictionary<Type, IEnumerable<MessageHandlerReference>> _cache;

        public MessageHandlersResolver(IWindsorContainer container, IEnumerable<Type> handlerInterfaces, IEnumerable<Type> excludedHandlerInterfaces)
        {
            _cache = new ConcurrentDictionary<Type, IEnumerable<MessageHandlerReference>>();
            _container = container;
            _excludedHandlerInterfaces = excludedHandlerInterfaces;
            _handlerInterfaces = handlerInterfaces;
        }

        public bool HasHandlerFor(object message)
        {
            return GetHandlers(message).Any();
        }

        internal IEnumerable<MessageHandlerReference> GetHandlers(object message)
        {
            var messageType = message.GetType();
            if (!_cache.ContainsKey(messageType))
            {
                var handlerTypes = FilterRepeatedHandlers(_handlerInterfaces.SelectMany(handlerInterface => GetRegisteredHandlerTypesForMessage(message, handlerInterface)));
                var excludedMessageHandlerTypes = GetExcludedHandlerTypes(message);
                handlerTypes = handlerTypes.Where(handler => excludedMessageHandlerTypes.None(remoteHandlerType => handler.ImplementingClass.Implements(remoteHandlerType))).ToList();
                _cache[messageType] = handlerTypes;
            }

            return _cache[messageType];
        }

        IEnumerable<MessageHandlerReference> FilterRepeatedHandlers(IEnumerable<MessageHandlerReference> handlers)
        {
            var methods = new List<MethodInfo>();
            var filteredHandlers = new List<MessageHandlerReference>();
            foreach (var handler in handlers)
            {
                var method = handler.GetHandlerMethod();
                if (!methods.Contains(method))
                {
                    methods.Add(method);
                    filteredHandlers.Add(handler);
                }
            }
            return filteredHandlers;
        }

        IEnumerable<Type> GetExcludedHandlerTypes(object message)
        {
            return _excludedHandlerInterfaces.SelectMany(excludedHandlerInterface => GenerateMessageHanderTypesByGenericInterface(message, excludedHandlerInterface));
        }

        IEnumerable<Type> GetCanBeHandledMessageTypes(object message)
        {
            return message.GetType().GetAllTypesInheritedOrImplemented().Where(type => type.Implements(typeof(IMessage)));
        }

        IEnumerable<Type> GenerateMessageHanderTypesByGenericInterface(object message, Type genericMessageHandlerInterface)
        {
            return GetCanBeHandledMessageTypes(message).Select(typeImplementedByMessageThatImplementsIMessage => genericMessageHandlerInterface.MakeGenericType(typeImplementedByMessageThatImplementsIMessage));
        }

        IEnumerable<MessageHandlerReference> GetRegisteredHandlerTypesForMessage(object message, Type genericInterface)
        {
            var messageHandlerTypes = GenerateMessageHanderTypesByGenericInterface(message, genericInterface);

            foreach (var component in _container.Kernel.GetAssignableHandlers(typeof(object)))
            {
                foreach (var messageHandlerType in messageHandlerTypes)
                {
                    if (messageHandlerType.IsAssignableFrom(component.ComponentModel.Implementation))
                    {
                        yield return new MessageHandlerReference(
                            genericInterfaceImplemented: messageHandlerType,
                            implementingClass: component.ComponentModel.Implementation,
                            handlerCreator: () => _container.Resolve(component.ComponentModel.Name, component.ComponentModel.Services.First()));
                    }
                }
            }
        }

        internal class MessageHandlerReference
        {
            public MessageHandlerReference(Type genericInterfaceImplemented, Type implementingClass, Func<object> handlerCreator)
            {
                GenericInterfaceImplemented = genericInterfaceImplemented;
                ImplementingClass = implementingClass;
                HandlerCreator = handlerCreator;
            }
            public Type ImplementingClass { get; private set; }
            public Type GenericInterfaceImplemented { get; private set; }
            public Func<object> HandlerCreator { get; private set; }

            internal void Invoke(object message)
            {
                var handler = HandlerCreator();
                new MessageHandlerMethod(ImplementingClass, GenericInterfaceImplemented).Invoke(handler, message);
            }

            internal MethodInfo GetHandlerMethod()
            {
                return ImplementingClass.GetInterfaceMap(GenericInterfaceImplemented).TargetMethods.Single();
            }
        }
    }
}
