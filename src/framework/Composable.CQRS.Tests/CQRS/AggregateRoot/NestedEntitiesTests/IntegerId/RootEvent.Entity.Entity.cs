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
            public static class NestedEntity
            {
                [TypeId("48392B87-5A01-45E3-A88F-62769B4F066F")]public interface IRoot : RootEvent.Entity.IRoot
                {
                    int NestedEntityId { get; }
                }

                [TypeId("A8239270-5274-4089-A066-B0F7CBCBDB15")]public interface Created : IRoot, PropertyUpdated.Name {}

                [TypeId("3E97666E-820E-4E1D-B044-AA0CC94C6197")]interface Renamed : IRoot, PropertyUpdated.Name {}
                [TypeId("45753603-C7FA-4616-82DA-6655DDEA55A1")]public interface Removed : IRoot { }

                public static class PropertyUpdated
                {
                    [TypeId("C0C37707-4D7A-4C25-8354-809F5A28982C")]public interface Name : IRoot
                    {
                        string Name { get; }
                    }
                }

                internal static class Implementation
                {
                    public abstract class Root : RootEvent.Entity.Implementation.Root, NestedEntity.IRoot
                    {
                        public int NestedEntityId { get; protected set; }

                        public new class IdGetterSetter : Root, IGetSetAggregateRootEntityEventEntityId<int, Root, IRoot>
                        {
                            public void SetEntityId(Root @event, int id) => @event.NestedEntityId = id;
                            public int GetId(IRoot @event) => @event.NestedEntityId;
                        }
                    }

                    [TypeId("A4953E9F-730D-4753-B81F-E21649530084")]public class Created : Root, NestedEntity.Created
                    {
                        public Created(int nestedEntityId, string name)
                        {
                            NestedEntityId = nestedEntityId;
                            Name = name;
                        }
                        public string Name { get; }
                    }

                    [TypeId("F85E6326-3994-4272-85AD-E83E5E354EDE")]public class Renamed : Root, NestedEntity.Renamed
                    {
                        public Renamed(string name) => Name = name;
                        public string Name { get; }
                    }

                    [TypeId("F9485187-B5BE-40AF-BF8B-8D6C753D966E")]public class Removed : Root, NestedEntity.Removed
                    {
                    }
                }
            }
        }
    }
}
