using System;

using AccountManagement.Domain.Shared;
using Composable.Contracts;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Events.Implementation
{
    public class UserRegisteredAccountEvent : AccountEvent, IUserRegisteredAccountEvent
    {
        [Obsolete("NServicebus requires this constructor to exist.", true), UsedImplicitly] //ncrunch: no coverage
        public UserRegisteredAccountEvent() {} //ncrunch: no coverage

        ///<summary>
        /// The constructor should guarantee that the event is correctly created.
        /// Once again we are saved from doing work here by using value objects for <see cref="Email"/> and <see cref="Password"/>
        /// The base class will ensure that the GUID is not empty.
        /// </summary>
        public UserRegisteredAccountEvent(Guid accountId, Email email, Password password) : base(accountId)
        {
            Contract.Argument(() => email, () => password)
                        .NotNull();

            Email = email;
            Password = password;
        }

        //The setters should be private but NServiceBus does not work with private setters :(
        //Hopefully they will fix this or we will create our own serializer for NServiceBus
        public Email Email { get; private set; }
        public Password Password { get; private set; }
    }
}
