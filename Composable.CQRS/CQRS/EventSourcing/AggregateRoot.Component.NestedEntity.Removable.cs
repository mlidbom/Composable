namespace Composable.CQRS.EventSourcing
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEntityRemovedEventInterface,
                                               TEventEntityIdSetterGetter> :
                                                   NestedEntity
                                                       <TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface,
                                                       TEventEntityIdSetterGetter>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityRemovedEventInterface : TEntityBaseEventInterface
                where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
                where TEntity :
                    NestedEntity<TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdSetterGetter>
            {
                protected NestedEntity(TComponent parent) : base(parent) { }

                public new static Collection CreateSelfManagingCollection(TComponent aggregate) => new Collection(aggregate);

                public new class Collection : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
                                                  .NestedEntity
                                                  <TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface,
                                                  TEventEntityIdSetterGetter>.Collection
                {
                    public Collection(TComponent parent) : base(parent: parent, appliersRegistrar: parent.RegisterEventAppliers())
                    {
                        parent.RegisterEventAppliers()
                                 .For<TEntityRemovedEventInterface>(
                                     e =>
                                     {
                                         var id = IdGetterSetter.GetId(e);
                                         _entities.Remove(id);
                                     });
                    }
                }
            }            
        }
    }
}
