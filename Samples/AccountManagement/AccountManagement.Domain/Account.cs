using System;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.Implementation;
using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Composable.Contracts;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;

namespace AccountManagement.Domain
{
    ///Completely encapsulates all the business logic for an account.  Should make it impossible for clients to use the class incorrectly.
    public class Account : AggregateRoot<Account, AccountEvent, IAccountEvent>
    {
        public Email Email { get; private set; } //Never public setters on an aggregate.
        public Password Password { get; private set; } //Never public setters on an aggregate.

        //No public constructors please. Aggregates are created through domain verbs.
        //Expose named factory methods that ensure the instance is valid instead. See register method below.
        Account():base(new DateTimeNowTimeSource())
        {
            //Maintain correct state as events are raised or read from the store.
            //Use property updated events whenever possible. Changes to public state should be represented by property updated events.
            RegisterEventAppliers()
                .For<IAccountEmailPropertyUpdatedEvent>(e => Email = e.Email)
                .For<IAccountPasswordPropertyUpdatedEvent>(e => Password = e.Password);
        }

        //Ensure that the state of the instance is sane. If not throw an exception.
        //Called after every call to RaiseEvent.
        protected override void AssertInvariantsAreMet()
        {
            Contract.Invariant(() => Email, () => Password, () => Id).NotNullOrDefault();
        }

        /// <summary><para>Used when a user manually creates an account themselves.</para>
        /// <para>Note how this design with a named static creation method: </para>
        /// <para> * makes it clearear what the caller intends.</para>
        /// <para> * makes it impossible to use the class incorrectly, such as forgetting to check for duplicates or save the new instance in the repository.</para>
        /// <para> * reduces code duplication since multiple callers are not burdened with saving the instance, checking for duplicates etc.</para>
        /// </summary>
        public static Account Register(
            Email email,
            Password password,
            Guid accountId,
            IAccountRepository repository,
            IDuplicateAccountChecker duplicateAccountChecker)
        {
            //Ensure that it is impossible to call with invalid arguments.
            //Since all domain types should ensure that it is impossible to create a non-default value that is invalid we only have to disallow default values.
            Contract.Argument(() => email, () => password, () => accountId, () => repository, () => duplicateAccountChecker).NotNullOrDefault();

            //The email is the unique identifier for logging into the account so obviously duplicates are forbidden.
            duplicateAccountChecker.AssertAccountDoesNotExist(email);

            var created = new Account();
            created.RaiseEvent(new UserRegisteredAccountEvent(accountId: accountId, email: email, password: password));
            repository.Add(created);

            return Contract.Return(created, inspect => inspect.NotNull()); //Promise and ensure that you will never return null.
        }

        public void ChangePassword(string oldPassword, Password newPassword)
        {
            Contract.Argument(() => newPassword).NotNullOrDefault();
            Contract.Argument(() => oldPassword).NotNullEmptyOrWhiteSpace();

            Password.AssertIsCorrectPassword(oldPassword);

            RaiseEvent(new UserChangedAccountPassword(newPassword));
        }

        public void ChangeEmail(Email email)
        {
            Contract.Argument(() => email).NotNullOrDefault();

            RaiseEvent(new UserChangedAccountEmailEvent(email));
        }
    }
}
