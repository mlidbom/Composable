using System;
using Composable.Persistence.EventStore;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.IntegerId
{
    static partial class RootEvent
    {
        [TypeId("41E237B3-F2B2-4159-84C0-6EC04AA48CE6")]public interface IRoot : IAggregateRootEvent {}

        [TypeId("FEEEC8A1-7FBC-4E4F-A8B4-727483214346")]interface Created : IRoot, IAggregateRootCreatedEvent, PropertyUpdated.Name {}

        public static class PropertyUpdated
        {
            [TypeId("4E781101-6234-4ED7-AD9A-DCCA1C56A430")]public interface Name : RootEvent.IRoot
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

            [TypeId("85A0D3A8-9F80-4F25-B490-F7E27483D2CB")]public class Created : Root, RootEvent.Created
            {
                public Created(Guid id, string name) : base(id) => Name = name;
                public string Name { get; }
            }
        }
    }
}
