using System;
using Composable.CQRS.EventSourcing;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public interface IRoot : IAggregateRootEvent {}

        interface Created : IRoot, IAggregateRootCreatedEvent, PropertyUpdated.Name {}

        public static class PropertyUpdated
        {
            public interface Name : RootEvent.IRoot
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

            public class Created : Root, RootEvent.Created
            {
                public Created(Guid id, string name) : base(id) { Name = name; }
                public string Name { get; }
            }
        }
    }
}
