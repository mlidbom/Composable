using Composable.Persistence.EventStore.AggregateRoots;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.IntegerId
{
    static partial class RootEvent
    {
        public static partial class Component
        {
            public static class Entity
            {
                [TypeId("B2CC834B-3CB6-4B5A-8425-E45B7E3AAC51")]public interface IRoot : RootEvent.Component.IRoot
                {
                    int EntityId { get; }
                }

                [TypeId("4AE183D9-1495-460D-9C3F-EEB07D805DBD")]public interface Created : IRoot, PropertyUpdated.Name {}

                [TypeId("E3D22DD1-5813-465A-8BB9-FBBA032E08EB")]interface Renamed : IRoot, PropertyUpdated.Name {}

                [TypeId("6464DA8E-A3A9-4F51-8CA1-DC1FC230AE6C")]public interface Removed : IRoot {}

                public static class PropertyUpdated
                {
                    [TypeId("77A9D0F2-4FB5-4322-9079-68E7890646B8")]public interface Name : IRoot
                    {
                        string Name { get; }
                    }
                }

                internal static class Implementation
                {
                    public abstract class Root : RootEvent.Component.Implementation.Root, Entity.IRoot
                    {
                        public int EntityId { get; protected set; }

                        public class IdGetterSetter : IGetSetAggregateRootEntityEventEntityId<int, Root, IRoot>
                        {
                            public void SetEntityId(Root @event, int id) => @event.EntityId = id;
                            public int GetId(IRoot @event) => @event.EntityId;
                        }
                    }

                    [TypeId("0F3A3AEC-65EC-45FA-8454-A0DACB951B90")]public class Created : Root, Entity.Created
                    {
                        public Created(int entityId, string name)
                        {
                            EntityId = entityId;
                            Name = name;
                        }
                        public string Name { get; }
                    }

                    [TypeId("DEE72C6A-9883-498E-8838-0678B526FC78")]public class Renamed : Root, Entity.Renamed
                    {
                        public Renamed(string name) => Name = name;
                        public string Name { get; }
                    }

                    [TypeId("58F0D892-1883-4793-BE0E-1AE5BE1A1214")]public class Removed : Root, Entity.Removed {}
                }
            }
        }
    }
}
