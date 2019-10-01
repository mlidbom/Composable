﻿// ReSharper disable All
#pragma warning disable
namespace Composable.Tests.Messaging.APIDraft
{

    interface IAccountEvent { }

    class SendAccountRegistrationWelcomeEmailCommand { }

    class AccountAggregate { }
    class AccountReadModel { }
    class EmailToAccountLookupModel { }

    class GetAccountQuery { }
    class AccountCreatedEvent { }
    class CreateAccountCommand { }

    class AccountQueryHandler
    {
        public string Handle(GetAccountQuery query) => string.Empty;
    }

    class AccountQueryModelUpdater
    {
        public void Handle(AccountCreatedEvent @event) { }
    }

    class AccountCommandHandler
    {
        public void Handle(CreateAccountCommand command) { }
    }

    class AccountController
    {
        public string Handle(GetAccountQuery query) => string.Empty;
        public void Handle(AccountCreatedEvent @event) { }
        public void Handle(CreateAccountCommand command) { }
    }
}
