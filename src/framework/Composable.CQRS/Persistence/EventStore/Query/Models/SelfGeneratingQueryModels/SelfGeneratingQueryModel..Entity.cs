using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;
using Composable.System.Reflection;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
    {
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityEvent,
                                     TEntityCreatedEvent,
                                     TEventEntityIdGetter> : Component<TEntity, TEntityEvent>
            where TEntityEvent : class, TAggregateEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityEvent,
                                TEntityCreatedEvent,
                                TEventEntityIdGetter>
            where TEventEntityIdGetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
        {
            static readonly TEventEntityIdGetter IdGetter = Constructor.For<TEventEntityIdGetter>.DefaultConstructor.Instance();

            public TEntityId Id { get; private set; }

            protected Entity(TQueryModel queryModel) : this(queryModel.RegisterEventAppliers()) {}

            Entity(IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEvent>(e => Id = IdGetter.GetId(e));
            }

            public static CollectionManager CreateSelfManagingCollection(TQueryModel parent) => new CollectionManager(parent, parent.RegisterEventAppliers());

            public class CollectionManager : QueryModelEntityCollectionManager<
                                                 TQueryModel,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityEvent,
                                                 TEntityCreatedEvent,
                                                 TEventEntityIdGetter>
            {
                internal CollectionManager
                    (TQueryModel parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(parent, appliersRegistrar) {}
            }
        }
    }
}
