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
            public interface IRoot : RootEvent.IRoot
            {
                Guid EntityId { get; }
            }

            public interface Created : IRoot, PropertyUpdated.Name {}

            public interface Renamed : IRoot, PropertyUpdated.Name {}

            public interface Removed : IRoot {}

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
                    public Guid EntityId { get; protected set; }

                    public class IdGetterSetter : IGetSetAggregateRootEntityEventEntityId<Root, IRoot>
                    {
                        public void SetEntityId(Root @event, Guid id) => @event.EntityId = id;
                        public Guid GetId(IRoot @event) => @event.EntityId;
                    }
                }

                public class Created : Root, Entity.Created
                {
                    public Created(Guid entityId, string name)
                    {
                        EntityId = entityId;
                        Name = name;
                    }
                    public string Name { get; }
                }

                public class Renamed : Root, Entity.Renamed
                {
                    public Renamed(string name) { Name = name; }
                    public string Name { get; }
                }

                public class Removed : Root, Entity.Removed {}
            }
        }
    }
}
