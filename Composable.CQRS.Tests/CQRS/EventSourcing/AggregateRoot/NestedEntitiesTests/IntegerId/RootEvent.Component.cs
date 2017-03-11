 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.IntegerId
{
    public static partial class RootEvent
    {
        public static partial class Component
        {
            public interface IRoot : RootEvent.IRoot {}

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
                public abstract class Root : RootEvent.Implementation.Root, Component.IRoot {}

                public class Renamed : Root, Component.Renamed
                {
                    public Renamed(string name) { Name = name; }
                    public string Name { get; }
                }
            }
        }
    }
}
