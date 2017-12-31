using System;
using Composable;
using Composable.Contracts;
using Composable.Persistence.EventStore;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

namespace AccountManagement.Domain.Events
{
    public static partial class AccountEvent
    {
        public static class Implementation
        {
            [TypeId("66BAE496-9422-4379-8719-1642FAAD35B6")]public abstract class Root : DomainEvent, AccountEvent.Root
            {
                protected Root() {}
                protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
            }

            [TypeId("CB7B686A-BF1A-4215-8081-98EF01135D6D")]public class UserRegistered : Root, AccountEvent.UserRegistered
            {
                [UsedImplicitly] UserRegistered() {} //ncrunch: no coverage

                ///<summary>
                /// The constructor should guarantee that the event is correctly created.
                /// Once again we are saved from doing work here by using value objects for <see cref="Email"/> and <see cref="Password"/>
                /// The base class will ensure that the GUID is not empty.
                /// </summary>
                public UserRegistered(Guid accountId, Email email, Password password) : base(accountId)
                {
                    Contract.Argument.Assert(email != null, password != null);

                    Email = email;
                    Password = password;
                }

                public Email Email { get; private set; }
                public Password Password { get; private set; }
            }

            [TypeId("6A7274A8-E3B7-4A18-95C6-7B767BCFD10F")]public class UserChangedEmail : Root, AccountEvent.UserChangedEmail
            {
                public UserChangedEmail(Email email) => Email = email;

                public Email Email { get; private set; }
            }

            [TypeId("902C514F-5FD1-4A19-88F5-7E3C33C18DE7")]public class UserChangedPassword : Root, AccountEvent.UserChangedPassword
            {
                public UserChangedPassword(Password password) => Password = password;

                public Password Password { get; private set; }
            }

            [TypeId("7E87FAC6-A391-47FE-88E7-2640CD81E75D")]public class LoggedIn : Root, AccountEvent.LoggedIn
            {
                public string AuthenticationToken { get; }

                public LoggedIn(string token) => AuthenticationToken = token;
            }

            [TypeId("9F164912-F2CA-4FB7-BE1B-76803FCA3285")]public class LoginFailed : Root, AccountEvent.LoginFailed {}
        }
    }
}
