#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.KeyValueStorage;
using Composable.StuffThatDoesNotBelongHere;
using Composable.System.Linq;

#endregion

namespace Composable.CQRS.ViewModels
{
    public class ViewModelUpdater<TImplementor, TViewModel, TEvent, TSession> :
        MultiEventHandler<TImplementor, TEvent>
        where TImplementor : ViewModelUpdater<TImplementor, TViewModel, TEvent, TSession>
        where TSession : IDocumentDbSession
        where TEvent : IAggregateRootEvent
        where TViewModel : class, IHasPersistentIdentity<Guid>
    {
        protected readonly TSession Session;

        protected TViewModel Model { get; set; }

        private bool _doAdd;

        protected ViewModelUpdater(TSession session, Type creationEvent):this(session, Seq.Create(creationEvent), Seq.Create<Type>())
        {

        }

        protected ViewModelUpdater(TSession session, Type creationEvent, Type deletionEvent)
            : this(session, Seq.Create(creationEvent), Seq.Create(deletionEvent))
        {
        }

        protected ViewModelUpdater(TSession session, IEnumerable<Type> creationEvents, IEnumerable<Type> deletionEvents)
        {
            Session = session;

            var registrar = RegisterHandlers();

            foreach (var deletionEvent in deletionEvents)
            {
                registrar.For(deletionEvent, e => Session.Delete<TViewModel>(e.AggregateRootId));
            }

            registrar.BeforeHandlers(e =>
                                         {
                                             if (creationEvents.Any(eventType => eventType.IsAssignableFrom(e.GetType())))
                                             {
                                                TViewModel m;
                                                Session.TryGetForUpdate(e.AggregateRootId, out m);
                                                Model = m;
                                                this._doAdd = (m == null);
                                             }
                                             else
                                             {
                                                 Model = Session.GetForUpdate<TViewModel>(e.AggregateRootId);
                                                 this._doAdd = false;
                                             }
                                         })
                .AfterHandlers(e =>
                                   {
                                       if (this._doAdd)
                                       {
                                           Session.Save(Model);
                                       }
                                       else
                                       {
                                           Session.SaveChanges();
                                       }
                                   });
        }
    }
}