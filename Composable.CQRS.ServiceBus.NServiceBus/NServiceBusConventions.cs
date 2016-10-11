using System;
using Composable.System.Reflection;
using NServiceBus;
using IMessage = Composable.ServiceBus.IMessage;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public static class NServiceBusConventions
    {
        public static bool IsMessageType(this Type type)
        {
            return type.Implements<global::NServiceBus.IMessage>()
                   || type.Implements<Composable.ServiceBus.IMessage>()
                   || type.Name.EndsWith("Message")
                   || (type.Namespace != null && type.Namespace.EndsWith("Messages"));
        }

        public static bool IsCommandType(this Type type)
        {
            return type.Implements<global::NServiceBus.ICommand>()
                   || type.Implements<Composable.CQRS.Command.ICommand>()
                   || type.Name.EndsWith("Command")
                   || (type.Namespace != null && type.Namespace.EndsWith("Commands"));
        }

        public static bool IsEventType(this Type type)
        {
            return type.Implements<global::NServiceBus.IEvent>()
                   || type.Implements<Composable.DomainEvents.IDomainEvent>()
                   || type.Name.EndsWith("Event")
                   || (type.Namespace != null && type.Namespace.EndsWith("Events"));
        }

        public static Configure SetupConventions(Configure config)
        {
            return config.DefiningEventsAs(IsEventType)
                         .DefiningCommandsAs(IsCommandType)
                         .DefiningCommandsAs(IsMessageType);
        }
    }
}