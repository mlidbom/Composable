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
            _handlerInterfaces = handlerInterfaces;
            _excludedHandlerInterfaces = excludedHandlerInterfaces;
        }

        public bool HasHandlerFor(object message)
        {
            return GetHandlerTypes(message).Any();
        }

        public IEnumerable<MessageHandlerReference> GetHandlers(object message)
        {
            var handlers = GetHandlerTypes(message)
                .SelectMany(
                    handlerType => _container.ResolveAll(handlerType.HandlerInterface).Cast<object>().Select(handler => new MessageHandlerReference(genericInterfaceImplemented: handlerType.HandlerInterface.GetGenericTypeDefinition(), instance: handler))
                )
                .Distinct() //Remove duplicates for classes that implement more than one interface. 
                .ToList();

            var excludedHandlerTypes = GetExcludedHandlerTypes(message);
            var handlersToCall =
                handlers.Where(handler => excludedHandlerTypes.None(remoteMessageHandlerType => remoteMessageHandlerType.IsInstanceOfType(handler.Instance)))
                    .ToList();

            return handlersToCall;
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

            private bool Equals(MessageHandlerReference other)
            {
                return GenericInterfaceImplemented == other.GenericInterfaceImplemented && Instance.Equals(other.Instance);
            }

            override public bool Equals(object other)
            {
                return Equals((MessageHandlerReference)other);
            }

            override public int GetHashCode()
            {
                return Instance.GetHashCode();
            }
        }

        private class MessageHandlerTypeDescriptor
        {
            public MessageHandlerTypeDescriptor(Type handlerConcreteType, Type handlerInterface)
            {
                HandlerConcreteType = handlerConcreteType;
                HandlerInterface = handlerInterface;
            }

            public Type HandlerConcreteType { get; private set; }

            public Type HandlerInterface { get; private set; }
        }


        private IEnumerable<MessageHandlerTypeDescriptor> GetHandlerTypes(object message)
        {
            var handlerTypes = _handlerInterfaces.SelectMany(handlerInterface => GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(message, handlerInterface));
            var excludedHandlerTypes = GetExcludedHandlerTypes(message);

            handlerTypes = handlerTypes.Where(handler => excludedHandlerTypes.None(remoteHandlerType => handler.HandlerConcreteType.Implements(remoteHandlerType))).ToList();

            return handlerTypes;
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

        private IEnumerable<MessageHandlerTypeDescriptor> GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(object message, Type handlerInterfaceGenericTypeDefinition)
        {
            var messageTypes = GetCanBeHandledMessageTypes(message);

            return messageTypes
                .Select(messageType => handlerInterfaceGenericTypeDefinition.MakeGenericType(messageType))
                .SelectMany(serviceInterface => _container.Kernel.GetAssignableHandlers(serviceInterface)
                    .Select(component => new MessageHandlerTypeDescriptor(
                        handlerConcreteType: component.ComponentModel.Implementation,
                        handlerInterface: serviceInterface)))
                .ToArray();
        }
    }
}
