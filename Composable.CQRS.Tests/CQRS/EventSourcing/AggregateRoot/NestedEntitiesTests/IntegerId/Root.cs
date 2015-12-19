using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using JetBrains.Annotations;

namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.IntegerId
{
    public class Root : AggregateRoot<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        private static int _instances;
        public string Name { get; private set; }
        private readonly Entity.CollectionManager _entities;
        public Component Component { get; private set; }

        public Root(string name) : base(new DateTimeNowTimeSource())
        {
            Component = new Component(this);
            _entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

            RaiseEvent(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
        }

        public IReadOnlyEntityCollection<Entity, int> Entities => _entities.Entities;
        public Entity AddEntity(string name) { return _entities.Add(new RootEvent.Entity.Implementation.Created(++_instances, name)); }
    }

    public class Component : Root.Component<Component, RootEvent.Component.Implementation.Root, RootEvent.Component.IRoot>
    {
        private static int _instances;
        public string Name { get; private set; }
        public Component(Root root) : base(root)
        {
            _entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadOnlyEntityCollection<Entity, int> Entities => _entities.Entities;
        private readonly Entity.CollectionManager _entities;

        public void Rename(string name) { RaiseEvent(new RootEvent.Component.Implementation.Renamed(name)); }
        public Entity AddEntity(string name) { return _entities.Add(new RootEvent.Component.Entity.Implementation.Created(++_instances, name)); }

        [UsedImplicitly]
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

    [UsedImplicitly]
    public class Entity : Root.Entity<Entity,
                              int,
                              RootEvent.Entity.Implementation.Root,
                              RootEvent.Entity.IRoot,
                              RootEvent.Entity.Created,
                              RootEvent.Entity.Removed,
                              RootEvent.Entity.Implementation.Root.IdGetterSetter>
    {
        private static int _instances;
        public string Name { get; private set; }
        public Root Root { get; }
        public Entity(Root root) : base(root)
        {
            Root = root;
            _entities = NestedEntity.CreateSelfManagingCollection(this);
            RegisterEventAppliers()
                .For<RootEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public IReadOnlyEntityCollection<NestedEntity, int> Entities => _entities.Entities;
        private readonly NestedEntity.CollectionManager _entities;

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
            public Entity Entity { get; }
            public NestedEntity(Entity entity) : base(entity)
            {
                Entity = entity;
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
