using Composable.Persistence.EventStore.AggregateRoots;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.IntegerId
{
    static partial class RootEvent
    {
        public static partial class Entity
        {
            [TypeId("5125A987-16E6-4FA1-BEF4-A63C559CE9AB")]public interface IRoot : RootEvent.IRoot
            {
                int EntityId { get; }
            }

            [TypeId("8BAA289D-6DA5-4FCA-A192-F9370E2D3DF6")]internal interface Created : IRoot, PropertyUpdated.Name {}

            [TypeId("AEC8F8BC-6D1C-4654-B300-DA737EAB8AC6")]interface Renamed : IRoot, PropertyUpdated.Name {}

            [TypeId("D388E2B9-3E5D-456B-B11F-89809EDCE481")]internal interface Removed : IRoot {}

            internal static class PropertyUpdated
            {
                [TypeId("DC41CA2E-5F7E-43B6-8BA2-195AF4F329A0")]public interface Name : IRoot
                {
                    string Name { get; }
                }
            }

            internal static class Implementation
            {
                public abstract class Root : RootEvent.Implementation.Root, Entity.IRoot
                {
                    public int EntityId { get; protected set; }

                    public class IdGetterSetter : IGetSetAggregateRootEntityEventEntityId<int, Root, IRoot>
                    {
                        public void SetEntityId(Root @event, int id) => @event.EntityId = id;
                        public int GetId(IRoot @event) => @event.EntityId;
                    }
                }

                [TypeId("3D78BF43-94EE-4C6C-9A5C-191B495A2354")]public class Created : Root, Entity.Created
                {
                    public Created(int entityId, string name)
                    {
                        EntityId = entityId;
                        Name = name;
                    }
                    public string Name { get; }
                }

                [TypeId("57FC56A5-86BE-4837-9206-5EA0ED2A4A90")]public class Renamed : Root, Entity.Renamed
                {
                    public Renamed(string name) => Name = name;
                    public string Name { get; }
                }

                [TypeId("55A09E0F-5D38-4EE7-9BA4-1DCE8609181B")]public class Removed : Root, Entity.Removed {}
            }
        }
    }
}
