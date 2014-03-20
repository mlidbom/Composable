using System;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.Impl;
using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.Shared;
using Composable.CQRS.EventSourcing;

namespace AccountManagement.Domain
{
    internal class Account : AggregateRoot<Account, IAccountEvent>
    {
        public Email Email { get; private set; }
        public Password Password { get; private set; }

        public Account()
        {
            RegisterEventAppliers()
                .For<IAccountEmailPropertyUpdatedEvent>(e => Email = e.Email)
                .For<IAccountPasswordPropertyUpdateEvent>(e => Password = e.Password);
        }

        public void Register(Email email, Password password, Guid accountId)
        {
            RaiseEvent(new AccountRegisteredEvent(accountId:accountId, email: email, password: password ));
        }

        public void ChangePassword(Password password)
        {
            RaiseEvent(new UserChangedAccountPassword(password));
        }

        public void ChangeEmail(Email email)
        {
            RaiseEvent(new UserChangedAccountEmailEvent(email));
        }
    }
}