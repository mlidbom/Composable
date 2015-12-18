using System;
using Composable.CQRS.EventSourcing;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot
{
    public static partial class RootEvent
    {        
        public static class Entity
        {
            public interface IRoot : IAggregateRootComponentEvent, RootEvent.IRoot {}

            public interface Created : IRoot, IAggregateRootEntityCreatedEvent, PropertyUpdated.Name {}

            public interface Renamed : IRoot, PropertyUpdated.Name {}

            public static class PropertyUpdated
            {
                public interface Name : IRoot
                {
                    string Name { get; }
                }
            }

            public static class Implementation
            {
                public abstract class Root : RootEvent.Implementation.Root, Entity.IRoot
                {
                    protected Root(Guid entityId) { EntityId = entityId; }
                    public Guid EntityId { get; }
                }

                public class Created : Root, Entity.Created
                {
                    public Created(Guid entityId, string name) : base(entityId) { Name = name; }
                    public string Name { get; }
                }

                public class Renamed : Root, Entity.Renamed
                {
                    public Renamed(string name, Guid l1Id) : base(l1Id) { Name = name; }
                    public string Name { get; }
                }
            }
        }
    }
}
