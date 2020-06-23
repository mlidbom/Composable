using Composable.Persistence.EventStore.Aggregates;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.Aggregates.NestedEntitiesTests.IntegerId
{
    static partial class RootEvent
    {
        public static partial class Entity
        {
            public static class NestedEntity
            {
                public interface IRoot : RootEvent.Entity.IRoot
                {
                    int NestedEntityId { get; }
                }

                public interface Created : IRoot, PropertyUpdated.Name {}

                interface Renamed : IRoot, PropertyUpdated.Name {}
                public interface Removed : IRoot { }

                public static class PropertyUpdated
                {
                    public interface Name : IRoot
                    {
                        string Name { get; }
                    }
                }

                internal static class Implementation
                {
                    public abstract class Root : RootEvent.Entity.Implementation.Root, NestedEntity.IRoot
                    {
                        public int NestedEntityId { get; protected set; }

                        [UsedImplicitly] public new class IdGetterSetter : Root, IGetSetAggregateEntityEventEntityId<int, Root, IRoot>
                        {
                            public void SetEntityId(Root @event, int id) => @event.NestedEntityId = id;
                            public int GetId(IRoot @event) => @event.NestedEntityId;
                        }
                    }

                    public class Created : Root, NestedEntity.Created
                    {
                        public Created(int nestedEntityId, string name)
                        {
                            NestedEntityId = nestedEntityId;
                            Name = name;
                        }
                        public string Name { get; }
                    }

                    public class Renamed : Root, NestedEntity.Renamed
                    {
                        public Renamed(string name) => Name = name;
                        public string Name { get; }
                    }

                    public class Removed : Root, NestedEntity.Removed
                    {
                    }
                }
            }
        }
    }
}
