using System;
using Composable.CQRS.EventSourcing;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot
{
    public static partial class RootEvent
    {       
        public static partial class Component
        {
            public interface IRoot : IAggregateRootEntityEvent, RootEvent.IRoot {}

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
                public abstract class Root : RootEvent.Implementation.Root, Component.IRoot
                {
                    protected Root() {}
                    protected Root(Guid entityId) { EntityId = entityId; }
                    public Guid EntityId { get; }
                }

                public class Renamed : Root, Component.Renamed
                {
                    public Renamed(string name) { Name = name; }
                    public string Name { get; }
                }
            }
        }
    }
}
