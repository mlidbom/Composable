using System;
using Composable.Messaging;
using Composable.Persistence.EventStore;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        [TypeId("61466CDB-38A8-450F-96CC-420F7C76BC99")]public interface IRoot : IAggregateRootEvent {}

        [TypeId("8FD0CA5E-C85F-4A98-AAEE-7F778D297488")]interface Created : IRoot, IAggregateRootCreatedEvent, PropertyUpdated.Name {}

        public static class PropertyUpdated
        {
            [TypeId("991BAC11-47B0-47FD-B0D2-DE41CE2F74E8")]public interface Name : RootEvent.IRoot
            {
                string Name { get; }
            }
        }

        internal static class Implementation
        {
            public abstract class Root : AggregateRootEvent, IRoot
            {
                protected Root() { }
                protected Root(Guid aggregateRootId) : base(aggregateRootId) { }
            }

            [TypeId("5B92E574-A4B5-461F-8065-5C2A1FE6F2F3")]public class Created : Root, RootEvent.Created
            {
                public Created(Guid id, string name) : base(id) => Name = name;
                public string Name { get; }
            }
        }
    }
}
