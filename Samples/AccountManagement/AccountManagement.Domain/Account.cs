using System;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.Implementation;
using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Composable.Contracts;
using Composable.CQRS.EventSourcing;

namespace AccountManagement.Domain
{
    ///Completely encapsulates all the business logic for an account.  Should make it impossible for clients to use the class incorrectly.
    public class Account : AggregateRoot<Account, IAccountEvent>
    {
        public Email Email { get; private set; }
        public Password Password { get; private set; }

        public Account()
        {
            //Maintain correct state as events are raised or read from the store. 
            //Use property updated events whenever possible. Changes to public state should be represented by property updated events.
            RegisterEventAppliers()
                .For<IAccountEmailPropertyUpdatedEvent>(e => Email = e.Email)
                .For<IAccountPasswordPropertyUpdateEvent>(e => Password = e.Password);
        }

        //Ensure that the state of the instance is sane. If not throw an exception.
        //Called after every call to RaiseEvent.
        override protected void AssertInvariantsAreMet()
        {
            Contract.Invariant(() => Email, () => Password, () => Id).NotNullOrDefault();
        }

        /// <summary><para>Usen when a user manually creates an account themselves.</para>
        /// <para>Note how this design with a named static creation method: </para>
        /// <para> * makes it clearear what the caller intends.</para>
        /// <para> * makes it impossible to use the class incorrectly, such as forgetting to save the new instance in the event store.</para>
        /// <para> * reduces code duplication since multiple callers are not burdened with saving the instance.</para>
        /// </summary>
        public static Account Register(Email email, Password password, Guid accountId, IAccountManagementEventStoreSession repository, IDuplicateAccountChecker duplicateAccountChecker)
        {
            //Ensure that it is impossible to call with invalid arguments. 
            //Since these types all ensure that it is impossible to create a non-default value that is invalid we only have to disallow default values.
            Contract.Arguments(() => email, () => password, () => accountId).NotNullOrDefault();
            if(duplicateAccountChecker.AccountExists(email))
            {
                throw new DuplicateAccountException(email);
            }

            var created = new Account();            
            created.RaiseEvent(new UserRegisteredAccountEvent(accountId: accountId, email: email, password: password));
            repository.Save(created);
            return created;
        }
        
        public void ChangePassword(string oldPassword, Password newPassword)
        {
            Contract.Arguments(() => newPassword).NotNullOrDefault();
            Contract.Arguments(() => oldPassword).NotNullEmptyOrWhiteSpace();            

            RaiseEvent(new UserChangedAccountPassword(newPassword));
        }

        public void ChangeEmail(Email email)
        {
            Contract.Arguments(() => email).NotNullOrDefault();

            RaiseEvent(new UserChangedAccountEmailEvent(email));
        }
    }
}