#region usings

using System;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.KeyValueStorage;

#endregion

namespace Composable.CQRS.EventHandling
{
    public class SingleAggregateQueryModelUpdater<TImplementor, TViewModel, TEvent, TSession> : CallAllMatchingHandlersInRegistrationOrderEventHandler<TImplementor, TEvent>
        where TImplementor : SingleAggregateQueryModelUpdater<TImplementor, TViewModel, TEvent, TSession>
        where TSession : IDocumentDbSession
        where TEvent : IAggregateRootEvent
        where TViewModel : class, IHasPersistentIdentity<Guid>, new()
    {
        protected readonly TSession Session;
        protected TViewModel Model { get; set; }

        protected SingleAggregateQueryModelUpdater(TSession session)
        {
            Session = session;

            RegisterHandlers()
                .ForGenericEvent<IAggregateRootDeletedEvent>(e =>  Session.Delete<TViewModel>(e.AggregateRootId))
                .BeforeHandlers(e =>
                                {
                                    if(e is IAggregateRootCreatedEvent)
                                    {
                                        Model = new TViewModel();
                                    }else if (!(e is IAggregateRootDeletedEvent))
                                    {
                                        Model = Session.GetForUpdate<TViewModel>(e.AggregateRootId);
                                    }
                                })
                .AfterHandlers(e =>
                               {
                                   if(e is IAggregateRootCreatedEvent)
                                   {
                                       Session.Save(Model);
                                   }else
                                   {
                                       Session.SaveChanges();
                                   }
                               });
        }
    }
}
