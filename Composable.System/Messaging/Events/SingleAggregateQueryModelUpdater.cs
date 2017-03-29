using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventSourcing;

namespace Composable.Messaging.Events
{
    public abstract class SingleAggregateQueryModelUpdater<TImplementer, TViewModel, TEvent, TSession> : CallsMatchingHandlersInRegistrationOrderEventHandler<TEvent>
        where TImplementer : SingleAggregateQueryModelUpdater<TImplementer, TViewModel, TEvent, TSession>
        where TSession : IDocumentDbUpdater
        where TEvent : class, IAggregateRootEvent
        where TViewModel : class, ISingleAggregateQueryModel, new()
    {
        protected TViewModel Model { get; private set; }

        protected SingleAggregateQueryModelUpdater(TSession session)
        {
            var session1 = session;

            RegisterHandlers()
                .ForGenericEvent<IAggregateRootDeletedEvent>(e => session1.Delete<TViewModel>(e.AggregateRootId))
                .BeforeHandlers(e =>
                                {
                                    if(e is IAggregateRootCreatedEvent)
                                    {
                                        Model = new TViewModel();
                                        Model.SetId(e.AggregateRootId);
                                    } else if(!(e is IAggregateRootDeletedEvent))
                                    {
                                        Model = session1.GetForUpdate<TViewModel>(e.AggregateRootId);
                                    }
                                })
                .AfterHandlers(e =>
                               {
                                   if(e is IAggregateRootCreatedEvent)
                                   {
                                       session1.Save(Model);
                                   } else
                                   {
                                       session1.SaveChanges();
                                   }
                               });
        }
    }
}
