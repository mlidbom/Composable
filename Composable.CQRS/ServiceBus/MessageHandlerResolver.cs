using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;
using NServiceBus;

namespace Composable.ServiceBus
{
    public partial class SynchronousBus
    {
        private class MyMessageHandlerResolver
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
                            .ResolveAll(handlerType.ServiceInterface)
                            .Cast<object>()
                            .Select(handler => new MessageHandlerReference(
                                genericInterfaceImplemented: handlerType.GenericInterface,
                                instance: handler))
                    )
                    .Distinct() //Remove duplicates for classes that implement more than one interface. 
                    .ToList();

                var remoteMessageHandlerTypes = RemoteMessageHandlerTypes(message);
                var handlersToCall =
                    handlers.Where(handler => remoteMessageHandlerTypes.None(remoteMessageHandlerType => remoteMessageHandlerType.IsInstanceOfType(handler.Instance)))
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

                private Type GenericInterfaceImplemented { get; set; }
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

                public void InvokeHandlers(object message)
                {
                    MessageHandlerInvoker.InvokeHandlerMethods(
                        messageHandler: Instance,
                        message: message,
                        genericInterfaceImplemented: GenericInterfaceImplemented);
                }
            }

            private class MessageHandlerTypeReference
            {
                public MessageHandlerTypeReference(Type genericInterface, Type implementingClass, Type serviceInterface)
                {
                    GenericInterface = genericInterface;
                    ImplementingClass = implementingClass;
                    ServiceInterface = serviceInterface;
                }

                public Type ImplementingClass { get; private set; }
                public Type GenericInterface { get; private set; }
                public Type ServiceInterface { get; private set; }
            }


            private List<MessageHandlerTypeReference> GetHandlerTypes(object message)
            {
                var inMemoryHandlerTypes = GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(message, typeof(IHandleInProcessMessages<>));
                var standardHandlerTypes = GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(message, typeof(IHandleMessages<>));

                var allHandlerTypes = inMemoryHandlerTypes.Concat(standardHandlerTypes).ToArray();

                var remoteMessageHandlerTypes = RemoteMessageHandlerTypes(message);

                var handlersToCall = allHandlerTypes
                    .Where(handler => remoteMessageHandlerTypes.None(remoteHandlerType => handler.ImplementingClass.Implements(remoteHandlerType)))
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


            private MessageHandlerTypeReference[] GetRegisteredHandlerTypesForMessageAndGenericInterfaceType(object message, Type genericInterface)
            {
                return message.GetType().GetAllTypesInheritedOrImplemented()
                    .Where(typeImplementedByMessage => typeImplementedByMessage.Implements(typeof(IMessage)))
                    .Select(typeImplementedByMessageThatImplementsIMessage => genericInterface.MakeGenericType(typeImplementedByMessageThatImplementsIMessage))
                    .SelectMany(serviceInterface => _container.Kernel.GetAssignableHandlers(serviceInterface)
                        .Select(component => new MessageHandlerTypeReference(
                            genericInterface: genericInterface,
                            implementingClass: component.ComponentModel.Implementation,
                            serviceInterface: serviceInterface)))
                    .ToArray();
            }
        }
    }
}