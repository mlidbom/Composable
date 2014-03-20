using Composable.CQRS.Sample.AccountManagement.Shared;

namespace Composable.CQRS.Sample.AccountManagement.Domain.Events.PropertyUpdated
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