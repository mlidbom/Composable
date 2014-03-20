using Composable.CQRS.EventSourcing;
using Composable.CQRS.Sample.AccountManagement.Domain.Events.PropertyUpdated;

namespace Composable.CQRS.Sample.AccountManagement.Domain.Events
{
    public interface IAccountEvent : IAggregateRootEvent { }

    public interface IAccountRegisteredEvent : IAggregateRootCreatedEvent,
        IAccountEmailPropertyUpdatedEvent,
        IAccountPasswordPropertyUpdateEvent
    {
        
    }

    public interface IUserChangedAccountEmailEvent : IAccountEmailPropertyUpdatedEvent
    {

    }

    public interface IUserChangedAccountPasswordEvent : IAccountPasswordPropertyUpdateEvent
    {

    }
}