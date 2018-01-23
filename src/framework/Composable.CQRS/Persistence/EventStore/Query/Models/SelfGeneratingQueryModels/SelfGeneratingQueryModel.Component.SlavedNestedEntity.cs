using Composable.Messaging.Events;
using Composable.Persistence.EventStore.AggregateRoots;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
    {
        public abstract partial class Component<TComponent, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventInterface>
        {
            ///<summary>
            /// An entity that is not created and removed through raising events.
            /// Instead it is automatically created and/or removed when another entity in the Aggregate object graph is added or removed.
            /// Inheritors must implement the add/remove behavior.
            /// Inheritors must ensure that the Id property is initialized.
            /// Usually this is implemented within a nested class that inherits from <see cref="EntityCollectionManagerBase"/>
            /// </summary>
            public abstract class SlavedNestedEntity<TEntity, TEntityId, TEntityBaseEventInterface, TEventEntityIdGetter> : NestedComponent<TEntity,TEntityBaseEventInterface>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntity : SlavedNestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventInterface,
                                    TEventEntityIdGetter>
                where TEventEntityIdGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
            {
                protected SlavedNestedEntity(TComponent parent) : this(parent.RegisterEventAppliers()) { }

                protected SlavedNestedEntity(IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar): base(appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityBaseEventInterface>();
                }

                // ReSharper disable once MemberCanBePrivate.Global
                protected TEntityId Id { get; set; }

                public abstract class EntityCollectionManagerBase
                {
                    static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

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
