using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore.AggregateRoots;

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
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEventEntityIdSetterGetter> : NestedComponent<TEntity,
                                                                                 TEntityBaseEventClass,
                                                                                 TEntityBaseEventInterface>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventClass,
                                    TEntityBaseEventInterface,
                                    TEntityCreatedEventInterface,
                                    TEventEntityIdSetterGetter>
                where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId,
                                                                                           TEntityBaseEventClass,
                                                                                           TEntityBaseEventInterface>, new()
            {
                static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

                // ReSharper disable once UnusedMember.Global todo: coverage
                protected NestedEntity(TComponent parent): this(parent.RegisterEventAppliers()) { }

                protected NestedEntity(IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e));
                }

                internal TEntityId Id { get; private set; }

                // ReSharper disable once UnusedMember.Global todo: coverage
                public  static CollectionManager CreateSelfManagingCollection(TComponent parent)//todo:tests
                  =>
                      new CollectionManager(
                          parent: parent,
                          appliersRegistrar: parent.RegisterEventAppliers());

                public class CollectionManager : QueryModelEntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityBaseEventClass,
                                                         TEntityBaseEventInterface,
                                                         TEntityCreatedEventInterface,
                                                         TEventEntityIdSetterGetter>
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
