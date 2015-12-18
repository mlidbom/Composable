using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;

namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot
{
    public class Root : AggregateRoot<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        public string Name { get; private set; }
        public L1Entity.Collection L1Entities { get; }

        public Root(string name) : base(new DateTimeNowTimeSource())
        {
            L1Entities = L1Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

            RaiseEvent(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
        }

        public L1Entity AddL1(string name) { return L1Entities.Add(new RootEvent.L1Entity.Implementation.Created(Guid.NewGuid(), name)); }      
    }


    public class L1Entity : Root.NestedEntity<L1Entity, RootEvent.L1Entity.Implementation.Root, RootEvent.L1Entity.IRoot, RootEvent.L1Entity.Created>
    {
        public string Name { get; private set; }
        public L1Entity()
        {
            RegisterEventAppliers()
                .For<RootEvent.L1Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public void Rename(string name)
        {
            RaiseEvent(new RootEvent.L1Entity.Implementation.Renamed(name, Id));
        }
    }
}