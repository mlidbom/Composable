using System;
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

        public MessageHandlersResolver(IWindsorContainer container, IEnumerable<Type> handlerInterfaces, IEnumerable<Type> excludedHandlerInterfaces)
        {
            _container = container;
            _excludedHandlerInterfaces = excludedHandlerInterfaces;
            _handlerInterfaces = handlerInterfaces;
        }

        public bool HasHandlerFor(object message)
        {
            return GetHandlerTypes(message).Any();
        }

        public IEnumerable<MessageHandlerReference> GetHandlers(object message)
        {
            return GetHandlerTypes(message)
                .Select(handlerType => new MessageHandlerReference(handlerType.GenericInterfaceImplemented, instance: _container.Resolve(handlerType.Name, handlerType.ServiceInterface)));
        }


        internal class MessageHandlerReference
        {
            public MessageHandlerReference(Type genericInterfaceImplemented, object instance)
            {
                GenericInterfaceImplemented = genericInterfaceImplemented;
                Instance = instance;
            }

            internal Type GenericInterfaceImplemented { get; private set; }
            public object Instance { get; private set; }

            internal void Invoke(object message)
            {
                var handlerType = Instance.GetType();
                new MessageHandlerMethod(handlerType, this.GenericInterfaceImplemented.GetGenericArguments()[0], this.GenericInterfaceImplemented).Invoke(this.Instance, message);
            }
        }

        private class MessageHandlerTypeReference
        {
            public MessageHandlerTypeReference(Type genericInterfaceImplemented, Type implementingClass, Type serviceInterface, string name)
            {
                GenericInterfaceImplemented = genericInterfaceImplemented;
                ImplementingClass = implementingClass;
                ServiceInterface = serviceInterface;
                Name = name;
            }
            public string Name { get; private set; }
            public Type ImplementingClass { get; private set; }
            public Type GenericInterfaceImplemented { get; private set; }
            public Type ServiceInterface { get; private set; }
        }


        private IEnumerable<MessageHandlerTypeReference> GetHandlerTypes(object message)
        {
            var allHandlerTypes = _handlerInterfaces.SelectMany(handlerInterface => GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(message, handlerInterface));

            var excludedMessageHandlerTypes = GetExcludedHandlerTypes(message);

            var handlersToCall = allHandlerTypes
                .Where(handler => excludedMessageHandlerTypes.None(remoteHandlerType => handler.ImplementingClass.Implements(remoteHandlerType)))
                .ToList();

            return handlersToCall;
        }

        private IEnumerable<Type> GetExcludedHandlerTypes(object message)
        {
            return GetCanBeHandledMessageTypes(message)
                .SelectMany(messageType => _excludedHandlerInterfaces.Select(excludedHandlerInterface => excludedHandlerInterface.MakeGenericType(messageType)))
                .ToList();
        }

        private IEnumerable<Type> GetCanBeHandledMessageTypes(object message)
        {
            return message.GetType().GetAllTypesInheritedOrImplemented()
                          .Where(type => type.Implements(typeof(IMessage)));
        }

        private IEnumerable<Type> GenerateMessageHanderTypesByGenericInterface(object message, Type genericMessageHandlerInterface)
        {
            return GetCanBeHandledMessageTypes(message).Select(typeImplementedByMessageThatImplementsIMessage => genericMessageHandlerInterface.MakeGenericType(typeImplementedByMessageThatImplementsIMessage));
        }

        private IEnumerable<MessageHandlerTypeReference> GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(object message, Type genericInterface)
        {
            var messageHandlerTypes = GenerateMessageHanderTypesByGenericInterface(message, genericInterface);

            foreach (var component in _container.Kernel.GetAssignableHandlers(typeof(object)))
            {
                foreach (var messageHandlerType in messageHandlerTypes)
                {
                    if (messageHandlerType.IsAssignableFrom(component.ComponentModel.Implementation))
                    {
                        yield return new MessageHandlerTypeReference(genericInterfaceImplemented: messageHandlerType, implementingClass: component.ComponentModel.Implementation, serviceInterface: component.ComponentModel.Services.First(), name: component.ComponentModel.Name);
                    }
                }
            }
        }
    }
}
