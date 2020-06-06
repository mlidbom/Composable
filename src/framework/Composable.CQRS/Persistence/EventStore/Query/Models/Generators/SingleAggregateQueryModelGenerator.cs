﻿using System;
using System.Linq;
using Composable.Functional;
using Composable.Messaging.Events;
using Composable.System.Linq;
using Composable.System.Reflection;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.Generators
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public abstract class SingleAggregateQueryModelGenerator<TImplementer, TViewModel, TEvent, TSession>
        : IQueryModelGenerator<TViewModel>,
        IVersioningQueryModelGenerator<TViewModel>
        where TImplementer : SingleAggregateQueryModelGenerator<TImplementer, TViewModel, TEvent, TSession>
        where TSession : IEventStoreReader
        where TEvent : class, IAggregateEvent
        where TViewModel : class, ISingleAggregateQueryModel
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>();
        readonly TSession _session;
        protected TViewModel? Model { get; private set; }

        protected SingleAggregateQueryModelGenerator(TSession session)
        {
            _session = session;
            _eventDispatcher.RegisterHandlers()
                .ForGenericEvent<IAggregateCreatedEvent>(e => Model!.SetId(e.AggregateId))
                .ForGenericEvent<IAggregateDeletedEvent>(e => Model = null);
        }

        ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
        protected IEventHandlerRegistrar<TEvent> RegisterHandlers() => _eventDispatcher.RegisterHandlers();

        public Option<TViewModel> TryGenerate(Guid id) => TryGenerate(id, int.MaxValue);

        public Option<TViewModel> TryGenerate(Guid id, int version)
        {
            var history = _session.GetHistory(id).Take(version).Cast<TEvent>().ToList();
            if (history.None())
            {
                return Option.None<TViewModel>();
            }
            var queryModel = Constructor.For<TViewModel>.DefaultConstructor.Instance();
            Model = queryModel;
            history.ForEach(_eventDispatcher.Dispatch);
            var result = Model;//Yes it does make sense. Look at the registered handler for IAggregateDeletedEvent
            Model = null;
            return Option.Some(result);
        }
    }
}