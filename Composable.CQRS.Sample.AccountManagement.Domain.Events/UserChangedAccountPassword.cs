using Composable.CQRS.EventSourcing;
using Composable.CQRS.Sample.AccountManagement.Domain.Events.PropertyUpdated;
using Composable.CQRS.Sample.AccountManagement.Shared;

namespace Composable.CQRS.Sample.AccountManagement.Domain.Events
{
    public class AccountChangedPassword : AggregateRootEvent, IAccountPasswordPropertyUpdateEvent
    {
        public AccountChangedPassword(Password password)
        {
            Password = password;
        }

        public Password Password { get; private set; }
    }


}