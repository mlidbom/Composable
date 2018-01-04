using System;
using Composable.Persistence.EventStore.AggregateRoots;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public static partial class Entity
        {
            [TypeId("C5788A29-0077-4950-88CF-6416772F3765")]public interface IRoot : RootEvent.IRoot
            {
                Guid EntityId { get; }
            }

            [TypeId("92ED77DD-005C-4594-8B5E-82F88693C2A9")]public interface Created : IRoot, PropertyUpdated.Name {}

            [TypeId("32BA3B3A-90E5-43C5-9D64-C35899FA7C1E")]interface Renamed : IRoot, PropertyUpdated.Name {}

            [TypeId("BD22CEF0-F8F4-49F5-93A7-BE59491B3D96")]public interface Removed : IRoot {}

            public static class PropertyUpdated
            {
                [TypeId("C944640D-29EC-4FFB-8F4B-8938C04D92D8")]public interface Name : IRoot
                {
                    string Name { get; }
                }
            }

            internal static class Implementation
            {
                public abstract class Root : RootEvent.Implementation.Root, Entity.IRoot
                {
                    public Guid EntityId { get; protected set; }

                    public class IdGetterSetter : IGetSetAggregateRootEntityEventEntityId<Guid, Root, IRoot>
                    {
                        public void SetEntityId(Root @event, Guid id) => @event.EntityId = id;
                        public Guid GetId(IRoot @event) => @event.EntityId;
                    }
                }

                [TypeId("4AE89D02-4F0D-40A4-A717-97961268F585")]public class Created : Root, Entity.Created
                {
                    public Created(Guid entityId, string name)
                    {
                        EntityId = entityId;
                        Name = name;
                    }
                    public string Name { get; }
                }

                [TypeId("D4E03F33-CE64-45E8-8FFB-3BB55C84C4D0")]public class Renamed : Root, Entity.Renamed
                {
                    public Renamed(string name) => Name = name;
                    public string Name { get; }
                }

                [TypeId("B5B28D26-809B-4EBC-9E06-8AA8C5945988")]public class Removed : Root, Entity.Removed {}
            }
        }
    }
}
