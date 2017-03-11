using Composable.CQRS.EventSourcing;
using Composable.Persistence.KeyValueStorage;

namespace Composable.Messaging.Events
{
    public abstract class SingleAggregateQueryModelUpdater<TImplementer, TViewModel, TEvent, TSession> : CallsMatchingHandlersInRegistrationOrderEventHandler<TEvent>
        where TImplementer : SingleAggregateQueryModelUpdater<TImplementer, TViewModel, TEvent, TSession>
        where TSession : IDocumentDbSession
        where TEvent : class, IAggregateRootEvent
        where TViewModel : class, ISingleAggregateQueryModel, new()
    {
        readonly TSession _session;
        protected TViewModel Model { get; private set; }

        protected SingleAggregateQueryModelUpdater(TSession session)
        {
            _session = session;

            RegisterHandlers()
                .ForGenericEvent<IAggregateRootDeletedEvent>(e => _session.Delete<TViewModel>(e.AggregateRootId))
                .BeforeHandlers(e =>
                                {
                                    if(e is IAggregateRootCreatedEvent)
                                    {
                                        Model = new TViewModel();
                                        Model.SetId(e.AggregateRootId);
                                    } else if(!(e is IAggregateRootDeletedEvent))
                                    {
                                        Model = _session.GetForUpdate<TViewModel>(e.AggregateRootId);
                                    }
                                })
                .AfterHandlers(e =>
                               {
                                   if(e is IAggregateRootCreatedEvent)
                                   {
                                       _session.Save(Model);
                                   } else
                                   {
                                       _session.SaveChanges();
                                   }
                               });
        }
    }
}
