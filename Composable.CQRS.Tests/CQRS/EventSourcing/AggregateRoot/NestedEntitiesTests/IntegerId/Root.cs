using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.IntegerId
{
    class Root : AggregateRoot<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        static int _instances;
        public string Name { get; private set; }
        readonly Entity.CollectionManager _entities;
#pragma warning disable 108,114
        public Component Component { get; private set; }
#pragma warning restore 108,114

        public Root(string name) : base(new DateTimeNowTimeSource())
        {
            Component = new Component(this);
            _entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

            RaiseEvent(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
        }

        public IReadOnlyEntityCollection<Entity, int> Entities => _entities.Entities;
        public Entity AddEntity(string name) => _entities.Add(new RootEvent.Entity.Implementation.Created(++_instances, name));
    }

    class Component : Root.Component<Component, RootEvent.Component.Implementation.Root, RootEvent.Component.IRoot>
    {
        static int _instances;
        public string Name { get; private set; }
        public Component(Root root) : base(root)
        {
            _entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadOnlyEntityCollection<Entity, int> Entities => _entities.Entities;
        readonly Entity.CollectionManager _entities;

        public void Rename(string name) { RaiseEvent(new RootEvent.Component.Implementation.Renamed(name)); }
        public Entity AddEntity(string name) => _entities.Add(new RootEvent.Component.Entity.Implementation.Created(++_instances, name));

        public class Entity : NestedEntity<Entity,
                                  int,
                                  RootEvent.Component.Entity.Implementation.Root,
                                  RootEvent.Component.Entity.IRoot,
                                  RootEvent.Component.Entity.Created,
                                  RootEvent.Component.Entity.Removed,
                                  RootEvent.Component.Entity.Implementation.Root.IdGetterSetter>
        {
            public string Name { get; private set; }
            public Entity(Component parent) : base(parent)
            {
                RegisterEventAppliers()
                    .For<RootEvent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
            }

            public void Rename(string name) { RaiseEvent(new RootEvent.Component.Entity.Implementation.Renamed(name)); }
            public void Remove() => RaiseEvent(new RootEvent.Component.Entity.Implementation.Removed());
        }
    }

    class Entity : Root.Entity<Entity,
                              int,
                              RootEvent.Entity.Implementation.Root,
                              RootEvent.Entity.IRoot,
                              RootEvent.Entity.Created,
                              RootEvent.Entity.Removed,
                              RootEvent.Entity.Implementation.Root.IdGetterSetter>
    {
        static int _instances;
        public string Name { get; private set; }
        public Entity(Root root) : base(root)
        {
            _entities = NestedEntity.CreateSelfManagingCollection(this);
            RegisterEventAppliers()
                .For<RootEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadOnlyEntityCollection<NestedEntity, int> Entities => _entities.Entities;
        readonly NestedEntity.CollectionManager _entities;

        public void Rename(string name) { RaiseEvent(new RootEvent.Entity.Implementation.Renamed(name)); }
        public void Remove() => RaiseEvent(new RootEvent.Entity.Implementation.Removed());

        public class NestedEntity : NestedEntity<NestedEntity,
                                        int,
                                        RootEvent.Entity.NestedEntity.Implementation.Root,
                                        RootEvent.Entity.NestedEntity.IRoot,
                                        RootEvent.Entity.NestedEntity.Created,
                                        RootEvent.Entity.NestedEntity.Removed,
                                        RootEvent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
        {
            public string Name { get; private set; }
            public NestedEntity(Entity entity) : base(entity)
            {
                RegisterEventAppliers()
                    .For<RootEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
            }

            public void Rename(string name) => RaiseEvent(new RootEvent.Entity.NestedEntity.Implementation.Renamed(name: name));
            public void Remove() => RaiseEvent(new RootEvent.Entity.NestedEntity.Implementation.Removed());

        }

        public NestedEntity AddEntity(string name)
            => _entities.Add(new RootEvent.Entity.NestedEntity.Implementation.Created(nestedEntityId: ++_instances, name: name));
    }
}
