using Composable.CQRS.EventSourcing;
using Composable.CQRS.Sample.AccountManagement.Shared;

namespace Composable.CQRS.Sample.AccountManagement.Domain.Events
{

    public class UserChangedAccountEmailEvent : AggregateRootEvent, IUserChangedAccountEmailEvent
    {
        public UserChangedAccountEmailEvent(Email email)
        {
            Email = email;
        }
        public Email Email { get; private set; }
    }
}