 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    public static partial class RootEvent
    {
        public static partial class Component
        {
            public static partial class NestedComponent
            {
                public interface IRoot : Component.IRoot { }

                public interface Renamed : NestedComponent.IRoot, PropertyUpdated.Name { }

                public static class PropertyUpdated
                {
                    public interface Name : NestedComponent.IRoot
                    {
                        string Name { get; }
                    }
                }

                public static class Implementation
                {
                    public abstract class Root : Component.Implementation.Root, NestedComponent.IRoot { }
                }
            }
        }
    }
}
