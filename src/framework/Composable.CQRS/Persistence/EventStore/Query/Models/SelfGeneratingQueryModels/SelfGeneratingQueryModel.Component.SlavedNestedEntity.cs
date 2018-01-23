using Composable.Messaging.Events;
using Composable.Persistence.EventStore.AggregateRoots;

// ReSharper disable UnusedMember.Global todo:tests

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            ///<summary>
            /// An entity that is not created and removed through raising events.
            /// Instead it is automatically created and/or removed when another entity in the Aggregate object graph is added or removed.
            /// Inheritors must implement the add/remove behavior.
            /// Inheritors must ensure that the Id property is initialized correctly before any calls to RaiseEvent.
            /// Usually this is implemented within a nested class that inherits from <see cref="EntityCollectionManagerBase"/>
            /// </summary>
            public abstract class SlavedNestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEventEntityIdSetterGetter> : NestedComponent<TEntity,
                                                                                 TEntityBaseEventClass,
                                                                                 TEntityBaseEventInterface>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntity : SlavedNestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventClass,
                                    TEntityBaseEventInterface,
                                    TEventEntityIdSetterGetter>
                where TEventEntityIdSetterGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
            {
                protected SlavedNestedEntity(TComponent parent) : this(parent.RegisterEventAppliers()) { }

                protected SlavedNestedEntity(IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar): base(appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityBaseEventInterface>();
                }

                // ReSharper disable once MemberCanBePrivate.Global
                // ReSharper disable once UnusedAutoPropertyAccessor.Global
                protected TEntityId Id { get; set; }

                public abstract class EntityCollectionManagerBase
                {
                    static readonly TEventEntityIdSetterGetter IdGetter = new TEventEntityIdSetterGetter();

                    readonly QueryModelEntityCollection<TEntity, TEntityId> _managedEntities;
                    protected EntityCollectionManagerBase
                        (IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                    {
                        _managedEntities = new QueryModelEntityCollection<TEntity, TEntityId>();
                        appliersRegistrar
                            .For<TEntityBaseEventInterface>(e => _managedEntities[IdGetter.GetId(e)].ApplyEvent(e));
                    }

                    public IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities => _managedEntities;
                }
            }
        }
    }
}
