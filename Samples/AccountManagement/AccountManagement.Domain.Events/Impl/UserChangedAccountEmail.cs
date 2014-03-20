using AccountManagement.Domain.Shared;
using Composable.CQRS.EventSourcing;

namespace AccountManagement.Domain.Events.Impl
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