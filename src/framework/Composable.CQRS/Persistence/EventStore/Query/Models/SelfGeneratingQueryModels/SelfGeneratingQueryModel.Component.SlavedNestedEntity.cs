using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;
using Composable.SystemCE.Reflection;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
    {
        public abstract partial class Component<TComponent, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponent : Component<TComponent, TComponentEvent>
        {
            ///<summary>
            /// An entity that is not created and removed through raising events.
            /// Instead it is automatically created and/or removed when another entity in the Aggregate object graph is added or removed.
            /// Inheritors must implement the add/remove behavior.
            /// Inheritors must ensure that the Id property is initialized.
            /// Usually this is implemented within a nested class that inherits from <see cref="EntityCollectionManagerBase"/>
            /// </summary>
            public abstract class SlavedNestedEntity<TEntity, TEntityId, TEntityEvent, TEventEntityIdGetter> : NestedComponent<TEntity,TEntityEvent>
                where TEntityEvent : class, TComponentEvent
                where TEntity : SlavedNestedEntity<TEntity,
                                    TEntityId,
                                    TEntityEvent,
                                    TEventEntityIdGetter>
                where TEventEntityIdGetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
            {
                protected SlavedNestedEntity(TComponent parent) : this(parent.RegisterEventAppliers()) { }

#pragma warning disable CS8618 //Todo: Should this unused class even be retained? Non-nullable field is uninitialized. Consider declaring as nullable.
                protected SlavedNestedEntity(IEventHandlerRegistrar<TEntityEvent> appliersRegistrar): base(appliersRegistrar, registerEventAppliers: false)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityEvent>();
                }

                // ReSharper disable once MemberCanBePrivate.Global
                protected TEntityId Id { get; set; }

                public abstract class EntityCollectionManagerBase
                {
                    static readonly TEventEntityIdGetter IdGetter = Constructor.For<TEventEntityIdGetter>.DefaultConstructor.Instance();

                    readonly QueryModelEntityCollection<TEntity, TEntityId> _managedEntities;
                    protected EntityCollectionManagerBase(IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                    {
                        _managedEntities = new QueryModelEntityCollection<TEntity, TEntityId>();
                        appliersRegistrar
                            .For<TEntityEvent>(e => _managedEntities[IdGetter.GetId(e)].ApplyEvent(e));
                    }

                    public IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities => _managedEntities;
                }
            }
        }
    }
}
