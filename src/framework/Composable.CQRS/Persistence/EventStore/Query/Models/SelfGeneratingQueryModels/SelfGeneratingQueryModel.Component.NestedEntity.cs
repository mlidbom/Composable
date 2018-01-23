using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

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
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEventEntityIdGetter> : NestedComponent<TEntity,
                                                                                 TEntityBaseEventInterface>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventInterface,
                                    TEntityCreatedEventInterface,
                                    TEventEntityIdGetter>
                where TEventEntityIdGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
            {
                static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

                protected NestedEntity(TComponent parent): this(parent.RegisterEventAppliers()) { }

                protected NestedEntity(IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .For<TEntityCreatedEventInterface>(e => Id = IdGetter.GetId(e));
                }

                internal TEntityId Id { get; private set; }

                public  static CollectionManager CreateSelfManagingCollection(TComponent parent)//todo:tests
                  =>
                      new CollectionManager(
                          parent: parent,
                          appliersRegistrar: parent.RegisterEventAppliers());

                public class CollectionManager : QueryModelEntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityBaseEventInterface,
                                                         TEntityCreatedEventInterface,
                                                         TEventEntityIdGetter>
                {
                    internal CollectionManager
                        (TComponent parent,
                         IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                        : base(parent, appliersRegistrar)
                    { }
                }
            }
        }
    }
}
