using System;
using Composable.CQRS.EventSourcing;
using Composable.DomainEvents;
using Composable.StuffThatDoesNotBelongHere;

namespace Composable.CQRS.ViewModels
{
    public class ViewModelUpdater<TImplementor, TViewModel, TEvent> : MultiEventHandler<TImplementor, TEvent> 
        where TEvent : IAggregateRootEvent 
        where TImplementor : ViewModelUpdater<TImplementor, TViewModel, TEvent>
    {
        
    }
}