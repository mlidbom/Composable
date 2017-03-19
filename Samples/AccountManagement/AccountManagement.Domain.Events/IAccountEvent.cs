using AccountManagement.Domain.Events.PropertyUpdated;
using Composable.Persistence.EventSourcing;

namespace AccountManagement.Domain.Events
{
    public interface IAccountEvent : IAggregateRootEvent {}

    public interface IAccountCreatedEvent :
        IAccountEvent,
        IAggregateRootCreatedEvent
        //Used in multiple places by the infrastructure and clients. Things WILL BREAK without this.
        //AggregateRoot: Sets the ID when such an event is raised.
        //Creates a viewmodel automatically when received by an SingleAggregateQueryModelUpdater
    {}


    public interface IUserRegisteredAccountEvent :
        IAccountCreatedEvent,
        IAccountEmailPropertyUpdatedEvent,
        IAccountPasswordPropertyUpdatedEvent {}

    public interface IUserChangedAccountEmailEvent :
        IAccountEmailPropertyUpdatedEvent {}

    public interface IUserChangedAccountPasswordEvent :
        IAccountPasswordPropertyUpdatedEvent {}
}
