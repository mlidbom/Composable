using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain.Events.PropertyUpdated
{
    public interface IAccountPasswordPropertyUpdateEvent : IAccountEvent
    {
        Password Password { get; }
    }

    public interface IAccountEmailPropertyUpdatedEvent : IAccountEvent
    {
        Email Email { get; }
    }
}
