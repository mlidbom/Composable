// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using System.Collections.Generic;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Composable.Messaging
{
    public static partial class MessageTypes
    {
        internal static class Internal
        {
            internal interface IMessage {}

            internal class EndpointInformationQuery : Internal.IMessage, IRemotableQuery<EndpointInformation> {}

            internal class EndpointInformation
            {
                [UsedImplicitly][JsonConstructor]EndpointInformation(string name, EndpointId id, HashSet<TypeId> handledMessageTypes)
                {
                    Name = name;
                    Id = id;
                    HandledMessageTypes = handledMessageTypes;
                }

                public EndpointInformation(IEnumerable<TypeId> handledRemoteMessageTypeIds, EndpointConfiguration configuration)
                {
                    Id = configuration.Id;
                    Name = configuration.Name;
                    HandledMessageTypes = new HashSet<TypeId>(handledRemoteMessageTypeIds);
                }

                public string Name { get; private set; }
                public EndpointId Id { get; private set; }
                public HashSet<TypeId> HandledMessageTypes { get; private set; }
            }

            public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (EndpointInformationQuery query, TypeMapper typemapper, IMessageHandlerRegistry registry, EndpointConfiguration configuration) =>
                    new EndpointInformation(registry.HandledRemoteMessageTypeIds(), configuration));
        }
    }
}
