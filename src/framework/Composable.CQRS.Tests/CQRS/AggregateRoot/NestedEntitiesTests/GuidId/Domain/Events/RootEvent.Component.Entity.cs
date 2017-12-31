using System;
using Composable.Persistence.EventStore.AggregateRoots;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public static partial class Component
        {
            public static class Entity
            {
                [TypeId("C886F0E5-99F6-46EC-A968-3DA1DB4D0E2D")]public interface IRoot : RootEvent.Component.IRoot
                {
                    Guid EntityId { get; }
                }

                [TypeId("052FBB2C-F65E-44E9-9C60-BAF6B8AB9CFD")]public interface Created : IRoot, PropertyUpdated.Name {}

                [TypeId("53BC2503-3296-45EC-A299-68D88D7000B4")]interface Renamed : IRoot, PropertyUpdated.Name {}

                [TypeId("AF0D7496-518B-4EA2-A914-700164D9C6A8")]public interface Removed : IRoot {}

                public static class PropertyUpdated
                {
                    [TypeId("11332A4B-4E1B-49C2-9B64-EDFC0787BA3A")]public interface Name : IRoot
                    {
                        string Name { get; }
                    }
                }

                internal static class Implementation
                {
                    public abstract class Root : RootEvent.Component.Implementation.Root, Entity.IRoot
                    {
                        public Guid EntityId { get; protected set; }

                        public class IdGetterSetter : IGetSetAggregateRootEntityEventEntityId<Guid, Root, IRoot>
                        {
                            public void SetEntityId(Root @event, Guid id) => @event.EntityId = id;
                            public Guid GetId(IRoot @event) => @event.EntityId;
                        }
                    }

                    [TypeId("CA0CB176-C760-4A78-818B-60F577193B4E")]public class Created : Root, Entity.Created
                    {
                        public Created(Guid entityId, string name)
                        {
                            EntityId = entityId;
                            Name = name;
                        }
                        public string Name { get; }
                    }

                    [TypeId("3353D035-24D1-4787-AE76-4B8C1027BDF0")]public class Renamed : Root, Entity.Renamed
                    {
                        public Renamed(string name) => Name = name;
                        public string Name { get; }
                    }

                    [TypeId("8220C48C-4BE0-4BC4-8330-A9DFCA231C1E")]public class Removed : Root, Entity.Removed {}
                }
            }
        }
    }
}
