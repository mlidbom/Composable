using System;
using Composable.Persistence.EventStore;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.IntegerId
{
    static partial class RootEvent
    {
        public interface IRoot : IAggregateEvent {}

        interface Created : IRoot, IAggregateCreatedEvent, PropertyUpdated.Name {}

        public static class PropertyUpdated
        {
            public interface Name : RootEvent.IRoot
            {
                string Name { get; }
            }
        }

        internal static class Implementation
        {
            public abstract class Root : AggregateEvent, IRoot
            {
                protected Root() { }
                protected Root(Guid aggregateRootId) : base(aggregateRootId) { }
            }

            public class Created : Root, RootEvent.Created
            {
                public Created(Guid id, string name) : base(id) => Name = name;
                public string Name { get; }
            }
        }
    }
}
