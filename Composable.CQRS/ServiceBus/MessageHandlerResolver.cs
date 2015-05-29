using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Castle.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;
using NServiceBus;

namespace Composable.ServiceBus
{
    internal class MessageHandlersResolver
    {
        private readonly IWindsorContainer _container;
        private readonly IEnumerable<Type> _handlerInterfaces;
        private readonly IEnumerable<Type> _excludedHandlerInterfaces;
        private IDictionary<Type, IEnumerable<MessageHandlerTypeReference>> _cache;

        public MessageHandlersResolver(IWindsorContainer container, IEnumerable<Type> handlerInterfaces, IEnumerable<Type> excludedHandlerInterfaces)
        {
            _cache = new ConcurrentDictionary<Type, IEnumerable<MessageHandlerTypeReference>>();
            _container = container;
            _excludedHandlerInterfaces = excludedHandlerInterfaces;
            _handlerInterfaces = handlerInterfaces;
        }

        public bool HasHandlerFor(object message)
        {
            return GetHandlerTypes(message).Any();
        }

        public IEnumerable<MessageHandlerTypeReference> GetHandlers(object message)
        {
            return GetHandlerTypes(message);
        }



        internal class MessageHandlerTypeReference
        {
            public MessageHandlerTypeReference(Type genericInterfaceImplemented, Type implementingClass, Func<object> handlerCreator)
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
                new MessageHandlerMethod(ImplementingClass, this.GenericInterfaceImplemented).Invoke(handler, message);
            }
        }


        private IEnumerable<MessageHandlerTypeReference> GetHandlerTypes(object message)
        {
            var messageType = message.GetType();
            if (!_cache.ContainsKey(messageType))
            {
                var handlerTypes = _handlerInterfaces.SelectMany(handlerInterface => GetRegisteredHandlerTypesForMessage(message, handlerInterface));
                var excludedMessageHandlerTypes = GetExcludedHandlerTypes(message);
                handlerTypes = handlerTypes.Where(handler => excludedMessageHandlerTypes.None(remoteHandlerType => handler.ImplementingClass.Implements(remoteHandlerType))).ToList();
                _cache[messageType] = handlerTypes;
            }

            return _cache[messageType];
        }

        private IEnumerable<Type> GetExcludedHandlerTypes(object message)
        {
            return _excludedHandlerInterfaces.SelectMany(excludedHandlerInterface => GenerateMessageHanderTypesByGenericInterface(message, excludedHandlerInterface));
        }

        private IEnumerable<Type> GetCanBeHandledMessageTypes(object message)
        {
            return message.GetType().GetAllTypesInheritedOrImplemented().Where(type => type.Implements(typeof(IMessage)));
        }

        private IEnumerable<Type> GenerateMessageHanderTypesByGenericInterface(object message, Type genericMessageHandlerInterface)
        {
            return GetCanBeHandledMessageTypes(message).Select(typeImplementedByMessageThatImplementsIMessage => genericMessageHandlerInterface.MakeGenericType(typeImplementedByMessageThatImplementsIMessage));
        }

        private IEnumerable<MessageHandlerTypeReference> GetRegisteredHandlerTypesForMessage(object message, Type genericInterface)
        {
            var messageHandlerTypes = GenerateMessageHanderTypesByGenericInterface(message, genericInterface);

            foreach (var component in _container.Kernel.GetAssignableHandlers(typeof(object)))
            {
                foreach (var messageHandlerType in messageHandlerTypes)
                {
                    if (messageHandlerType.IsAssignableFrom(component.ComponentModel.Implementation))
                    {
                        yield return new MessageHandlerTypeReference(
                            genericInterfaceImplemented: messageHandlerType,
                            implementingClass: component.ComponentModel.Implementation,
                            handlerCreator: () => _container.Resolve(component.ComponentModel.Name, component.ComponentModel.Services.First()));
                    }
                }
            }
        }
    }
}
