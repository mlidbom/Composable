using System;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Sample.AccountManagement.Domain.Events;
using Composable.CQRS.Sample.AccountManagement.Domain.Events.Impl;
using Composable.CQRS.Sample.AccountManagement.Domain.Events.PropertyUpdated;
using Composable.CQRS.Sample.AccountManagement.Shared;

namespace Composable.CQRS.Sample.AccountManagement.Domain
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