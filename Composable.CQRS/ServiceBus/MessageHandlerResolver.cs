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
                .Select(handlerType => new MessageHandlerReference(handlerType.GenericInterfaceImplemented, instance: handlerType.HandlerCreator()));
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
                new MessageHandlerMethod(handlerType, this.GenericInterfaceImplemented).Invoke(this.Instance, message);
            }
        }

        private class MessageHandlerTypeReference
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
        }


        private IEnumerable<MessageHandlerTypeReference> GetHandlerTypes(object message)
        {
            var handlerTypes = _handlerInterfaces.SelectMany(handlerInterface => GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(message, handlerInterface));

            var excludedMessageHandlerTypes = GetExcludedHandlerTypes(message);

            handlerTypes = handlerTypes.Where(handler => excludedMessageHandlerTypes.None(remoteHandlerType => handler.ImplementingClass.Implements(remoteHandlerType)));

            return handlerTypes;
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

        private IEnumerable<MessageHandlerTypeReference> GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(object message, Type genericInterface)
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
