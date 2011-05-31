using System;
using Composable.CQRS.EventSourcing;
using Composable.StuffThatDoesNotBelongHere;

namespace Composable.CQRS.ViewModels
{
    public abstract class ViewModelUpdater<TImplementor, TViewModel, TEvent> : 
        EventPersister<TImplementor, TEvent>, 
        IViewModelUpdater<TViewModel, TEvent> where TImplementor : EventPersister<TImplementor, TEvent> where TEvent : IAggregateRootEvent
    {
        public abstract void Handle(TEvent message);
    }
}