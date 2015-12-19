using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {            
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
                where TEntityBaseEventInterface : TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntity :
                    NestedEntity<TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdSetterGetter>
                where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
            {
                private readonly TComponent _component;
                protected static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

                public TEntityId Id { get; private set; }

                protected NestedEntity(TComponent component) : base(aggregateRoot: component.AggregateRoot, registerEventAppliers: false)
                {
                    _component = component;
                    RegisterEventAppliers()
                        .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e))
                        .IgnoreUnhandled<TEntityBaseEventInterface>();
                }

                protected override void RaiseEvent(TEntityBaseEventClass @event)
                {
                    var id = IdGetterSetter.GetId(@event);
                    if (Equals(id, default(TEntityId)))
                    {
                        IdGetterSetter.SetEntityId(@event, Id);
                    }
                    else if (!Equals(id, Id))
                    {
                        throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
                    }
                    _component.RaiseEvent(@event);
                }

                public static Collection CreateSelfManagingCollection(TComponent aggregate) => new Collection(aggregate);

                public class Collection : IEntityCollectionManager<TEntity, TEntityId, TEntityBaseEventClass, TEntityCreatedEventInterface>
                {
                    private readonly TComponent _parent;
                    protected readonly EntityCollection<TEntity, TEntityId> _entities;
                    public Collection(TComponent parent)
                    {
                        _entities = new EntityCollection<TEntity, TEntityId>();
                        _parent = parent;
                        _parent.RegisterEventAppliers()
                                  .For<TEntityCreatedEventInterface>(
                                      e =>
                                      {
                                          var entity = ObjectFactory<TEntity>.CreateInstance(_parent);
                                          var entityId = IdGetterSetter.GetId(e);

                                          _entities.Add(entity, entityId);
                                      })
                                  .For<TEntityBaseEventInterface>(e => _entities[IdGetterSetter.GetId(e)].ApplyEvent(e));
                    }

                 
                    public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => _entities;

                    public TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
                        where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
                    {
                        _parent.RaiseEvent(creationEvent);
                        var result = _entities.InCreationOrder.Last();
                        result.EventHandlersEventDispatcher.Dispatch(creationEvent);
                        return result;
                    }
                }
            }
        }
    }
}
