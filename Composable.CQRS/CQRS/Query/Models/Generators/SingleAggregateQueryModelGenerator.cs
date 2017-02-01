using System;
using System.Linq;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.System.Linq;

namespace Composable.CQRS.Query.Models.Generators
{
    public abstract class SingleAggregateQueryModelGenerator<TImplementer, TViewModel, TEvent, TSession> 
        : IQueryModelGenerator<TViewModel>,
        IVersioningQueryModelGenerator<TViewModel> 
        where TImplementer : SingleAggregateQueryModelGenerator<TImplementer, TViewModel, TEvent, TSession>
        where TSession : IEventStoreReader
        where TEvent : class, IAggregateRootEvent
        where TViewModel : class, ISingleAggregateQueryModel, new()
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>();
        protected readonly TSession Session;
        protected TViewModel Model { get; set; }

        protected SingleAggregateQueryModelGenerator(TSession session)
        {
            Session = session;
            _eventDispatcher.RegisterHandlers()
                .ForGenericEvent<IAggregateRootCreatedEvent>(e => Model.SetId(e.AggregateRootId))
                .ForGenericEvent<IAggregateRootDeletedEvent>(e => Model = null);
        }

        ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>.RegistrationBuilder RegisterHandlers()
        {
            return _eventDispatcher.RegisterHandlers();
        }

        public TViewModel TryGenerate(Guid id)
        {
            return TryGenerate(id, int.MaxValue);
        }

        public TViewModel TryGenerate(Guid id, int version)
        {
            var history = Session.GetHistory(id).Take(version).Cast<TEvent>().ToList();
            if (history.None())
            {
                return null;
            }
            var queryModel = new TViewModel();
            Model = queryModel;
            history.ForEach(_eventDispatcher.Dispatch);
            var result = Model;//Yes it does make sense. Look at the registered handler for IAggregateRootDeletedEvent
            Model = null;
            return result;
        }
    }
}