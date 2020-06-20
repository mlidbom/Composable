using Composable.Contracts;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;
using Composable.System.Reflection;

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
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityEvent,
                                               TEntityCreatedEvent,
                                               TEventEntityIdGetter> : NestedComponent<TEntity,
                                                                                 TEntityEvent>
                where TEntityEvent : class, TComponentEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityEvent,
                                    TEntityCreatedEvent,
                                    TEventEntityIdGetter>
                where TEventEntityIdGetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
            {
                static readonly TEventEntityIdGetter IdGetter = Constructor.For<TEventEntityIdGetter>.DefaultConstructor.Instance();

                protected NestedEntity(TComponent parent): this(parent.RegisterEventAppliers()) { }

#pragma warning disable CS8618 //Review OK-ish: We guarantee that we never deliver out a null or default value from the public property.
                protected NestedEntity(IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
                {
                    RegisterEventAppliers()
                        .For<TEntityCreatedEvent>(e => _id = IdGetter.GetId(e));
                }

                TEntityId _id;
                internal TEntityId Id => Assert.Result.NotNullOrDefault(_id);

                public  static CollectionManager CreateSelfManagingCollection(TComponent parent)//todo:tests
                  =>
                      new CollectionManager(
                          parent: parent,
                          appliersRegistrar: parent.RegisterEventAppliers());

                public class CollectionManager : QueryModelEntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityEvent,
                                                         TEntityCreatedEvent,
                                                         TEventEntityIdGetter>
                {
                    internal CollectionManager(TComponent parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(parent, appliersRegistrar)
                    { }
                }
            }
        }
    }
}
