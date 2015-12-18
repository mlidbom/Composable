using System;
using Composable.CQRS.EventSourcing;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot
{
    public static class RootEvent
    {
        public interface IRoot : IAggregateRootEvent {}

        public interface Created : IRoot, IAggregateRootCreatedEvent, PropertyUpdated.Name {}

        public static class PropertyUpdated
        {
            public interface Name : RootEvent.IRoot
            {
                string Name { get; }
            }
        }

        public static class Implementation
        {
            public abstract class Root : AggregateRootEvent, IRoot
            {
                protected Root() { }
                protected Root(Guid aggregateRootId) : base(aggregateRootId) { }
            }

            public class Created : Root, RootEvent.Created
            {
                public Created(Guid id, string name) : base(id) { Name = name; }
                public string Name { get; }
            }
        }

        public static class L1Entity
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
                public abstract class Root : RootEvent.Implementation.Root, L1Entity.IRoot
                {
                    protected Root(Guid entityId) { EntityId = entityId; }
                    public Guid EntityId { get; }
                }

                public class Created : Root, L1Entity.Created
                {
                    public Created(Guid entityId, string name) : base(entityId) { Name = name; }
                    public string Name { get; }
                }

                public class Renamed : Root, L1Entity.Renamed
                {
                    public Renamed(string name, Guid l1Id) : base(l1Id) { Name = name; }
                    public string Name { get; }
                }
            }
        }

        public static class L1Component
        {
            public interface IRoot : IAggregateRootComponentEvent, RootEvent.IRoot {}

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
                public abstract class Root : RootEvent.Implementation.Root, L1Component.IRoot
                {
                    public Guid EntityId { get; }
                }

                public class Renamed : Root, L1Component.Renamed
                {
                    public Renamed(string name) { Name = name; }
                    public string Name { get; }
                }
            }

            public static class L2Entity
            {
                public interface IRoot : IAggregateRootComponentEvent, RootEvent.L1Component.IRoot { }

                public interface Created : IRoot, IAggregateRootEntityCreatedEvent, PropertyUpdated.Name { }

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
                    public abstract class Root : RootEvent.L1Component.Implementation.Root, L2Entity.IRoot
                    {
                        protected Root(Guid entityId) { EntityId = entityId; }
                        public Guid EntityId { get; }
                    }

                    public class Created : Root, L2Entity.Created
                    {
                        public Created(Guid entityId, string name) : base(entityId) { Name = name; }
                        public string Name { get; }
                    }

                    public class Renamed : Root, L2Entity.Renamed
                    {
                        public Renamed(string name, Guid l1Id) : base(l1Id) { Name = name; }
                        public string Name { get; }
                    }
                }
            }
        }
    }
}
