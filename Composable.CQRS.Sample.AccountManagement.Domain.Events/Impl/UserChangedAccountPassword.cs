using Composable.CQRS.EventSourcing;
using Composable.CQRS.Sample.AccountManagement.Domain.Events.PropertyUpdated;
using Composable.CQRS.Sample.AccountManagement.Shared;

namespace Composable.CQRS.Sample.AccountManagement.Domain.Events.Impl
{
    public class UserChangedAccountPassword : AggregateRootEvent, IAccountPasswordPropertyUpdateEvent
    {
        public UserChangedAccountPassword(Password password)
        {
            Password = password;
        }

        public Password Password { get; private set; }
    }


}