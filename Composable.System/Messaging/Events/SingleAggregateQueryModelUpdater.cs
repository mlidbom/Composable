using Composable.CQRS.EventSourcing;
using Composable.KeyValueStorage;

namespace Composable.Messaging.Events
{
    public abstract class SingleAggregateQueryModelUpdater<TImplementer, TViewModel, TEvent, TSession> : CallsMatchingHandlersInRegistrationOrderEventHandler<TEvent>
        where TImplementer : SingleAggregateQueryModelUpdater<TImplementer, TViewModel, TEvent, TSession>
        where TSession : IDocumentDbSession
        where TEvent : class, IAggregateRootEvent
        where TViewModel : class, ISingleAggregateQueryModel, new()
    {
        protected readonly TSession Session;
        protected TViewModel Model { get; set; }

        protected SingleAggregateQueryModelUpdater(TSession session)
        {
            Session = session;

            RegisterHandlers()
                .ForGenericEvent<IAggregateRootDeletedEvent>(e => Session.Delete<TViewModel>(e.AggregateRootId))
                .BeforeHandlers(e =>
                                {
                                    if(e is IAggregateRootCreatedEvent)
                                    {
                                        Model = new TViewModel();
                                        Model.SetId(e.AggregateRootId);
                                    } else if(!(e is IAggregateRootDeletedEvent))
                                    {
                                        Model = Session.GetForUpdate<TViewModel>(e.AggregateRootId);
                                    }
                                })
                .AfterHandlers(e =>
                               {
                                   if(e is IAggregateRootCreatedEvent)
                                   {
                                       Session.Save(Model);
                                   } else
                                   {
                                       Session.SaveChanges();
                                   }
                               });
        }
    }
}
