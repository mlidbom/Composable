using System;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable UnusedMember.Local
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable MemberCanBeInternal

namespace Composable.Tests.CQRS.Aggregates
{
    public class PublicSettersAndFieldsAreDisallowedTests
    {
        public static class RootEvent
        {
            public interface IRoot : IAggregateEvent { string Public1 { get; set; } }

            public class Root : AggregateEvent, IRoot { public string Public1 { get; set; } }

            [AllowPublicSetters]
            public class Ignored : Root { public string IgnoredMember { get; set; } }

            public static class Component
            {
                public interface IRoot : RootEvent.IRoot { string Public2 { get; set; }  }
                internal class Root : RootEvent.Root, IRoot { public string Public2 { get; set; } }

                public static class NestedComponent
                {
                    public interface IRoot : Component.IRoot{ string           Public3 { get; set; }  }
                    internal class Root : Component.Root, IRoot { public string Public3 { get; set; } }
                }
            }

            public static class Entity
            {
                public interface IRoot : RootEvent.IRoot{ string            Public4 { get; set; }  }
                internal class Root : RootEvent.Root, IRoot { public string Public4 { get; set; }
                    [UsedImplicitly] public class GetterSetter : IGetSetAggregateEntityEventEntityId<Guid, Root, IRoot>
                    {
                        public Guid GetId(IRoot @event) { throw new NotImplementedException(); }
                        public void SetEntityId(Root @event, Guid id) { throw new NotImplementedException(); }
                    }
                }

                public static class Component
                {
                    public interface IRoot : Entity.IRoot { string           Public2 { get; set; }  }
                    internal class Root : Entity.Root, IRoot { public string Public2 { get; set; } }

                    public static class NestedComponent
                    {
                        public interface IRoot : Component.IRoot{ string            Public3 { get; set; }  }
                        internal class Root : Component.Root, IRoot { public string Public3 { get; set; } }
                    }
                }
            }

        }

        class Root : Aggregate<Root, RootEvent.Root, RootEvent.IRoot>
        {
            public Root(IUtcTimeTimeSource timeSource) : base(timeSource) {}

            public class AggComponent : Root.Component<AggComponent, RootEvent.Component.Root, RootEvent.Component.IRoot>
            {
                public AggComponent(Root aggregate) : base(aggregate) {}

                public string Public { get; set; }

                public class NestedAggComponent : AggComponent.NestedComponent<NestedAggComponent, RootEvent.Component.NestedComponent.Root, RootEvent.Component.NestedComponent.IRoot>
                {
                    public NestedAggComponent(AggComponent parent) : base(parent) {}

                    public string Public { get; set; }
                }
            }

            public class AggEntity : Root.Entity<AggEntity, Guid, RootEvent.Entity.Root, RootEvent.Entity.IRoot,RootEvent.Entity.IRoot, RootEvent.Entity.Root.GetterSetter>
            {
                public AggEntity(Root aggregate) : base(aggregate) {}
                public string Public { get; set; }

                public class EntNestedComp : AggEntity.NestedComponent<EntNestedComp, RootEvent.Entity.Component.Root, RootEvent.Entity.Component.IRoot>
                {
                    public EntNestedComp(AggEntity parent) : base(parent) {}
                    public string Public2 { get; set; }
                }
            }
        }


        [Test]public void Trying_to_create_instance_of_aggregate_throws_and_lists_all_broken_types_in_exception_except_ignored()
        {
            AssertThrows.Exception<Exception>(() => new Root(null)).InnerException
                        .Message
                        .Should().Contain(typeof(Root).FullName)
                        .And.Contain(typeof(RootEvent.IRoot).FullName)
                        .And.Contain(typeof(RootEvent.Root).FullName)
                        .And.NotContain(typeof(RootEvent.Ignored).FullName);
        }

        [Test] public void Trying_to_create_instance_of_component_throws_and_lists_all_broken_types_in_exception()
        {
            AssertThrows.Exception<Exception>(() => new Root.AggComponent(null)).InnerException
                        .Message
                        .Should().Contain(typeof(Root.AggComponent).FullName).And
                        .Contain(typeof(RootEvent.Component.IRoot).FullName)
                        .And.Contain(typeof(RootEvent.Component.Root).FullName);
        }


        [Test] public void Trying_to_create_instance_of_nested_nested_component_throws_and_lists_all_broken_types_in_exception()
        {
            AssertThrows.Exception<Exception>(() => new Root.AggComponent.NestedAggComponent(null)).InnerException
                        .Message
               .Should().Contain(typeof(Root.AggComponent.NestedAggComponent).FullName).And
               .Contain(typeof(RootEvent.Component.NestedComponent.IRoot).FullName)
               .And.Contain(typeof(RootEvent.Component.NestedComponent.Root).FullName);
        }

        [Test] public void Trying_to_create_instance_of_entity_throws_and_lists_all_broken_types_in_exception()
        {
            AssertThrows.Exception<Exception>(() => new Root.AggEntity(null)).InnerException
                        .Message
                        .Should().Contain(typeof(Root.AggEntity).FullName).And
                        .Contain(typeof(RootEvent.Entity.IRoot).FullName)
                        .And.Contain(typeof(RootEvent.Entity.Root).FullName);
        }

        [Test] public void Trying_to_create_instance_of_entity_nested_component_throws_and_lists_all_broken_types_in_exception()
        {
            AssertThrows.Exception<Exception>(() => new Root.AggEntity.EntNestedComp(null)).InnerException
                        .Message
                        .Should().Contain(typeof(Root.AggEntity.EntNestedComp).FullName).And
                        .Contain(typeof(RootEvent.Entity.Component.IRoot).FullName)
                        .And.Contain(typeof(RootEvent.Entity.Component.Root).FullName);
        }
    }
}
