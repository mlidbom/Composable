using System;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Sample.AccountManagement.Shared;

namespace Composable.CQRS.Sample.AccountManagement.Domain.Events
{
    public class AccountRegisteredEvent : AggregateRootEvent, IAggregateRootCreatedEvent, IAccountRegisteredEvent
    {
        protected AccountRegisteredEvent(Guid accountId, Email email, Password password):base(accountId)
        {
            Email = email;
            Password = password;
        }

        public Email Email { get; set; }
        public Password Password { get; set; }
    }
}