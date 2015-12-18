using System;
using Composable.CQRS.EventSourcing;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot
{
    public static partial class RootEvent
    {
        public static partial class Entity
        {
            public static class NestedEntity
            {
                public interface IRoot : RootEvent.Entity.IRoot
                {
                    Guid NestedEntityId { get; }
                }

                public interface Created : IRoot, PropertyUpdated.Name {}

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
                    public abstract class Root : RootEvent.Entity.Implementation.Root, NestedEntity.IRoot
                    {
                        protected Root(Guid entityId, Guid nestedEntityid) : base(entityId) { NestedEntityId = nestedEntityid; }
                        public Guid NestedEntityId { get; set; }
                    }

                    public class Created : Root, NestedEntity.Created
                    {
                        public Created(Guid nestedEntityId, Guid entityId, string name) : base(entityId: entityId, nestedEntityid: nestedEntityId)
                        {
                            Name = name;
                        }
                        public string Name { get; }
                    }

                    public class Renamed : Root, NestedEntity.Renamed
                    {
                        public Renamed(string name, Guid nestedEntityId, Guid entityId) : base(nestedEntityid: nestedEntityId, entityId: entityId) { Name = name; }
                        public string Name { get; }
                    }

                    public class IdGetterSetter : Root, IGetSetAggregateRootEntityEventEntityId<Root, IRoot>
                    {
                        public void SetEntityId(Root @event, Guid id) => @event.NestedEntityId = id;
                        public Guid GetId(IRoot @event) => @event.NestedEntityId;

                        public IdGetterSetter() : base(Guid.NewGuid(), Guid.NewGuid()) { }
                    }
                }
            }
        }
    }
}
