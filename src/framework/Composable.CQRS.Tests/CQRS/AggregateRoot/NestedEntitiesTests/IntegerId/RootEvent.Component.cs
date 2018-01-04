 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.IntegerId
{
    static partial class RootEvent
    {
        public static partial class Component
        {
            [TypeId("0B50E0B3-7C68-4366-B398-67B71CC8EC32")]public interface IRoot : RootEvent.IRoot {}

            [TypeId("DEFAA6BA-38B5-4F86-A996-43335BEFE6EE")]interface Renamed : IRoot, PropertyUpdated.Name {}

            public static class PropertyUpdated
            {
                [TypeId("7DB5A895-08BB-4DBD-9F25-7446D7C8CE7D")]public interface Name : IRoot
                {
                    string Name { get; }
                }
            }

            internal static class Implementation
            {
                public abstract class Root : RootEvent.Implementation.Root, Component.IRoot {}

                [TypeId("FD278BF6-3B49-44E5-BF96-26AEB58918B0")]public class Renamed : Root, Component.Renamed
                {
                    public Renamed(string name) => Name = name;
                    public string Name { get; }
                }
            }
        }
    }
}
