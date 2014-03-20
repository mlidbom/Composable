using AccountManagement.Domain.Events.PropertyUpdated;
using Composable.CQRS.EventSourcing;

namespace AccountManagement.Domain.Events
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