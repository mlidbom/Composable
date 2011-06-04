using System;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.KeyValueStorage;
using Composable.StuffThatDoesNotBelongHere;
using System.Linq;

namespace Composable.CQRS.ViewModels
{
    public class ViewModelUpdater<TImplementor, TViewModel, TEvent, TSession> : 
        MultiEventHandler<TImplementor, TEvent>
        where TImplementor : ViewModelUpdater<TImplementor, TViewModel, TEvent, TSession>
        where TSession : IKeyValueStoreSession 
        where TEvent : IAggregateRootEvent 
        where TViewModel : IHasPersistentIdentity<Guid>
    {
        protected readonly TSession Session;

        protected TViewModel Model { get; set; }

        protected ViewModelUpdater(TSession session, params Type[] creationEvents)
        {
            Session = session;
            RegisterHandlers()
                .BeforeHandlers(e =>
                                    {
                                        if (!creationEvents.Contains(e.GetType()))
                                        {
                                            Model = Session.Get<TViewModel>(e.AggregateRootId);
                                        }
                                    })
                .AfterHandlers(e =>
                                   {
                                        if (creationEvents.Contains(e.GetType()))
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