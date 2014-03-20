using System;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Sample.AccountManagement.Shared;

namespace Composable.CQRS.Sample.AccountManagement.Domain.Events.Impl
{
    public class AccountRegisteredEvent : AggregateRootEvent, IAccountRegisteredEvent
    {
        public AccountRegisteredEvent(Guid accountId, Email email, Password password):base(accountId)
        {
            Email = email;
            Password = password;
        }

        public Email Email { get; set; }
        public Password Password { get; set; }
    }
}