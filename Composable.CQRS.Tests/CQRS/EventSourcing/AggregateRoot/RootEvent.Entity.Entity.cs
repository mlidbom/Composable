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
                public interface IRoot : RootEvent.Entity.IRoot { }

                public interface Created : IRoot, PropertyUpdated.Name { }

                public interface Renamed : IRoot, PropertyUpdated.Name { }

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
                        protected Root(Guid entityId):base(entityId) { }
                    }

                    public class Created : Root, NestedEntity.Created
                    {
                        public Created(Guid innerEntityId, Guid outerEntityId, string name) : base(innerEntityId) { Name = name; }
                        public string Name { get; }
                    }

                    public class Renamed : Root, NestedEntity.Renamed
                    {
                        public Renamed(string name, Guid l1Id) : base(l1Id) { Name = name; }
                        public string Name { get; }
                    }

                    public class IdGetterSetter : Root, IGetSetAggregateRootEntityEventEntityId<Root, IRoot>
                    {
                        public void SetEntityId(Root @event, Guid id) => @event.EntityId = id;
                        public Guid GetId(IRoot @event) => @event.EntityId;

                        public IdGetterSetter() : base(Guid.NewGuid()) {}
                    }
                }
            }
        }
    }
}
