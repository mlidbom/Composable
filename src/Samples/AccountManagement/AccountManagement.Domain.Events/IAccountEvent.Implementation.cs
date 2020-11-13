using System;
using AccountManagement.Domain.Passwords;
using Composable.Contracts;
using Composable.Persistence.EventStore;
using Newtonsoft.Json;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

namespace AccountManagement.Domain.Events
{
    //refactor: Consider using interfaces instead of static classes for nesting our events.
    public static partial class AccountEvent
    {
#pragma warning disable CA1724 // Type names should not match namespaces
        public static class Implementation
#pragma warning restore CA1724 // Type names should not match namespaces
        {
            public abstract class Root : AggregateEvent, AccountEvent.Root
            {
                protected Root() {}
                protected Root(Guid aggregateId) : base(aggregateId) {}
            }

            public class UserRegistered : Root, AccountEvent.UserRegistered
            {
#pragma warning disable IDE0051 // Remove unused private members
                [JsonConstructor] UserRegistered(Email email, Password password)
#pragma warning restore IDE0051 // Remove unused private members
                {
                    Email = email;
                    Password = password;
                }

                ///<summary>
                /// The constructor should guarantee that the event is correctly created.
                /// Once again we are saved from doing work here by using value objects for <see cref="Email"/> and <see cref="Password"/>
                /// The base class will ensure that the GUID is not empty.
                /// </summary>
                public UserRegistered(Guid accountId, Email email, Password password) : base(accountId)
                {
                    Contract.ArgumentNotNull(email, nameof(email), password, nameof(password));

                    Email = email;
                    Password = password;
                }

                public Email Email { get; private set; }
                public Password Password { get; private set; }
            }

            public class UserChangedEmail : Root, AccountEvent.UserChangedEmail
            {
                public UserChangedEmail(Email email) => Email = email;

                public Email Email { get; private set; }
            }

            public class UserChangedPassword : Root, AccountEvent.UserChangedPassword
            {
                public UserChangedPassword(Password password) => Password = password;

                public Password Password { get; private set; }
            }

            public class LoggedIn : Root, AccountEvent.LoggedIn
            {
                public string AuthenticationToken { get; }

                public LoggedIn(string token) => AuthenticationToken = token;
            }

            public class LoginFailed : Root, AccountEvent.LoginFailed {}
        }
    }
}
