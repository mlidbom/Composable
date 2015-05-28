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

            var handlers = GetHandlerTypes(message)
                .SelectMany(handlerType => _container.ResolveAll(handlerType.ServiceInterface).Cast<object>()
                .Select(handler => new MessageHandlerReference(handlerType.GenericInterfaceImplemented, instance: handler)))
                .ToList();


            var excludedHandlerTypes = GetExcludedHandlerTypes(message);
            var handlersToCall =
                handlers.Where(handler => excludedHandlerTypes.None(remoteMessageHandlerType => remoteMessageHandlerType.IsInstanceOfType(handler.Instance)))
                    .ToList();

            return handlers;
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
            public MessageHandlerTypeReference(Type genericInterfaceImplemented, Type implementingClass, Type serviceInterface)
            {
                GenericInterfaceImplemented = genericInterfaceImplemented;
                ImplementingClass = implementingClass;
                ServiceInterface = serviceInterface;
            }

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

        private IEnumerable<MessageHandlerTypeReference> GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(object message, Type genericInterface)
        {
            var messageHandlerTypes = message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(typeImplementedByMessage => typeImplementedByMessage.Implements(typeof(IMessage)))
                .Select(typeImplementedByMessageThatImplementsIMessage => genericInterface.MakeGenericType(typeImplementedByMessageThatImplementsIMessage));

            foreach (var component in _container.Kernel.GetAssignableHandlers(typeof(object)))
            {
                foreach (var messageHandlerType in messageHandlerTypes)
                {
                    if (messageHandlerType.IsAssignableFrom(component.ComponentModel.Implementation))
                    {
                        yield return new MessageHandlerTypeReference(genericInterfaceImplemented: messageHandlerType, implementingClass: component.ComponentModel.Implementation, serviceInterface: component.ComponentModel.Services.First());
                    }
                }
            }
        }
    }
}
