using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;
using Castle.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;
using NServiceBus;

namespace Composable.ServiceBus
{
    internal class MyMessageHandlerResolver
    {        
        private readonly IWindsorContainer _container;
        public MyMessageHandlerResolver(IWindsorContainer container)
        {
            _container = container;
        }

        public bool HasHandlerFor(object message)
        {
            return GetHandlerTypes(message).Any();
        }

        public IEnumerable<MessageHandlerReference> GetHandlers(object message)
        {
            var handlers = GetHandlerTypes(message)
                .SelectMany(
                    handlerType => _container
                                        .ResolveAll(handlerType.ImplementedInterfaceType)
                                        .Cast<object>()
                                        .Select(handler => new MessageHandlerReference(
                                            handlerInterfaceType: handlerType.HandlerInterfaceType,
                                            instance: handler))
                )
                .Distinct()//Remove duplicates for classes that implement more than one interface. 
                .ToList();

            var remoteMessageHandlerTypes = RemoteMessageHandlerTypes(message);
            var handlersToCall = handlers.Where(handler => remoteMessageHandlerTypes.None(remoteMessageHandlerType => remoteMessageHandlerType.IsInstanceOfType(handler.Instance)))
                .ToList();

            return handlersToCall;
        }

        internal class MessageHandlerReference
        {
            public MessageHandlerReference(Type handlerInterfaceType, object instance)
            {
                HandlerInterfaceType = handlerInterfaceType;
                Instance = instance;
            }

            public Type HandlerInterfaceType { get; private set; }
            public object Instance { get; private set; }

            private bool Equals(MessageHandlerReference other)
            {
                return HandlerInterfaceType == other.HandlerInterfaceType && Instance.Equals(other.Instance);
            }

            public override bool Equals(object other)
            {
                if(ReferenceEquals(null, other))
                {
                    return false;
                }
                if(ReferenceEquals(this, other))
                {
                    return true;
                }
                if(other.GetType() != this.GetType())
                {
                    return false;
                }
                return Equals((MessageHandlerReference)other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (HandlerInterfaceType.GetHashCode()*397) ^ Instance.GetHashCode();
                }
            }
        }

        private class MessageHandlerTypeReference
        {
            public MessageHandlerTypeReference(WindsorHandlerReference handler, Type handlerInterfaceType)
            {
                HandlerInterfaceType = handlerInterfaceType;
                ImplementedInterfaceType = handler.ServiceType;
                Handler = handler.Handler;
            }

            public IHandler Handler { get; private set; }
            public Type HandlerInterfaceType { get; private set; }
            public Type ImplementedInterfaceType { get; private set; }
        }
        

        private List<MessageHandlerTypeReference> GetHandlerTypes(object message)
        {
            var inMemoryHandlers = GetHandlerTypesForInterfaceType(message, typeof(IHandleInProcessMessages<>))
                .Select(handler => new MessageHandlerTypeReference(handler, typeof(IHandleInProcessMessages<>)))
                .ToList();
            var standardHandlers = GetHandlerTypesForInterfaceType(message, typeof(IHandleMessages<>))
                .Select(handler => new MessageHandlerTypeReference(handler, typeof(IHandleMessages<>)))
                .ToList();
            var allHandlers = inMemoryHandlers.Concat(standardHandlers).ToArray();

            var remoteMessageHandlerTypes = RemoteMessageHandlerTypes(message);

            var handlersToCall = allHandlers
                .Where(handler => remoteMessageHandlerTypes.None( remoteHandlerType => handler.Handler.ComponentModel.Implementation.Implements(remoteHandlerType)))
                .ToList();

            return handlersToCall;
        }

        private static List<Type> RemoteMessageHandlerTypes(object message)
        {
            return message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(typeInheritedByMessageInstance => typeInheritedByMessageInstance.Implements(typeof(IMessage)))
                .Select(messageTypeInheritedByMessageInstance => typeof(IHandleRemoteMessages<>).MakeGenericType(messageTypeInheritedByMessageInstance))
                .ToList();
        }

        private class WindsorHandlerReference
        {
            public IHandler Handler { get; private set; }
            public Type ServiceType { get; private set; }
            public WindsorHandlerReference(IHandler handler, Type serviceType)
            {
                Handler = handler;
                ServiceType = serviceType;
            }
        }


        private WindsorHandlerReference[] GetHandlerTypesForInterfaceType(object message, Type handlerInterfaceType)
        {
            return message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(typeImplementedByMessage => typeImplementedByMessage.Implements(typeof(IMessage)))
                .Select(typeImplementedByMessageThatImplementsIMessage => handlerInterfaceType.MakeGenericType(typeImplementedByMessageThatImplementsIMessage))
                .SelectMany(implementedMessageHandlerInterface => _container.Kernel.GetHandlers(implementedMessageHandlerInterface)
                    .Select(component => new WindsorHandlerReference(component, implementedMessageHandlerInterface)))
                .ToArray();
        }
    }
}
