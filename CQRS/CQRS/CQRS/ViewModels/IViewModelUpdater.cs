using Composable.CQRS.EventSourcing;
using NServiceBus;

namespace Composable.CQRS.ViewModels
{
    public interface IViewModelUpdater<TViewModel, TEvent> : IHandleMessages<TEvent> where TEvent : IAggregateRootEvent
    {
        
    }
}