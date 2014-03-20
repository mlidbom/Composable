using System;
using AccountManagement.Domain.Shared;
using Composable.CQRS.EventSourcing;

namespace AccountManagement.Domain.Events.Impl
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