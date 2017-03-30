using System;
using Composable.Contracts;
using Composable.Persistence.EventSourcing;
using JetBrains.Annotations;
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

namespace AccountManagement.Domain.Events
{
    public static partial class AccountEvent
    {
        public static class Implementation
        {
            public class Root : AggregateRootEvent, AccountEvent.Root
            {
                protected Root() {}
                protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
            }

            public class UserRegistered : Root, AccountEvent.UserRegistered
            {
                [UsedImplicitly] //ncrunch: no coverage
                UserRegistered() { } //ncrunch: no coverage

                ///<summary>
                /// The constructor should guarantee that the event is correctly created.
                /// Once again we are saved from doing work here by using value objects for <see cref="Email"/> and <see cref="Password"/>
                /// The base class will ensure that the GUID is not empty.
                /// </summary>
                public UserRegistered(Guid accountId, Email email, Password password) : base(accountId)
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

            public class UserChangedEmail : Root, AccountEvent.UserChangedEmail
            {
                [UsedImplicitly] //ncrunch: no coverage
                UserChangedEmail() { } //ncrunch: no coverage

                public UserChangedEmail(Email email) => Email = email;

                public Email Email { get; private set; }
            }

            public class UserChangedPassword : Root, AccountEvent.UserChangedPassword
            {
                [UsedImplicitly] //ncrunch: no coverage
                public UserChangedPassword() { } //ncrunch: no coverage

                public UserChangedPassword(Password password) => Password = password;

                public Password Password { get; private set; }
            }
        }
    }
}
